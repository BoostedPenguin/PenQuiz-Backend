using AutoMapper;
using GameService.Context;
using GameService.Data;
using GameService.Data.Models;
using GameService.Dtos.SignalR_Responses;
using GameService.Hubs;
using GameService.MessageBus;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.GameTimerServices
{
    public interface INeutralMCTimerEvents
    {
        Task Close_Neutral_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper);
        Task Close_Neutral_MultipleChoice_Question_Voting(TimerWrapper timerWrapper);
        Task Debug_Start_Number_Neutral(TimerWrapper timerWrapper);
        Task Open_Neutral_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper);
        Task Show_Neutral_MultipleChoice_Screen(TimerWrapper timerWrapper);
    }

    public class NeutralMCTimerEvents : INeutralMCTimerEvents
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        private readonly IGameTerritoryService gameTerritoryService;
        private readonly IMapper mapper;
        private readonly IMessageBusClient messageBus;

        public NeutralMCTimerEvents(IDbContextFactory<DefaultContext> _contextFactory,
            IHubContext<GameHub, IGameHub> hubContext,
            IGameTerritoryService gameTerritoryService,
            IMapper mapper,
            IMessageBusClient messageBus)
        {
            contextFactory = _contextFactory;
            this.hubContext = hubContext;
            this.gameTerritoryService = gameTerritoryService;
            this.mapper = mapper;
            this.messageBus = messageBus;
        }

        public async Task Close_Neutral_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();

            var gm = data.GameInstance;
            var currentRound = data.GameInstance.Rounds.Where(x => x.GameRoundNumber == data.CurrentGameRoundNumber && x.GameInstanceId == data.GameInstanceId)
                .FirstOrDefault();

            var currentAttacker = currentRound.NeutralRound.TerritoryAttackers
                .First(x => x.AttackOrderNumber == currentRound.NeutralRound.AttackOrderNumber);

            // Player didn't select anything, assign him a random UNSELECTED territory
            if (currentAttacker.AttackedTerritoryId == null)
            {
                var randomTerritory =
                    gameTerritoryService.GetRandomTerritory(gm, currentAttacker.AttackerId, data.GameInstanceId);

                // Set this territory as being attacked from this person
                currentAttacker.AttackedTerritoryId = randomTerritory.Id;

                // Set the ObjectTerritory as being attacked currently
                data.GameInstance.ObjectTerritory.First(x => x.Id == randomTerritory.Id).AttackedBy = currentAttacker.AttackerId;

                db.Update(data.GameInstance);
            }

            // This attacker voting is over, go to next attacker
            switch (currentRound.NeutralRound.AttackOrderNumber)
            {
                case 1:
                case 2:
                    var newAttackorderNumber = ++currentRound.NeutralRound.AttackOrderNumber;
                    var nextAttacker =
                        currentRound.NeutralRound.TerritoryAttackers
                        .First(x => x.AttackOrderNumber == newAttackorderNumber);

                    db.Update(currentRound);
                    await db.SaveChangesAsync();

                    var availableTerritories = gameTerritoryService
                        .GetAvailableAttackTerritoriesNames(gm, nextAttacker.AttackerId, currentRound.GameInstanceId, true);

                    // Notify the group who is the next attacker
                    await hubContext.Clients.Group(data.GameLink)
                        .ShowRoundingAttacker(nextAttacker.AttackerId, availableTerritories);

                    await hubContext.Clients.Group(data.GameLink)
                        .GetGameInstance(data.GameInstance);

                    timerWrapper.StartTimer(ActionState.CLOSE_PLAYER_ATTACK_VOTING);

                    break;

                // This was the last attacker, show a preview of all territories now, and then show the questions
                case 3:
                    currentRound.IsTerritoryVotingOpen = false;
                    db.Update(currentRound);
                    await db.SaveChangesAsync();

                    await hubContext.Clients.Group(data.GameLink)
                        .GetGameInstance(data.GameInstance);

                    timerWrapper.StartTimer(ActionState.SHOW_MULTIPLE_CHOICE_QUESTION);
                    break;
                //throw new NotImplementedException();

                default:
                    throw new ArgumentException($"Unexpected attackordernumber value: {currentRound.NeutralRound.AttackOrderNumber}");
            }
        }

        public async Task Close_Neutral_MultipleChoice_Question_Voting(TimerWrapper timerWrapper)
        {
            // Can disable voting on start, however even 0-1s delay wouldn't be game breaking and would ease performance
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            var db = contextFactory.CreateDbContext();
            var gm = data.GameInstance;

            var currentRound = data.GameInstance.Rounds
                .Where(x => x.GameRoundNumber == data.CurrentGameRoundNumber && x.GameInstanceId == data.GameInstanceId)
                .FirstOrDefault();



            currentRound.IsQuestionVotingOpen = false;

            var playerIdAnswerId = new Dictionary<int, int>();

            foreach (var p in currentRound.NeutralRound.TerritoryAttackers)
            {
                var isThisPlayerAnswerCorrect =
                    currentRound.Question.Answers.FirstOrDefault(x => x.Id == p.AttackerMChoiceQAnswerId);

                // Player hasn't answered anything for this question, he loses
                if (isThisPlayerAnswerCorrect == null)
                {
                    p.AttackerWon = false;
                    p.AttackedTerritory.AttackedBy = null;
                    p.AttackedTerritory.TakenBy = null;


                    playerIdAnswerId.Add(
                        p.AttackerId,
                        0
                    );
                    continue;
                }

                if (isThisPlayerAnswerCorrect.Correct)
                {
                    // Player answered correctly, he gets the territory
                    p.AttackerWon = true;
                    p.AttackedTerritory.AttackedBy = null;
                    p.AttackedTerritory.TakenBy = p.AttackerId;
                }
                else
                {
                    // Player answered incorrecly, release isattacked lock on objterritory
                    p.AttackerWon = false;
                    p.AttackedTerritory.AttackedBy = null;
                    p.AttackedTerritory.TakenBy = null;
                }

                playerIdAnswerId.Add(
                    p.AttackerId,
                    p.AttackerMChoiceQAnswerId ?? 0
                );
            }

            // Debug
            //if (currentRound.GameRoundNumber == 1)
            //{
            //    currentRound.GameRoundNumber = 5;
            //    timerWrapper.Data.CurrentGameRoundNumber = 5;
            //}


            // Go to next round
            timerWrapper.Data.CurrentGameRoundNumber++;
            currentRound.GameInstance.GameRoundNumber = timerWrapper.Data.CurrentGameRoundNumber;

            db.Update(currentRound);
            await db.SaveChangesAsync();

            // Request a new batch of number questions from the question service
            if (data.CurrentGameRoundNumber > data.LastNeutralMCRound)
            {
                // Create number question rounds if gm multiple choice neutral rounds are over
                var rounds = Create_Neutral_Number_Rounds(gm, timerWrapper);

                rounds.ForEach(e => gm.Rounds.Add(e));

                db.Update(gm);
                await db.SaveChangesAsync();

                data.LastNeutralNumberRound = db.Round
                    .Where(x => x.GameInstanceId == data.GameInstanceId && x.AttackStage == AttackStage.NUMBER_NEUTRAL)
                    .OrderByDescending(x => x.GameRoundNumber)
                    .Select(x => x.GameRoundNumber)
                    .First();


                CommonTimerFunc.RequestQuestions(messageBus, data.GameGlobalIdentifier, rounds, true);
            }

            // Response correct answer and all player answers
            var response = new MCPlayerQuestionAnswers()
            {
                CorrectAnswerId = currentRound.Question.Answers.FirstOrDefault(x => x.Correct).Id,
                PlayerAnswers = new List<PlayerIdAnswerId>(),
            };

            foreach (var pId in playerIdAnswerId)
            {
                response.PlayerAnswers.Add(new PlayerIdAnswerId()
                {
                    Id = pId.Key,
                    AnswerId = pId.Value,
                });
            }

            await hubContext.Clients.Groups(data.GameLink).MCQuestionPreviewResult(response);

            if (data.CurrentGameRoundNumber > data.LastNeutralMCRound)
            {
                // Next action should be a number question related one
                timerWrapper.StartTimer(ActionState.SHOW_PREVIEW_GAME_MAP);
            }
            else
            {
                timerWrapper.StartTimer(ActionState.OPEN_PLAYER_ATTACK_VOTING);
            }
        }

        public async Task Open_Neutral_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();

            var gm = data.GameInstance;
            var currentRound = data.GameInstance.Rounds
                .Where(x => x.GameRoundNumber == data.CurrentGameRoundNumber && x.GameInstanceId == data.GameInstanceId)
                .FirstOrDefault();


            // Open this round for territory voting
            currentRound.IsTerritoryVotingOpen = true;

            db.Update(currentRound);
            await db.SaveChangesAsync();

            var currentAttacker = currentRound.NeutralRound.TerritoryAttackers
                .First(x => x.AttackOrderNumber == currentRound.NeutralRound.AttackOrderNumber);

            var availableTerritories = gameTerritoryService
                .GetAvailableAttackTerritoriesNames(gm, currentAttacker.AttackerId, currentRound.GameInstanceId, true);

            await hubContext.Clients.Group(data.GameLink)
                .ShowRoundingAttacker(currentAttacker.AttackerId, availableTerritories);

            await hubContext.Clients.Group(data.GameLink)
                .GetGameInstance(data.GameInstance);

            timerWrapper.StartTimer(ActionState.CLOSE_PLAYER_ATTACK_VOTING);
        }

        public async Task Show_Neutral_MultipleChoice_Screen(TimerWrapper timerWrapper)
        {
            // Stop timer until we calculate the next action and client event
            timerWrapper.Stop();

            // Get the question and show it to the clients
            var data = timerWrapper.Data;
            var db = contextFactory.CreateDbContext();


            var currentRound = data.GameInstance.Rounds
                .Where(x => x.GameRoundNumber == data.CurrentGameRoundNumber && x.GameInstanceId == data.GameInstanceId)
                .FirstOrDefault();

            var question = currentRound.Question;
            // Show the question to the user

            if (question == null)
                throw new ArgumentException($"There was no question generated for gameinstanceid: {data.GameInstanceId}, gameroundnumber: {data.CurrentGameRoundNumber}.");


            // Open this question for voting
            question.Round.IsQuestionVotingOpen = true;
            db.Update(question.Round);
            await db.SaveChangesAsync();

            var response = mapper.Map<QuestionClientResponse>(question);

            // If the round is a neutral one, then everyone can attack
            response.IsNeutral = true;
            response.Participants = question.Round.GameInstance.Participants.ToArray();

            await hubContext.Clients.Group(data.GameLink).GetRoundQuestion(response);

            timerWrapper.StartTimer(ActionState.END_MULTIPLE_CHOICE_QUESTION);
        }

        private static List<Round> Create_Neutral_Number_Rounds(GameInstance gm, TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;
            var untakenTerritoriesCount = gm.ObjectTerritory
                .Where(x => x.TakenBy == null && x.GameInstanceId == data.GameInstanceId)
                .Count();

            var participantsIds = gm.Participants.Select(x => x.PlayerId).ToList();

            var numberQuestionRounds = new List<Round>();
            for (var i = 0; i < untakenTerritoriesCount; i++)
            {
                var baseRound = new Round()
                {
                    // After this method executes we switch to the next round automatically thus + 1 now
                    GameRoundNumber = data.CurrentGameRoundNumber + i,
                    AttackStage = AttackStage.NUMBER_NEUTRAL,
                    Description = $"Number question. Attacker vs NEUTRAL territory",
                    IsQuestionVotingOpen = false,
                    IsTerritoryVotingOpen = false,
                    GameInstanceId = data.GameInstanceId
                };

                baseRound.NeutralRound = new NeutralRound()
                {
                    AttackOrderNumber = 0,
                };

                foreach (var participId in participantsIds)
                {
                    baseRound.NeutralRound.TerritoryAttackers.Add(new AttackingNeutralTerritory()
                    {
                        AttackerId = participId,
                        AttackOrderNumber = 0,
                    });
                }

                numberQuestionRounds.Add(baseRound);
            }

            return numberQuestionRounds;
        }


        public async Task Debug_Start_Number_Neutral(TimerWrapper timerWrapper)
        {
            timerWrapper.Stop();
            using var db = contextFactory.CreateDbContext();
            var data = timerWrapper.Data;

            var gm = data.GameInstance;

            data.CurrentGameRoundNumber = data.LastNeutralMCRound + 1;

            // Go to next round
            gm.GameRoundNumber = timerWrapper.Data.CurrentGameRoundNumber;

            // Create number question rounds if gm multiple choice neutral rounds are over
            var rounds = Create_Neutral_Number_Rounds(gm, timerWrapper);

            rounds.ForEach(e => gm.Rounds.Add(e));

            db.Update(gm);
            await db.SaveChangesAsync();

            data.LastNeutralNumberRound = db.Round
                .Where(x => x.GameInstanceId == data.GameInstanceId && x.AttackStage == AttackStage.NUMBER_NEUTRAL)
                .OrderByDescending(x => x.GameRoundNumber)
                .Select(x => x.GameRoundNumber)
                .First();


            CommonTimerFunc.RequestQuestions(messageBus, data.GameGlobalIdentifier, rounds, true);

            // Next action should be a number question related one
            timerWrapper.StartTimer(ActionState.SHOW_PREVIEW_GAME_MAP);
        }
    }
}

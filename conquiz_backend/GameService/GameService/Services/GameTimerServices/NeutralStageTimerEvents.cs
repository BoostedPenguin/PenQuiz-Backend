using AutoMapper;
using GameService.Context;
using GameService.Dtos;
using GameService.Dtos.SignalR_Responses;
using GameService.Hubs;
using GameService.MessageBus;
using GameService.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.GameTimerServices
{
    public interface INeutralStageTimerEvents
    {
        Task Close_Neutral_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper);
        Task Close_Neutral_MultipleChoice_Question_Voting(TimerWrapper timerWrapper);
        Task Open_Neutral_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper);
        Task Show_Neutral_MultipleChoice_Screen(TimerWrapper timerWrapper);
    }

    public class NeutralStageTimerEvents : INeutralStageTimerEvents
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        private readonly IGameTerritoryService gameTerritoryService;
        private readonly IMapper mapper;
        private readonly IMessageBusClient messageBus;

        public NeutralStageTimerEvents(IDbContextFactory<DefaultContext> _contextFactory,
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


        public async Task Open_Neutral_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();

            var currentRound = await db.Round
                .Include(x => x.NeutralRound)
                .ThenInclude(x => x.TerritoryAttackers)
                .ThenInclude(x => x.AttackedTerritory)
                .Where(x => x.GameRoundNumber == data.CurrentGameRoundNumber && x.GameInstanceId == data.GameInstanceId)
                .FirstOrDefaultAsync();

            // Open this round for territory voting
            currentRound.IsTerritoryVotingOpen = true;

            db.Update(currentRound);
            await db.SaveChangesAsync();

            var currentAttacker = currentRound.NeutralRound.TerritoryAttackers
                .First(x => x.AttackOrderNumber == currentRound.NeutralRound.AttackOrderNumber);

            var availableTerritories = await gameTerritoryService
                .GetAvailableAttackTerritoriesNames(db, currentAttacker.AttackerId, currentRound.GameInstanceId, true);

            await hubContext.Clients.Group(data.GameLink)
                .ShowRoundingAttacker(currentAttacker.AttackerId,
                    GameActionsTime.GetServerActionsTime(ActionState.OPEN_PLAYER_ATTACK_VOTING), availableTerritories);

            var fullGame = await CommonTimerFunc.GetFullGameInstance(data.GameInstanceId, db);
            await hubContext.Clients.Group(data.GameLink)
                .GetGameInstance(fullGame);

            // Set next action and interval
            timerWrapper.Data.NextAction = ActionState.CLOSE_PLAYER_ATTACK_VOTING;
            timerWrapper.Interval = GameActionsTime.GetServerActionsTime(ActionState.OPEN_PLAYER_ATTACK_VOTING);

            timerWrapper.Start();
        }

        /// <summary>
        /// When a player's timeframe for attacking neutral territory expires
        /// Close off the voting for him. If he hasn't selected anything, give him a random selected territory
        /// Start next player territory select timer
        /// </summary>
        /// <returns></returns>
        public async Task Close_Neutral_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();

            var currentRound = await db.Round
                .Include(x => x.NeutralRound)
                .ThenInclude(x => x.TerritoryAttackers)
                .ThenInclude(x => x.AttackedTerritory)
                .Where(x => x.GameRoundNumber == data.CurrentGameRoundNumber && x.GameInstanceId == data.GameInstanceId)
                .FirstOrDefaultAsync();

            var currentAttacker = currentRound.NeutralRound.TerritoryAttackers
                .First(x => x.AttackOrderNumber == currentRound.NeutralRound.AttackOrderNumber);

            // Player didn't select anything, assign him a random UNSELECTED territory
            if (currentAttacker.AttackedTerritoryId == null)
            {
                var randomTerritory =
                    await gameTerritoryService.GetRandomMCTerritoryNeutral(currentAttacker.AttackerId, data.GameInstanceId);

                // Set this territory as being attacked from this person
                currentAttacker.AttackedTerritoryId = randomTerritory.Id;

                // Set the ObjectTerritory as being attacked currently
                randomTerritory.AttackedBy = currentAttacker.AttackerId;
                db.Update(randomTerritory);
            }
            var fullGame = await CommonTimerFunc.GetFullGameInstance(data.GameInstanceId, db);

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

                    var availableTerritories = await gameTerritoryService
                        .GetAvailableAttackTerritoriesNames(db, nextAttacker.AttackerId, currentRound.GameInstanceId, true);

                    // Notify the group who is the next attacker
                    await hubContext.Clients.Group(data.GameLink).ShowRoundingAttacker(nextAttacker.AttackerId,
                        GameActionsTime.GetServerActionsTime(ActionState.OPEN_PLAYER_ATTACK_VOTING), availableTerritories);

                    await hubContext.Clients.Group(data.GameLink)
                        .GetGameInstance(fullGame);

                    // Set the next timer event
                    timerWrapper.Data.NextAction = ActionState.CLOSE_PLAYER_ATTACK_VOTING;
                    timerWrapper.Interval = GameActionsTime.GetServerActionsTime(ActionState.OPEN_PLAYER_ATTACK_VOTING);

                    timerWrapper.Start();

                    break;

                // This was the last attacker, show a preview of all territories now, and then show the questions
                case 3:
                    currentRound.IsTerritoryVotingOpen = false;
                    db.Update(currentRound);
                    await db.SaveChangesAsync();

                    await hubContext.Clients.Group(data.GameLink)
                        .GetGameInstance(fullGame);


                    timerWrapper.Interval = GameActionsTime.DefaultPreviewTime;
                    timerWrapper.Data.NextAction = ActionState.SHOW_MULTIPLE_CHOICE_QUESTION;
                    timerWrapper.Start();
                    break;
                //throw new NotImplementedException();

                default:
                    throw new ArgumentException($"Unexpected attackordernumber value: {currentRound.NeutralRound.AttackOrderNumber}");
            }
        }

        public async Task Show_Neutral_MultipleChoice_Screen(TimerWrapper timerWrapper)
        {
            // Stop timer until we calculate the next action and client event
            timerWrapper.Stop();

            // Get the question and show it to the clients
            var data = timerWrapper.Data;
            var db = contextFactory.CreateDbContext();

            // Show the question to the user
            var question = await db.Questions
                .Include(x => x.Answers)
                .Include(x => x.Round)
                .ThenInclude(x => x.GameInstance)
                .ThenInclude(x => x.Participants)
                .Where(x => x.Round.GameInstanceId == data.GameInstanceId &&
                    x.Round.GameRoundNumber == x.Round.GameInstance.GameRoundNumber)
                .FirstOrDefaultAsync();


            // Open this question for voting
            question.Round.IsQuestionVotingOpen = true;
            db.Update(question.Round);
            await db.SaveChangesAsync();

            var response = mapper.Map<QuestionClientResponse>(question);

            // If the round is a neutral one, then everyone can attack
            response.IsNeutral = true;
            response.Participants = question.Round.GameInstance.Participants.ToArray();

            await hubContext.Clients.Group(data.GameLink).GetRoundQuestion(response,
                GameActionsTime.GetServerActionsTime(ActionState.SHOW_MULTIPLE_CHOICE_QUESTION));


            timerWrapper.Data.NextAction = ActionState.END_MULTIPLE_CHOICE_QUESTION;
            timerWrapper.Interval = GameActionsTime.GetServerActionsTime(ActionState.SHOW_MULTIPLE_CHOICE_QUESTION);
            timerWrapper.Start();
        }


        public async Task Close_Neutral_MultipleChoice_Question_Voting(TimerWrapper timerWrapper)
        {
            // Can disable voting on start, however even 0-1s delay wouldn't be game breaking and would ease performance
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            var db = contextFactory.CreateDbContext();

            var currentRound =
                await db.Round
                .Include(x => x.GameInstance)
                .Include(x => x.Question)
                .ThenInclude(x => x.Answers)
                .Include(x => x.NeutralRound)
                .ThenInclude(x => x.TerritoryAttackers)
                .ThenInclude(x => x.AttackedTerritory)
                .Where(x => x.GameRoundNumber == data.CurrentGameRoundNumber
                    && x.GameInstanceId == data.GameInstanceId)
                .FirstOrDefaultAsync();



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

            // Create number question rounds if gm multiple choice neutral rounds are over
            Round[] rounds = null;
            if (currentRound.GameRoundNumber == data.LastNeutralMCRound)
            {
                rounds = await Create_Neutral_Number_Rounds(db, timerWrapper);
                await db.AddRangeAsync(rounds);
            }

            // Go to next round
            timerWrapper.Data.CurrentGameRoundNumber++;
            currentRound.GameInstance.GameRoundNumber = timerWrapper.Data.CurrentGameRoundNumber;

            db.Update(currentRound);
            await db.SaveChangesAsync();

            // Request a new batch of number questions from the question service
            if (currentRound.GameRoundNumber == data.LastNeutralMCRound)
            {
                RequestQuestions(data.GameInstanceId, rounds, true);
            }

            // Response correct answer and all player answers
            var response = new PlayerQuestionAnswers()
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

            await hubContext.Clients.Groups(data.GameLink).QuestionPreviewResult(response);

            timerWrapper.Data.NextAction = ActionState.OPEN_PLAYER_ATTACK_VOTING;
            timerWrapper.Interval = GameActionsTime.DefaultPreviewTime;

            timerWrapper.Start();
        }

        private void RequestQuestions(int gameInstanceId, Round[] rounds, bool isNeutralGeneration = false)
        {
            // Request questions only for the initial multiple questions for neutral attacking order
            // After multiple choices are over, request a new batch for number questions for all untaken territories
            messageBus.RequestQuestions(new RequestQuestionsDto()
            {
                Event = "Question_Request",
                GameInstanceId = gameInstanceId,
                MultipleChoiceQuestionsRoundId = new List<int>(),
                NumberQuestionsRoundId = rounds.Select(x => x.Id).ToList(),
                IsNeutralGeneration = isNeutralGeneration,
            });
        }

        private async Task<Round[]> Create_Neutral_Number_Rounds(DefaultContext db, TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;
            var untakenTerritoriesCount = await db.ObjectTerritory
                .Where(x => x.TakenBy == null && x.GameInstanceId == data.GameInstanceId)
                .CountAsync();

            var participantsIds = await db.Participants
                .Where(x => x.GameId == data.GameInstanceId)
                .Select(x => x.PlayerId)
                .ToListAsync();

            var numberQuestionRounds = new List<Round>();
            int baseDebug = 5;
            for (var i = 0; i < untakenTerritoriesCount; i++)
            {
                var baseRound = new Round()
                {
                    // After this method executes we switch to the next round automatically thus + 1 now
                    GameRoundNumber = baseDebug + i + 1,
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

            return numberQuestionRounds.ToArray();
        }
    }
}

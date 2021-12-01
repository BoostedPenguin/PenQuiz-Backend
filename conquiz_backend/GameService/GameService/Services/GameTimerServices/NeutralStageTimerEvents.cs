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
        Task Close_Neutral_Number_Question_Voting(TimerWrapper timerWrapper);
        Task Open_Neutral_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper);
        Task Show_Game_Map_Screen(TimerWrapper timerWrapper);
        Task Show_Neutral_MultipleChoice_Screen(TimerWrapper timerWrapper);
        Task Show_Neutral_Number_Screen(TimerWrapper timerWrapper);
    }

    public class NeutralStageTimerEvents : INeutralStageTimerEvents
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        private readonly IGameTerritoryService gameTerritoryService;
        private readonly IMapper mapper;
        private readonly IMessageBusClient messageBus;
        private readonly Random r = new Random();

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

        public async Task Show_Neutral_Number_Screen(TimerWrapper timerWrapper)
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
            question.Round.QuestionOpenedAt = DateTime.Now;

            db.Update(question.Round);
            await db.SaveChangesAsync();

            var response = mapper.Map<QuestionClientResponse>(question);

            // If the round is a neutral one, then everyone can attack
            response.IsNeutral = true;
            response.Participants = question.Round.GameInstance.Participants.ToArray();

            await hubContext.Clients.Group(data.GameLink).GetRoundQuestion(response,
                GameActionsTime.GetServerActionsTime(ActionState.SHOW_NUMBER_QUESTION));


            timerWrapper.Data.NextAction = ActionState.END_NUMBER_QUESTION;
            timerWrapper.Interval = GameActionsTime.GetServerActionsTime(ActionState.SHOW_NUMBER_QUESTION);
            timerWrapper.Start();
        }

        public async Task Show_Game_Map_Screen(TimerWrapper timerWrapper)
        {
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            var db = contextFactory.CreateDbContext();

            await hubContext.Clients.Group(data.GameLink)
                .ShowGameMap(GameActionsTime.DefaultPreviewTime);

            var fullGame = await CommonTimerFunc.GetFullGameInstance(data.GameInstanceId, db);
            await hubContext.Clients.Group(data.GameLink)
                .GetGameInstance(fullGame);

            timerWrapper.Data.NextAction = ActionState.SHOW_NUMBER_QUESTION;
            timerWrapper.Interval = GameActionsTime.DefaultPreviewTime;
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

            // Debug
            if (currentRound.GameRoundNumber == 1)
            {
                currentRound.GameRoundNumber = 5;
                timerWrapper.Data.CurrentGameRoundNumber = 5;
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

            if(currentRound.GameRoundNumber == data.LastNeutralMCRound)
            {
                // Next action should be a number question related one
                timerWrapper.Data.NextAction = ActionState.SHOW_PREVIEW_GAME_MAP;
            }
            else
            {
                timerWrapper.Data.NextAction = ActionState.OPEN_PLAYER_ATTACK_VOTING;
            }
            timerWrapper.Interval = GameActionsTime.DefaultPreviewTime;

            timerWrapper.Start();
        }

        public async Task Close_Neutral_Number_Question_Voting(TimerWrapper timerWrapper)
        {
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

            var correctNumberQuestionAnswer = long.Parse(currentRound.Question.Answers.First().Answer);

            var attackerAnswers = currentRound
                .NeutralRound
                .TerritoryAttackers
                .Select(x => new
                {
                    x.AnsweredAt,
                    x.AttackerId,
                    x.AttackerNumberQAnswer,
                });

            // 2 things need to happen:
            // First check for closest answer to the correct answer
            // The closest answer wins the round
            // If 2 or more answers are same check answeredAt and the person who answered
            // The quickest wins the round

            // Use case: If no person answered, then reward the a random territory to a random person
            // (to prevent empty rounds)

            var clientResponse = new NumberPlayerQuestionAnswers()
            {
                CorrectAnswer = correctNumberQuestionAnswer.ToString(),
                PlayerAnswers = new List<NumberPlayerIdAnswer>(),
            };


            foreach(var at in attackerAnswers)
            {
                if(at.AttackerNumberQAnswer == null)
                {
                    clientResponse.PlayerAnswers.Add(new NumberPlayerIdAnswer()
                    {
                        Answer = null,
                        TimeElapsed = "",
                        PlayerId = at.AttackerId,
                    });
                    continue;
                }
                // Solution is to absolute value both of them
                // Then subtract from each other
                // Also have to make an absolute value the result

                var difference = Math.Abs(correctNumberQuestionAnswer) - Math.Abs((long)at.AttackerNumberQAnswer);
                var absoluteDifference = Math.Abs(difference);

                var timeElapsed = Math.Abs((currentRound.QuestionOpenedAt - at.AnsweredAt).Value.TotalSeconds);

                clientResponse.PlayerAnswers.Add(new NumberPlayerIdAnswer()
                {
                    Answer = at.AttackerNumberQAnswer.ToString(),
                    TimeElapsedNumber = timeElapsed,
                    TimeElapsed = timeElapsed.ToString("0.00"),
                    DifferenceWithCorrectNumber = absoluteDifference,
                    PlayerId = at.AttackerId,
                    DifferenceWithCorrect = absoluteDifference.ToString(),
                });
            }

            // After calculating all answer differences, see who is the winner

            // If all player answers are null (no one answered) give a random person a random territory
            // Will prioritize matching borders instead of totally random territories
            // Will make the map look more connected
            
            int winnerId;

            // If no player answered
            // Give out a random territory to one of them
            if(clientResponse.PlayerAnswers.All(x => x.Answer == null))
            {
                var randomWinnerIndex = r.Next(0, clientResponse.PlayerAnswers.Count());
                winnerId = clientResponse.PlayerAnswers[randomWinnerIndex].PlayerId;
            }
            else
            {
                // Order by answer first
                // Then orderby answeredat
                winnerId = clientResponse.PlayerAnswers
                    .Where(x => x.Answer != null && x.TimeElapsed != null)
                    .OrderBy(x => x.DifferenceWithCorrectNumber)
                    .ThenBy(x => x.TimeElapsedNumber)
                    .Select(x => x.PlayerId)
                    .First();
            }

            var randomTerritory = await gameTerritoryService
                .GetRandomMCTerritoryNeutral(winnerId, currentRound.GameInstanceId);

            var selectedPersonObj = currentRound
                .NeutralRound
                .TerritoryAttackers
                .First(x => x.AttackerId == winnerId);

            // Client response winner
            clientResponse.PlayerAnswers.ForEach(x => x.Winner = x.PlayerId == winnerId);
            
            // Update db
            randomTerritory.TakenBy = winnerId;
            randomTerritory.AttackedBy = null;
            selectedPersonObj.AttackerWon = true;
            selectedPersonObj.AttackedTerritoryId = randomTerritory.Id;

            // All other attackers lost
            currentRound
                .NeutralRound
                .TerritoryAttackers
                .Where(x => x.AttackerId != winnerId)
                .ToList()
                .ForEach(x => x.AttackerWon = false);

            // Go to next round
            timerWrapper.Data.CurrentGameRoundNumber++;
            currentRound.GameInstance.GameRoundNumber = timerWrapper.Data.CurrentGameRoundNumber;

            db.Update(randomTerritory);
            db.Update(currentRound);
            await db.SaveChangesAsync();

            // Tell clients
            await hubContext.Clients.Groups(data.GameLink).NumberQuestionPreviewResult(clientResponse);

            // Set next action
            timerWrapper.Data.NextAction = ActionState.SHOW_PREVIEW_GAME_MAP;
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
            for (var i = 0; i < untakenTerritoriesCount; i++)
            {
                var baseRound = new Round()
                {
                    // After this method executes we switch to the next round automatically thus + 1 now
                    GameRoundNumber = data.LastNeutralMCRound + i + 1,
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

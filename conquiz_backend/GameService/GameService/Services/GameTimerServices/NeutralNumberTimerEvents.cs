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
    public interface INeutralNumberTimerEvents
    {
        Task Show_Game_Map_Screen(TimerWrapper timerWrapper);
        Task Close_Neutral_Number_Question_Voting(TimerWrapper timerWrapper);
        Task Show_Neutral_Number_Screen(TimerWrapper timerWrapper);

        // Debug
        Task Debug_Assign_All_Territories_Start_Pvp(TimerWrapper timerWrapper);
    }

    public class NeutralNumberTimerEvents : INeutralNumberTimerEvents
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        private readonly IGameTerritoryService gameTerritoryService;
        private readonly IMapper mapper;
        private readonly IMapGeneratorService mapGeneratorService;
        private readonly IMessageBusClient messageBus;
        private readonly Random r = new Random();

        public NeutralNumberTimerEvents(IDbContextFactory<DefaultContext> _contextFactory,
            IHubContext<GameHub, IGameHub> hubContext,
            IGameTerritoryService gameTerritoryService,
            IMapper mapper,
            IMapGeneratorService mapGeneratorService,
            IMessageBusClient messageBus)
        {
            contextFactory = _contextFactory;
            this.hubContext = hubContext;
            this.gameTerritoryService = gameTerritoryService;
            this.mapper = mapper;
            this.mapGeneratorService = mapGeneratorService;
            this.messageBus = messageBus;
        }

        public async Task Show_Game_Map_Screen(TimerWrapper timerWrapper)
        {
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();

            await hubContext.Clients.Group(data.GameLink)
                .ShowGameMap();

            var fullGame = await CommonTimerFunc.GetFullGameInstance(data.GameInstanceId, db);
            var res1 = mapper.Map<GameInstanceResponse>(fullGame);

            await hubContext.Clients.Group(data.GameLink)
                .GetGameInstanceDebug(res1);

            timerWrapper.StartTimer(ActionState.SHOW_NUMBER_QUESTION);
        }

        public async Task Close_Neutral_Number_Question_Voting(TimerWrapper timerWrapper)
        {
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();
            var gm = data.GameInstance;

            var currentRound = gm.Rounds
                .Where(x => x.GameRoundNumber == data.CurrentGameRoundNumber)
                .FirstOrDefault();


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


            foreach (var at in attackerAnswers)
            {
                if (at.AttackerNumberQAnswer == null)
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
            if (clientResponse.PlayerAnswers.All(x => x.Answer == null))
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

            // Servers as a read-only replica, entity does NOT track it
            var readonlyRandomTerritory = gameTerritoryService
                .GetRandomTerritory(gm, winnerId, currentRound.GameInstanceId);


            var randomTerritory = data.GameInstance.ObjectTerritory.First(x => x.Id == readonlyRandomTerritory.Id);

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


            // Debug
            //if (currentRound.GameRoundNumber == 6)
            //{
            //    currentRound.GameRoundNumber = 22;
            //    timerWrapper.Data.CurrentGameRoundNumber = 22;
            //}

            // Go to next round
            timerWrapper.Data.CurrentGameRoundNumber++;
            currentRound.GameInstance.GameRoundNumber = timerWrapper.Data.CurrentGameRoundNumber;

            db.Update(randomTerritory);
            db.Update(currentRound);
            await db.SaveChangesAsync();


            // Request a new batch of number questions from the question service

            if (data.CurrentGameRoundNumber > data.LastNeutralNumberRound)
            {
                // Create pvp question rounds if gm number neutral rounds are over
                var rounds = Create_Pvp_Rounds(gm, timerWrapper, currentRound.NeutralRound.TerritoryAttackers.Select(x => x.AttackerId).ToList());


                await db.AddRangeAsync(rounds);
                await db.SaveChangesAsync();

                data.LastPvpRound = db.Round
                    .Where(x => x.GameInstanceId == data.GameInstanceId)
                    .OrderByDescending(x => x.GameRoundNumber)
                    .Select(x => x.GameRoundNumber)
                    .First();

                CommonTimerFunc.RequestQuestions(messageBus, data.GameGlobalIdentifier, rounds, false);
            }

            // Tell clients
            await hubContext.Clients.Groups(data.GameLink).NumberQuestionPreviewResult(clientResponse);


            if (data.CurrentGameRoundNumber > data.LastNeutralNumberRound)
            {
                // Next action should be a pvp question related one
                timerWrapper.StartTimer(ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING);
            }
            else
            {
                timerWrapper.StartTimer(ActionState.SHOW_PREVIEW_GAME_MAP);
            }
        }

        public async Task Show_Neutral_Number_Screen(TimerWrapper timerWrapper)
        {
            // Stop timer until we calculate the next action and client event
            timerWrapper.Stop();

            // Get the question and show it to the clients
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();


            var gm = data.GameInstance;


            // Show the question to the user
            var roundQuestion = gm.Rounds
                .First(e => e.GameRoundNumber == e.GameInstance.GameRoundNumber);



            if (roundQuestion.Question == null)
                throw new ArgumentException($"There was no question generated for gameinstanceid: {data.GameInstanceId}, gameroundnumber: {data.CurrentGameRoundNumber}.");


            // Open this question for voting
            roundQuestion.IsQuestionVotingOpen = true;
            roundQuestion.QuestionOpenedAt = DateTime.Now;

            db.Update(roundQuestion);
            await db.SaveChangesAsync();

            var response = mapper.Map<QuestionClientResponse>(roundQuestion.Question);

            // If the round is a neutral one, then everyone can attack
            response.IsNeutral = true;
            var participantsMapping = mapper.Map<ParticipantsResponse[]>(roundQuestion.GameInstance.Participants.ToArray());

            response.Participants = participantsMapping;

            await hubContext.Clients.Group(data.GameLink).GetRoundQuestion(response);

            timerWrapper.StartTimer(ActionState.END_NUMBER_QUESTION);
        }

        public async Task Debug_Assign_All_Territories_Start_Pvp(TimerWrapper timerWrapper)
        {
            timerWrapper.Stop();
            using var db = contextFactory.CreateDbContext();

            var data = timerWrapper.Data;
            var gm = data.GameInstance;

            var particip = gm.Participants.ToList();

            var untakenTer = gm.ObjectTerritory.Where(x => x.TakenBy == null).ToList();

            for(var i = 0; i < 5; i++)
            {
                untakenTer.First(x => x.TakenBy == null).TakenBy = particip[0].PlayerId;
                untakenTer.First(x => x.TakenBy == null).TakenBy = particip[1].PlayerId;
            }

            untakenTer.ForEach(x =>
            {
                if (x.TakenBy != null) return;
                x.TakenBy = particip[2].PlayerId;
            });

            data.CurrentGameRoundNumber = 40;
            gm.GameRoundNumber = 41;

            var rounds = Create_Pvp_Rounds(gm, timerWrapper, gm.Participants.Select(x => x.PlayerId).ToList());

            foreach (var round in rounds)
            {
                // Multiple
                round.Question = new Questions()
                {
                    Question = "When was bulgaria created?",
                    Type = "multiple",
                };
                round.Question.Answers.Add(new Answers()
                {
                    Correct = true,
                    Answer = "681",
                });
                round.Question.Answers.Add(new Answers()
                {
                    Correct = false,
                    Answer = "15",
                });
                round.Question.Answers.Add(new Answers()
                {
                    Correct = false,
                    Answer = "22",
                });
                round.Question.Answers.Add(new Answers()
                {
                    Correct = false,
                    Answer = "512",
                });

                // Number
                round.PvpRound.NumberQuestion = new Questions()
                {
                    Question = "When was covid discovered?",
                    Type = "number"
                };
                round.PvpRound.NumberQuestion.Answers.Add(new Answers()
                {
                    Correct = true,
                    Answer = "2019"
                });
            }

            data.CurrentGameRoundNumber = 55;
            gm.GameRoundNumber = 56;

            data.CurrentGameRoundNumber++;
            await db.AddRangeAsync(rounds);
            db.Update(gm);
            await db.SaveChangesAsync();


            data.LastPvpRound = db.Round
                .Where(x => x.GameInstanceId == data.GameInstanceId)
                .OrderByDescending(x => x.GameRoundNumber)
                .Select(x => x.GameRoundNumber)
                .First();

            timerWrapper.StartTimer(ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING);
        }

        private Round[] Create_Pvp_Rounds(GameInstance gm, TimerWrapper timerWrapper, List<int> userIds)
        {
            int RequiredPlayers = 3;
            var data = timerWrapper.Data;

            var totalTerritories = mapGeneratorService.GetAmountOfTerritories(gm);

            var order = CommonTimerFunc.GenerateAttackOrder(userIds, totalTerritories, RequiredPlayers, false);

            var finalRounds = new List<Round>();

            var roundCounter = data.CurrentGameRoundNumber;

            foreach (var fullRound in order.UserRoundAttackOrders)
            {
                foreach (var roundAttackerId in fullRound)
                {
                    var baseRound = new Round()
                    {
                        GameInstanceId = data.GameInstanceId,
                        GameRoundNumber = roundCounter++,
                        AttackStage = AttackStage.MULTIPLE_PVP,
                        Description = $"MultipleChoice question. Attacker vs PVP territory",
                        IsQuestionVotingOpen = false,
                        IsTerritoryVotingOpen = false,
                    };
                    baseRound.PvpRound = new PvpRound()
                    {
                        AttackerId = roundAttackerId,
                    };

                    finalRounds.Add(baseRound);
                }
            }

            var result = finalRounds.ToArray();

            return result;
        }
    }
}

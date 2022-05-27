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

namespace GameService.Services.GameTimerServices.NeutralTimerServices
{
    public interface INeutralNumberTimerEvents
    {
        Task Show_Game_Map_Screen(TimerWrapper timerWrapper);
        Task Close_Neutral_Number_Question_Voting(TimerWrapper timerWrapper);
        Task Show_Neutral_Number_Screen(TimerWrapper timerWrapper);

        // Debug
        Task Debug_Assign_All_Territories_Start_Pvp(TimerWrapper timerWrapper);
    }

    public partial class NeutralNumberTimerEvents : INeutralNumberTimerEvents
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        private readonly IGameTerritoryService gameTerritoryService;
        private readonly IMapper mapper;
        private readonly ICurrentStageQuestionService dataExtractionService;
        private readonly IMapGeneratorService mapGeneratorService;
        private readonly IMessageBusClient messageBus;
        private readonly Random r = new();

        public NeutralNumberTimerEvents(IDbContextFactory<DefaultContext> _contextFactory,
            IHubContext<GameHub, IGameHub> hubContext,
            IGameTerritoryService gameTerritoryService,
            IMapper mapper,
            ICurrentStageQuestionService dataExtractionService,
            IMapGeneratorService mapGeneratorService,
            IMessageBusClient messageBus)
        {
            contextFactory = _contextFactory;
            this.hubContext = hubContext;
            this.gameTerritoryService = gameTerritoryService;
            this.mapper = mapper;
            this.dataExtractionService = dataExtractionService;
            this.mapGeneratorService = mapGeneratorService;
            this.messageBus = messageBus;
        }

        public async Task Show_Game_Map_Screen(TimerWrapper timerWrapper)
        {
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            var gm = data.GameInstance;

            await hubContext.Clients.Group(data.GameLink)
                .ShowGameMap();

            var res1 = mapper.Map<GameInstanceResponse>(gm);

            await hubContext.Clients.Group(data.GameLink)
                .GetGameInstance(res1);

            timerWrapper.StartTimer(ActionState.SHOW_NUMBER_QUESTION);
        }

        public async Task Close_Neutral_Number_Question_Voting(TimerWrapper timerWrapper)
        {
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();
            var gm = data.GameInstance;


            var currentRound = data.GetBaseRound;


            currentRound.IsQuestionVotingOpen = false;

            var correctNumberQuestionAnswer = long.Parse(currentRound.Question.Answers.First().Answer);

            var attackerAnswers = currentRound
                .NeutralRound
                .TerritoryAttackers;

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
                // Check if the current attacker is a bot
                // Handle bot answer
                var isThisPlayerBot = gm.Participants.First(e => e.PlayerId == at.AttackerId).Player.IsBot;
                if (isThisPlayerBot)
                {
                    at.AnsweredAt = DateTime.Now;
                    at.AttackerNumberQAnswer = BotService.GenerateBotNumberAnswer(correctNumberQuestionAnswer);
                }

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
            currentRound.GameInstance.GameRoundNumber++;

            db.Update(gm);
            await db.SaveChangesAsync();

            CommonTimerFunc.CalculateUserScore(gm);


            // Request a new batch of number questions from the question service

            if (gm.GameRoundNumber > data.LastNeutralNumberRound)
            {
                // Create pvp question rounds if gm number neutral rounds are over
                var rounds = Create_Pvp_Rounds(gm, currentRound.NeutralRound.TerritoryAttackers.Select(x => x.AttackerId).ToList());

                rounds.ForEach(e => gm.Rounds.Add(e));

                db.Update(gm);
                await db.SaveChangesAsync();

                data.LastPvpRound = gm.Rounds.OrderByDescending(e => e.GameRoundNumber).Select(e => e.GameRoundNumber).First();

                CommonTimerFunc.RequestQuestions(messageBus, data.GameGlobalIdentifier, rounds, false);
            }

            // Tell clients
            await hubContext.Clients.Groups(data.GameLink).NumberQuestionPreviewResult(clientResponse);


            if (gm.GameRoundNumber > data.LastNeutralNumberRound)
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


            var response = dataExtractionService.GetCurrentStageQuestionResponse(gm);


            // Open this question for voting
            roundQuestion.IsQuestionVotingOpen = true;
            roundQuestion.QuestionOpenedAt = DateTime.Now;

            db.Update(gm);
            await db.SaveChangesAsync();

            await hubContext.Clients.Group(data.GameLink).GetRoundQuestion(response);

            timerWrapper.StartTimer(ActionState.END_NUMBER_QUESTION);
        }
    }
}

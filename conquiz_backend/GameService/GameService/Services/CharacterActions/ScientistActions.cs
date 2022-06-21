using AutoMapper;
using GameService.Data;
using GameService.Data.Models;
using GameService.Hubs;
using GameService.MessageBus;
using GameService.Services.Extensions;
using GameService.Services.GameTimerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace GameService.Services.CharacterActions
{
    public class ScientistActions
    {
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IMessageBusClient messageBusClient;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IGameTimerService gameTimerService;
        private readonly ICurrentStageQuestionService currentStageQuestionService;
        private readonly IMapper mapper;
        private readonly Random random = new();

        public ScientistActions(IHubContext<GameHub, IGameHub> hubContext,
            IDbContextFactory<DefaultContext> contextFactory,
            IMessageBusClient messageBusClient,
            IHttpContextAccessor httpContextAccessor,
            IGameTimerService gameTimerService,
            ICurrentStageQuestionService currentStageQuestionService,
            IMapper mapper)
        {
            this.hubContext = hubContext;
            this.contextFactory = contextFactory;
            this.messageBusClient = messageBusClient;
            this.httpContextAccessor = httpContextAccessor;
            this.gameTimerService = gameTimerService;
            this.currentStageQuestionService = currentStageQuestionService;
            this.mapper = mapper;
        }


        public void UseNumberHint()
        {
            // Get the current game instance
            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();

            var playerGameTimer = gameTimerService.GameTimers.FirstOrDefault(e =>
                e.Data.GameInstance.Participants.FirstOrDefault(e => e.Player.UserGlobalIdentifier == globalUserId) is not null);

            if (playerGameTimer == null)
                throw new GameException("There is no open game where this player participates");

            var gm = playerGameTimer.Data.GameInstance;

            var currentRound = gm.Rounds
                .Where(x =>
                    x.GameRoundNumber == x.GameInstance.GameRoundNumber)
                .FirstOrDefault();

            // If not multiple choice round return
            if (currentRound.AttackStage != AttackStage.NUMBER_NEUTRAL && currentRound.AttackStage != AttackStage.NUMBER_PVP)
                throw new System.ArgumentException("Round type is not number choice");

            // If capital round, but it's number, return
            if (currentRound.PvpRound?.IsCurrentlyCapitalStage == true && currentRound
                    .PvpRound
                    .CapitalRounds
                    .FirstOrDefault(x => !x.IsCompleted && x.IsQuestionVotingOpen).CapitalRoundAttackStage == CapitalRoundAttackStage.MULTIPLE_CHOICE_QUESTION)
                throw new ArgumentException("Capital MC round. Should be number.");

            var question = currentStageQuestionService.GetCurrentStageQuestion(gm);

            var participant = gm.Participants.First(e => e.Player.UserGlobalIdentifier == httpContextAccessor.GetCurrentUserGlobalId());


            if (participant.GameCharacter.CharacterAbilities is not ScientistCharacterAbilities scientistAbilities)
                throw new ArgumentException($"Character is {participant.GameCharacter.CharacterAbilities.CharacterType}, but ScientistCharacter is expected");

            if (scientistAbilities.AbilityUsedInRounds.Any(e => e == currentRound.Id))
                throw new ArgumentException("Cannot use the scientist ability more than once per round!");

            if (!scientistAbilities.IsNumberHintsAvailable)
                throw new ArgumentException("Number hints use are maxed.");


            // Update wizard abilities data
            scientistAbilities.NumberQuestionHintUseCount++;
            scientistAbilities.AbilityUsedInRounds.Add(currentRound.Id);

            var correctAnswer = question.Answers.First(e => e.Correct);

        }

        private class ScientistHint
        {
            public string MinRange { get; set; }
            public string MaxRange { get; set; }
        }
        private ScientistHint GenerateScientistHint(long correctAnswer)
        {
            // For the max range we can add x2 the correct answer as top range margin
            // For the min range we can divide by 2 the correct answer as bot range margin

            // For very high correct answers  (70 000), adding or subtracting twice the margin would
            // Not be very helpful
            // Need a threshhold for when the answer becomes "too big"
            
            // For year answers, presumably between 1500-2022, we can 


            // Presumed year answer
            if(correctAnswer >= 1300 && correctAnswer <= 2022)
            {
                var topAnswerMargin = 2022;

                var topAnswer = random.NextInt64(correctAnswer, correctAnswer + 50 >= topAnswerMargin 
                    ? topAnswerMargin : correctAnswer + 50);

                var botAnswer = random.NextInt64(correctAnswer - 50, correctAnswer);

                return new ScientistHint()
                {
                    MaxRange = topAnswer.ToString(),
                    MinRange = botAnswer.ToString(),
                };
            }

            // If not presumed year

            // 40-80 ~~ 60
            // 20-40 ~~ 30

            // 5000-10000 ~~ 7500
            // 2500-5000 ~~ 3500

            // 15000-30000 ~~ 25000
            // 7500-15000 ~~ 10000

            return new ScientistHint()
            {
                MaxRange = random.NextInt64(correctAnswer * 2, correctAnswer).ToString(),
                MinRange = random.NextInt64(correctAnswer / 2, correctAnswer).ToString(),
            };
        }
    }
}

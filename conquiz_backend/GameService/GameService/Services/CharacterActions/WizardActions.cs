using AutoMapper;
using GameService.Data;
using GameService.Data.Models;
using GameService.Dtos.SignalR_Responses;
using GameService.Hubs;
using GameService.MessageBus;
using GameService.Services.Extensions;
using GameService.Services.GameTimerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.CharacterActions
{
    public interface IWizardActions
    {
        WizardUseMultipleChoiceHint UseMultipleChoiceHint();
    }

    public class WizardActions : IWizardActions
    {
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        private readonly IMapper mapper;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IGameTimerService gameTimerService;
        private readonly ICurrentStageQuestionService currentStageQuestionService;
        private readonly static Random r = new();

        public WizardActions(
            IHubContext<GameHub, IGameHub> hubContext,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor, 
            IGameTimerService gameTimerService,
            ICurrentStageQuestionService currentStageQuestionService
            )
        {
            this.hubContext = hubContext;
            this.mapper = mapper;
            this.httpContextAccessor = httpContextAccessor;
            this.gameTimerService = gameTimerService;
            this.currentStageQuestionService = currentStageQuestionService;
        }

        public WizardUseMultipleChoiceHint UseMultipleChoiceHint()
        {
            // Get the character
            // Check if he can use choice hints
            // Get the original question asked (only if multiple choice)
            // Send a message to the client with 1 correct value and 1 wrong one

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
            if (currentRound.AttackStage != AttackStage.MULTIPLE_NEUTRAL && currentRound.AttackStage != AttackStage.MULTIPLE_PVP)
                throw new ArgumentException("Round type is not multiple choice");

            // If capital round, but it's number, return
            if (currentRound.PvpRound?.IsCurrentlyCapitalStage == true && currentRound
                    .PvpRound
                    .CapitalRounds
                    .FirstOrDefault(x => !x.IsCompleted && x.IsQuestionVotingOpen).CapitalRoundAttackStage == CapitalRoundAttackStage.NUMBER_QUESTION)
                throw new ArgumentException("Capital number round. Should be multiple choice.");

            var question = currentStageQuestionService.GetCurrentStageQuestion(gm);

            var participant = gm.Participants.First(e => e.Player.UserGlobalIdentifier == httpContextAccessor.GetCurrentUserGlobalId());


            if (participant.GameCharacter.CharacterAbilities is not WizardCharacterAbilities wizardAbilities)
                throw new ArgumentException($"Character is {participant.GameCharacter.CharacterAbilities.CharacterType}, but WizardCharacter is expected");

            if (wizardAbilities.AbilityUsedInRounds.Any(e => e == currentRound.Id))
                throw new ArgumentException("Cannot use the wizard ability more than once per round!");

            if (!wizardAbilities.IsMCHintsAvailable)
                throw new ArgumentException("Multiple choice hints use are maxed.");


            // Update wizard abilities data
            wizardAbilities.MCQuestionHintUseCount++;
            wizardAbilities.AbilityUsedInRounds.Add(currentRound.Id);

            var correct = question.Answers.FirstOrDefault(x => x.Correct);

            var wrongAnswers = question.Answers.Where(x => !x.Correct).ToArray();

            var wrong = wrongAnswers[r.Next(wrongAnswers.Length)];



            var questionResponse = currentStageQuestionService.GetCurrentStageQuestionResponse(gm);
            var response = new WizardUseMultipleChoiceHint()
            {
                PlayerId = participant.PlayerId,
                Answers = new List<AnswerClientResponse>()
                {
                     mapper.Map<AnswerClientResponse>(correct),
                     mapper.Map<AnswerClientResponse>(wrong),
                },
                GameLink = gm.InvitationLink,
                QuestionResponse = questionResponse,
            };


            return response;
        }
    }
}

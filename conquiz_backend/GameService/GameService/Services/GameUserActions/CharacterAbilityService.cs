using GameService.Data.Models;
using GameService.Dtos.SignalR_Responses;
using GameService.Services.CharacterActions;
using GameService.Services.Extensions;
using GameService.Services.GameTimerServices;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.GameUserActions
{
    public interface ICharacterAbilityService
    {
        Task<VikingUseFortifyResponse> VikingUseAbility();
        WizardUseMultipleChoiceHint WizardUseAbility();
    }

    public class CharacterAbilityService : ICharacterAbilityService
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IGameTimerService gameTimerService;
        private readonly IVikingActions vikingActions;
        private readonly ICurrentStageQuestionService currentStageQuestionService;
        private readonly IWizardActions wizardActions;

        public CharacterAbilityService(IHttpContextAccessor httpContextAccessor, IGameTimerService gameTimerService, 
            IVikingActions vikingActions,
            ICurrentStageQuestionService currentStageQuestionService, IWizardActions wizardActions)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.gameTimerService = gameTimerService;
            this.vikingActions = vikingActions;
            this.currentStageQuestionService = currentStageQuestionService;
            this.wizardActions = wizardActions;
        }

        public async Task<VikingUseFortifyResponse> VikingUseAbility()
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

            var currentParticipant = gm.Participants.First(e => e.Player.UserGlobalIdentifier == globalUserId);

            if (currentRound.PvpRound == null)
                throw new GameException("This is not a pvp round");

            if (!currentRound.PvpRound.AttackedTerritory.IsCapital)
                throw new GameException("Pvp Round isn't capital");


            await vikingActions.UseFortifyCapital(currentParticipant, gm, currentRound);

            var res = currentStageQuestionService.GetCurrentStageQuestionResponse(gm);

            return new VikingUseFortifyResponse()
            {
                QuestionResponse = res,
                UsedInRoundId = currentRound.Id,
            };
        }


        public WizardUseMultipleChoiceHint WizardUseAbility()
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
            if (currentRound.AttackStage != AttackStage.MULTIPLE_NEUTRAL && currentRound.AttackStage != AttackStage.MULTIPLE_PVP)
                throw new ArgumentException("Round type is not multiple choice");

            // If capital round, but it's number, return
            if (currentRound.PvpRound?.IsCurrentlyCapitalStage == true && currentRound
                    .PvpRound
                    .CapitalRounds
                    .FirstOrDefault(x => !x.IsCompleted && x.IsQuestionVotingOpen).CapitalRoundAttackStage == CapitalRoundAttackStage.NUMBER_QUESTION)
                throw new ArgumentException("Capital number round. Should be multiple choice.");

            var currentRoundQuestion = currentStageQuestionService.GetCurrentStageQuestion(gm);

            var participant = gm.Participants.First(e => e.Player.UserGlobalIdentifier == httpContextAccessor.GetCurrentUserGlobalId());

            var res = wizardActions
                .UseMultipleChoiceHint(currentRoundQuestion, participant, gm.InvitationLink);

            return res;

        }
    }
}

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
        ScientistUseNumberHintResponse ScientistUseNumberHint();
        Task<VikingUseFortifyResponse> VikingUseAbility();
        WizardUseMultipleChoiceHint WizardUseAbility();
    }

    public class CharacterAbilityService : ICharacterAbilityService
    {
        private readonly IVikingActions vikingActions;
        private readonly IScientistActions scientistActions;
        private readonly IWizardActions wizardActions;

        public CharacterAbilityService(
            IVikingActions vikingActions,
            IScientistActions scientistActions,
            IWizardActions wizardActions)
        {
            this.vikingActions = vikingActions;
            this.scientistActions = scientistActions;
            this.wizardActions = wizardActions;
        }

        public async Task<VikingUseFortifyResponse> VikingUseAbility()
        {
            return await vikingActions.UseFortifyCapital();
        }


        public WizardUseMultipleChoiceHint WizardUseAbility()
        {
            return wizardActions
                .UseMultipleChoiceHint();
        }

        public ScientistUseNumberHintResponse ScientistUseNumberHint()
        {
            return scientistActions.UseNumberHint();
        }
    }
}

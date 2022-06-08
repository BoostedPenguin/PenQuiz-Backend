using GameService.Context;
using GameService.Data;
using GameService.Data.Models;
using GameService.Dtos.SignalR_Responses;
using GameService.Services.Extensions;
using GameService.Services.GameTimerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.GameUserActions
{
    public interface IGameControlService
    {
        void AnswerQuestion(string answerIdString);
        SelectedTerritoryResponse SelectTerritory(string mapTerritoryName);
    }

    /// <summary>
    /// Handles the user actions, used for top level actions
    /// Per action has it's own services
    /// </summary>
    public class GameControlService : IGameControlService
    {
        private readonly ICharacterAbilityService characterAbilityService;
        private readonly IAnswerQuestionService answerQuestionService;
        private readonly ITerritorySelectionService territorySelectionService;
        public GameControlService(
            ICharacterAbilityService characterAbilityService,
            IAnswerQuestionService answerQuestionService,
            ITerritorySelectionService territorySelectionService)
        {
            this.characterAbilityService = characterAbilityService;
            this.answerQuestionService = answerQuestionService;
            this.territorySelectionService = territorySelectionService;
        }

        public void AnswerQuestion(string answerIdString)
        {
            answerQuestionService.AnswerQuestion(answerIdString);
        }

        public void WizardUseAbility()
        {
            characterAbilityService.WizardUseAbility();
        }

        public SelectedTerritoryResponse SelectTerritory(string mapTerritoryName)
        {
            return territorySelectionService.SelectTerritory(mapTerritoryName);
        }
    }
}

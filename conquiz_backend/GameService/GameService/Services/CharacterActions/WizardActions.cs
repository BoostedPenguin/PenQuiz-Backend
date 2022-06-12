using AutoMapper;
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

namespace GameService.Services.CharacterActions
{
    public interface IWizardActions
    {
        Task GetAvailableMultipleChoiceHints(Participants participant, string invitationLink);
        WizardUseMultipleChoiceHint UseMultipleChoiceHint(Questions question, Participants participant, string invitationLink);
    }

    public class WizardActions : IWizardActions
    {
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        private readonly IMapper mapper;
        private readonly static Random r = new();

        public WizardActions(
            IHubContext<GameHub, IGameHub> hubContext,
            IMapper mapper
            )
        {
            this.hubContext = hubContext;
            this.mapper = mapper;
        }

        public WizardUseMultipleChoiceHint UseMultipleChoiceHint(Questions question, Participants participant, string invitationLink)
        {
            // Get the character
            // Check if he can use choice hints
            // Get the original question asked (only if multiple choice)
            // Send a message to the client with 1 correct value and 1 wrong one

            if (participant.GameCharacter.CharacterAbilities is not WizardCharacterAbilities wizardAbilities)
                throw new ArgumentException($"Character is {participant.GameCharacter.CharacterAbilities.CharacterType}, but WizardCharacter is expected");


            if (!wizardAbilities.IsMCHintsAvailable)
                throw new ArgumentException("Multiple choice hints use are maxed.");

            wizardAbilities.MCQuestionHintUseCount++;


            var correct = question.Answers.FirstOrDefault(x => x.Correct);

            var wrongAnswers = question.Answers.Where(x => !x.Correct).ToArray();

            var wrong = wrongAnswers[r.Next(wrongAnswers.Length)];


            var response = new WizardUseMultipleChoiceHint()
            {
                PlayerId = participant.PlayerId,
                Answers = new List<AnswerClientResponse>()
                {
                     mapper.Map<AnswerClientResponse>(correct),
                     mapper.Map<AnswerClientResponse>(wrong),
                },
                GameLink = invitationLink
            };


            return response;
        }

        public async Task GetAvailableMultipleChoiceHints(Participants participant, string invitationLink)
        {
            // Get the character
            // Check if he can use choice hints
            // Send a message to the client with the available count


            var wizardAbilities = participant.GameCharacter.CharacterAbilities as WizardCharacterAbilities;

            var totalLeftUses = wizardAbilities.MCQuestionHintMaxUseCount - wizardAbilities.MCQuestionHintUseCount;

            await hubContext.Clients.Group(invitationLink)
                .WizardGetAbilityUsesLeft(totalLeftUses);
        }
    }
}

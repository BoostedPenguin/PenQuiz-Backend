using AutoMapper;
using GameService.Data;
using GameService.Data.Models;
using GameService.Dtos.SignalR_Responses;
using GameService.Hubs;
using GameService.MessageBus;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.CharacterActions
{
    public interface IWizardActions
    {
        Task GetAvailableMultipleChoiceHints(Participants participant, string invitationLink);
        Task UseMultipleChoiceHint(Questions question, Participants participant, string invitationLink);
    }

    public class WizardActions : IWizardActions
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        private readonly IMapper mapper;

        public WizardActions(IDbContextFactory<DefaultContext> contextFactory,
            IHubContext<GameHub, IGameHub> hubContext,
            IMapper mapper
            )
        {
            this.contextFactory = contextFactory;
            this.hubContext = hubContext;
            this.mapper = mapper;
        }
        static Random r = new Random();

        public async Task UseMultipleChoiceHint(Questions question, Participants participant, string invitationLink)
        {
            // Get the character
            // Check if he can use choice hints
            // Get the original question asked (only if multiple choice)
            // Send a message to the client with 1 correct value and 1 wrong one

            var wizardAbilities = participant.GameCharacter.CharacterAbilities as WizardCharacterAbilities;

            if (!wizardAbilities.IsMCHintsAvailable) return;

            wizardAbilities.MCQuestionHintUseCount++;


            var correct = question.Answers.FirstOrDefault(x => x.Correct);

            var wrongAnswers = question.Answers.Where(x => !x.Correct).ToArray();

            var wrong = wrongAnswers[r.Next(wrongAnswers.Length)];


            var response = new WizardUseMultipleChoiceHint()
            {
                Answers = new System.Collections.Generic.List<AnswerClientResponse>()
                {
                     mapper.Map<AnswerClientResponse>(correct),
                     mapper.Map<AnswerClientResponse>(wrong),
                }
            };



            await hubContext.Clients.Group(invitationLink).WizardUseMultipleChoiceHint(response);
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

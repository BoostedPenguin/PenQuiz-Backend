using AutoMapper;
using GameService.Data;
using GameService.Data.Models;
using GameService.Hubs;
using GameService.MessageBus;
using GameService.Services.GameTimerServices;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.CharacterActions
{
    public class VikingActions
    {
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        private readonly IMessageBusClient messageBusClient;
        private readonly IMapper mapper;

        public VikingActions(IHubContext<GameHub, IGameHub> hubContext,
            IMessageBusClient messageBusClient,
            IMapper mapper)
        {
            this.hubContext = hubContext;
            this.messageBusClient = messageBusClient;
            this.mapper = mapper;
        }

        public async Task UseFortifyCapital(Participants participant, 
            GameInstance gameInstance,
            Round currentRound, 
            DefaultContext db)
        {
            if (participant.GameCharacter.CharacterAbilities is not VikingCharacterAbilities vikingAbilities)
                throw new ArgumentException($"Character is {participant.GameCharacter.CharacterAbilities.CharacterType}, but VikingCharacter is expected");

            if (!vikingAbilities.IsFortifyCapitalAvailable)
                throw new ArgumentException("Viking character has reached the max number of fortify capitals");


            currentRound.PvpRound.CapitalRounds.Add(new CapitalRound());

            db.Update(gameInstance);
            await db.SaveChangesAsync();

            // Request questions for these rounds
            CommonTimerFunc.RequestCapitalQuestions(messageBusClient,
                gameInstance.GameGlobalIdentifier,
                currentRound.PvpRound.CapitalRounds.Select(x => x.Id).ToList());


            vikingAbilities.FortifyCapitalUseCount++;
        }

        public async Task GetAvailableFortifyCapitalUses(Participants participant, string invitationLink)
        {
            // Get the character
            // Check if he can use fortify capital
            // Send a message to the client with the available count


            var vikingAbilities = participant.GameCharacter.CharacterAbilities as VikingCharacterAbilities;

            var totalLeftUses = vikingAbilities.FortifyCapitalMaxUseCount - vikingAbilities.FortifyCapitalUseCount;

            await hubContext.Clients.Group(invitationLink)
                .VikingGetAbilityUsesLeft(totalLeftUses);
        }
    }
}

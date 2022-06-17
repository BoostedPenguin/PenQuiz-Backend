using AutoMapper;
using GameService.Data;
using GameService.Data.Models;
using GameService.Hubs;
using GameService.MessageBus;
using GameService.Services.GameTimerServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.CharacterActions
{
    public interface IVikingActions
    {
        Task GetAvailableFortifyCapitalUses(Participants participant, string invitationLink);
        Task UseFortifyCapital(Participants participant, GameInstance gameInstance, Round currentRound);
    }

    public class VikingActions : IVikingActions
    {
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IMessageBusClient messageBusClient;
        private readonly IMapper mapper;

        public VikingActions(IHubContext<GameHub, IGameHub> hubContext,
            IDbContextFactory<DefaultContext> contextFactory,
            IMessageBusClient messageBusClient,
            IMapper mapper)
        {
            this.hubContext = hubContext;
            this.contextFactory = contextFactory;
            this.messageBusClient = messageBusClient;
            this.mapper = mapper;
        }

        public async Task UseFortifyCapital(Participants participant,
            GameInstance gameInstance,
            Round currentRound)
        {
            using var db = contextFactory.CreateDbContext();

            if (participant.GameCharacter.CharacterAbilities is not VikingCharacterAbilities vikingAbilities)
                throw new ArgumentException($"Character is {participant.GameCharacter.CharacterAbilities.CharacterType}, but VikingCharacter is expected");

            if (!vikingAbilities.IsFortifyCapitalAvailable)
                throw new ArgumentException("Viking character has reached the max number of fortify capitals");

            // Without using the viking ability the max capital rounds would be 1
            // Therefore, we can assume that if there is more than 1, then this person used it in this round

            if (currentRound.PvpRound.CapitalRounds.Count > 1)
                throw new ArgumentException("This person already used his viking ability this round.");


            var extraCapitalRound = new CapitalRound();
            currentRound.PvpRound.CapitalRounds.Add(extraCapitalRound);

            db.Update(gameInstance);
            await db.SaveChangesAsync();

            // Request questions for these rounds
            CommonTimerFunc.RequestCapitalQuestions(messageBusClient,
                gameInstance.GameGlobalIdentifier, new List<int>()
                {
                    extraCapitalRound.Id,
                });


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

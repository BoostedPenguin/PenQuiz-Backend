﻿using GameService.Data.Models;
using GameService.Dtos.SignalR_Responses;
using GameService.Hubs;
using GameService.Services.GameTimerServices;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace GameService.Services.GameLobbyServices
{
    public enum GameLobbyParticipantCharacterStatus
    {
        SELECTED,
        UNSELECTED,
        LOCKED
    }

    public class ParticipantCharacter
    {
        public int PlayerId { get; set; }
        public int[] OwnedCharacterIds { get; set; }
        public int CharacterId { get; set; }
        public GameLobbyParticipantCharacterStatus ParticipantCharacterStatus { get; set; }
    }

    public class GameLobbyTimer : Timer
    {
        public GameLobbyTimer(string gameCode, int[] allCharacterIds, int creatorPlayerId, int[] creatorOwnedCharacterIds, IHubContext<GameHub, IGameHub> hubContext)
        {
            GameLobbyData = new GameLobbyData(gameCode, allCharacterIds);
            
            GameLobbyData.AddInitialParticipant(creatorPlayerId, creatorOwnedCharacterIds);

            CountDownTimer = new CountDownTimer(hubContext, gameCode);
        }
        public GameLobbyData GameLobbyData { get; set; }
        public CountDownTimer CountDownTimer { get; set; }


        public void StartTimer(int overrideMsInterval = 0)
        {
            var interval = overrideMsInterval == 0 ? 10000 : overrideMsInterval;
            this.Interval = interval;

            CountDownTimer.StartCountDownTimer(interval);
            this.Start();
        }
    }

    public class GameLobbyData
    {
        public GameLobbyData(string gameCode, int[] allCharacterIds)
        {
            GameCode = gameCode;
            this.allCharacterIds = allCharacterIds;
        }



        private readonly int[] allCharacterIds;
        public string GameCode { get; set; }


        private List<ParticipantCharacter> ParticipantCharacters { get; set; } = new List<ParticipantCharacter>();


        public void AddInitialParticipant(int playerId, int[] ownedCharacterIds)
        {
            if (ParticipantCharacters.FirstOrDefault(e => e.PlayerId == playerId) != null)
                throw new ArgumentException("This user is already in the list for this lobby");

            ParticipantCharacters.Add(new ParticipantCharacter()
            {
                PlayerId = playerId,
                OwnedCharacterIds = ownedCharacterIds,
                ParticipantCharacterStatus = GameLobbyParticipantCharacterStatus.UNSELECTED,
            });

            if (ParticipantCharacters.Count > 3)
                throw new ArgumentException("Error! More than 3 people are in the game lobby data!");
        }

        public void RemoveParticipant(int playerId)
        {

            var participantCharacter = ParticipantCharacters.FirstOrDefault(e => e.PlayerId == playerId);

            if (participantCharacter == null)
                throw new ArgumentException("This person does not exist in the lobby!");

            ParticipantCharacters.Remove(participantCharacter);

        }

        public void ParticipantSelectCharacter(int playerId, int characterId)
        {
            if (!allCharacterIds.Contains(characterId))
                throw new ArgumentException("The selected character id does not exist in the system!");

            var participantCharacter = ParticipantCharacters.FirstOrDefault(e => e.PlayerId == playerId);

            if (participantCharacter == null)
                throw new ArgumentException("This person does not exist in the lobby!");

            if (participantCharacter.ParticipantCharacterStatus == GameLobbyParticipantCharacterStatus.LOCKED)
                throw new ArgumentException("This person has already locked his character! You can't change it anymore!");

            if (ParticipantCharacters.FirstOrDefault(e => e.CharacterId == characterId && playerId != e.PlayerId) != null)
                throw new ArgumentException("The given character is taken by someone else!");

            if (participantCharacter.OwnedCharacterIds.FirstOrDefault(e => e == characterId) == 0)
                throw new ArgumentException("This person does not own the given character!");


            participantCharacter.CharacterId = characterId;
            participantCharacter.ParticipantCharacterStatus = GameLobbyParticipantCharacterStatus.SELECTED;
        }

        public void ParticipantLockCharacter(int playerId)
        {

            var participantCharacter = ParticipantCharacters.FirstOrDefault(e => e.PlayerId == playerId);

            if (participantCharacter == null)
                throw new ArgumentException("This person does not exist in the lobby!");

            if (participantCharacter.ParticipantCharacterStatus != GameLobbyParticipantCharacterStatus.SELECTED || participantCharacter.CharacterId == 0)
                throw new ArgumentException("Person hasn't selected a character. Unable to lock in!");

            participantCharacter.ParticipantCharacterStatus = GameLobbyParticipantCharacterStatus.LOCKED;
        }

        private readonly Random r = new();
        public ParticipantCharacter[] SelectCharactersForNonlockedPlayers()
        {
            var nonlockedPlayers = ParticipantCharacters
                .Where(e => e.ParticipantCharacterStatus != GameLobbyParticipantCharacterStatus.LOCKED)
                .ToList();

            // As long as there 3 or more free characters in the system, this will not be an infinite loop!
            foreach(var player in nonlockedPlayers)
            {
                while(player.ParticipantCharacterStatus != GameLobbyParticipantCharacterStatus.LOCKED)
                {
                    var randomOwnedCharacterIndex = r.Next(0, player.OwnedCharacterIds.Length);

                    // If any other participant has LOCKED this character, skip
                    if (ParticipantCharacters.Any(e => e.CharacterId == player.OwnedCharacterIds[randomOwnedCharacterIndex] && 
                        e.ParticipantCharacterStatus == GameLobbyParticipantCharacterStatus.LOCKED))
                        continue;

                    player.CharacterId = player.OwnedCharacterIds[randomOwnedCharacterIndex];
                    player.ParticipantCharacterStatus = GameLobbyParticipantCharacterStatus.LOCKED;
                }
            }

            return nonlockedPlayers.ToArray();
        }

        public int GetRandomUnselectedCharacter()
        {
            var unselected = allCharacterIds.Except(ParticipantCharacters.Select(e => e.CharacterId)).ToArray();

            var randomIndex = r.Next(0, unselected.Length);

            return unselected[randomIndex];
        }

        public LobbyParticipantCharacterResponse GetParticipantCharactersResponse()
        {
            return new LobbyParticipantCharacterResponse()
            {
                ParticipantCharacters = this.ParticipantCharacters.ToArray(),
                InvitiationLink = GameCode,
            };
        }

        public ParticipantCharacter[] GetParticipantCharacters()
        {
            return this.ParticipantCharacters.ToArray();
        } 

        public int[] GetAllUnselectedCharacters()
        {
            return allCharacterIds.Where(e => ParticipantCharacters.Any(y => y.CharacterId != e)).ToArray();
        }

        public int[] GetAllSelectedCharacters()
        {
            return allCharacterIds.Where(e => ParticipantCharacters.Any(y => y.CharacterId == e)).ToArray();
        }
    }
}

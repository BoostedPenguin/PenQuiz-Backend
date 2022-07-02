using GameService.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace GameService.Services.GameLobbyServices
{
    public class GameLobbyTimer : Timer
    {
        public GameLobbyTimer(string gameCode, int[] allCharacterIds, int creatorPlayerId)
        {
            GameLobbyData.GameCode = gameCode;
            GameLobbyData = new GameLobbyData(gameCode, allCharacterIds);
            
            GameLobbyData.AddInitialParticipant(creatorPlayerId);
        }
        public GameLobbyData GameLobbyData { get; set; }
    }

    public class GameLobbyData
    {
        public GameLobbyData(string gameCode, int[] allCharacterIds)
        {
            GameCode = gameCode;
            this.allCharacterIds = allCharacterIds;
        }
        public enum GameLobbyParticipantCharacterStatus
        {
            SELECTED,
            UNSELECTED,
            LOCKED
        }
        public class ParticipantCharacter
        {
            public int PlayerId { get; set; }
            public int CharacterId { get; set; }
            public GameLobbyParticipantCharacterStatus ParticipantCharacterStatus { get; set; }
        }

        private readonly int[] allCharacterIds;
        public string GameCode { get; set; }


        private List<ParticipantCharacter> ParticipantCharacters { get; set; }


        public void AddInitialParticipant(int playerId)
        {
            if (ParticipantCharacters.FirstOrDefault(e => e.PlayerId == playerId) != null)
                throw new ArgumentException("This user is already in the list for this lobby");

            ParticipantCharacters.Add(new ParticipantCharacter()
            {
                PlayerId = playerId,
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

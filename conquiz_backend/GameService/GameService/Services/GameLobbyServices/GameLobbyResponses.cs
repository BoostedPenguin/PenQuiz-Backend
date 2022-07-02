using GameService.Data.Models;
using GameService.Dtos.SignalR_Responses;
using System;

namespace GameService.Services.GameLobbyServices
{
    public class RemovePlayerFromLobbyResponse
    {
        public GameInstance GameInstance { get; set; }
        public string RemovedPlayerId { get; set; }
    }

    public class OnJoinLobbyResponse
    {
        public OnJoinLobbyResponse(GameInstance gameInstance, CharacterResponse[] availableUserCharacters = null, GameCharacter gameCharacter = null)
        {
            GameInstance = gameInstance;
            AvailableUserCharacters = availableUserCharacters;
            GameCharacter = gameCharacter;
        }
        public GameInstance GameInstance { get; init; }
        public CharacterResponse[] AvailableUserCharacters { get; init; }

        [Obsolete("This functionality was stripped during Character development phase, we send all character data to QuestionClientResponse now.")]
        public GameCharacter GameCharacter { get; init; }
    }
}

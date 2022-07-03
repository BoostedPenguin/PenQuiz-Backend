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
        public OnJoinLobbyResponse(GameLobbyDataResponse gameLobbyDataResponse, CharacterResponse[] availableUserCharacters)
        {
            GameLobbyDataResponse = gameLobbyDataResponse;
            AvailableUserCharacters = availableUserCharacters;
        }

        public GameLobbyDataResponse GameLobbyDataResponse { get; init; }
        public CharacterResponse[] AvailableUserCharacters { get; init; }
    }
}

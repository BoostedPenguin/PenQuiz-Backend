using GameService.Data.Models;
using GameService.Dtos.SignalR_Responses;
using System;

namespace GameService.Services.GameLobbyServices
{
    public class OnRemoveFromlobbyResponse
    {
        public int RemovedPlayerId { get; set; }
        public string InvitationLink { get; set; }
    }

    public class OnJoinLobbyResponse
    {
        public OnJoinLobbyResponse(GameLobbyDataResponse gameLobbyDataResponse, CharacterResponse[] availableUserCharacters, LobbyParticipantCharacterResponse lobbyParticipantCharacterResponse)
        {
            GameLobbyDataResponse = gameLobbyDataResponse;
            AvailableUserCharacters = availableUserCharacters;
            LobbyParticipantCharacterResponse = lobbyParticipantCharacterResponse;
        }

        public GameLobbyDataResponse GameLobbyDataResponse { get; init; }
        public CharacterResponse[] AvailableUserCharacters { get; init; }
        public LobbyParticipantCharacterResponse LobbyParticipantCharacterResponse { get; init; }
    }
}

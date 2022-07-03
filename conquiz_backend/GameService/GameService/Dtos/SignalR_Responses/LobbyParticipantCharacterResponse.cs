using GameService.Services.GameLobbyServices;

namespace GameService.Dtos.SignalR_Responses
{
    public class GameDataWrapperResponse
    {
        public LobbyParticipantCharacterResponse LobbyParticipantCharacterResponse { get; set; }
        public GameLobbyDataResponse GameLobbyDataResponse { get; set; }
    }

    public class LobbyParticipantCharacterResponse
    {
        public ParticipantCharacter[] ParticipantCharacters { get; set; }
        public string InvitiationLink { get; set; }
    }
}

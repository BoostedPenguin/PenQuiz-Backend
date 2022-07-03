using GameService.Services.GameLobbyServices;

namespace GameService.Dtos.SignalR_Responses
{
    public class LobbyParticipantCharacterResponse
    {
        public ParticipantCharacter[] ParticipantCharacters { get; set; }
        public string InvitiationCode { get; set; }
    }
}

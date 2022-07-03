using GameService.Data.Models;

namespace GameService.Dtos.SignalR_Responses
{
    public class GameLobbyDataResponse
    {
        public string InvitationLink { get; set; }
        public GameType GameType { get; set; }
        public ParticipantsResponse[] Participants { get; set; }
        public int GameCreatorId { get; set; }
    }
}

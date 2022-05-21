using GameService.Data.Models;

namespace GameService.Dtos
{

    public class RemovePlayerFromLobbyResponse
    {
        public GameInstance GameInstance { get; set; }
        public string RemovedPlayerId { get; set; }
    }
}

using GameService.Data.Models;

namespace GameService.Services.GameLobbyServices
{
    public class RemovePlayerFromLobbyResponse
    {
        public GameInstance GameInstance { get; set; }
        public string RemovedPlayerId { get; set; }
    }

    public class OnJoinLobbyResponse
    {
        public OnJoinLobbyResponse(GameInstance gameInstance, GameCharacter gameCharacter = null)
        {
            GameInstance = gameInstance;
            GameCharacter = gameCharacter;
        }
        public GameInstance GameInstance { get; set; }
        public GameCharacter GameCharacter { get; set; }
    }
}

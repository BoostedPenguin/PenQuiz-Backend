using GameService.Dtos.SignalR_Responses;
using System.Collections.Generic;

namespace GameService.Dtos
{
    /// <summary>
    /// Stores the current game lobby data, such as participant amount, their data and their selected characters
    /// Use this instead of sending all of the game instance when a lobby event happens
    /// </summary>
    public class GameLobbyData
    {
        public List<ParticipantsResponse> Participants { get; set; }
    }
}

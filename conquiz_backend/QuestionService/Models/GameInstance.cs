using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.Models
{
    public enum GameState
    {
        IN_LOBBY,
        IN_PROGRESS,
        FINISHED,
        CANCELED
    }

    public partial class GameInstance
    {
        public int Id { get; set; }
        public GameState GameState { get; set; }
        public string OpentDbSessionToken { get; set; }
        public int ExternalId { get; set; }
    }
}

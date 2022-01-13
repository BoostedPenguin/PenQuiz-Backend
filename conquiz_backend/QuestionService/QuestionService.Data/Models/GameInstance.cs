using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.Data.Models
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
        public GameInstance()
        {
            GameSessionQuestions = new HashSet<GameSessionQuestions>();
        }

        public int Id { get; set; }
        public string OpentDbSessionToken { get; set; }
        public string ExternalId { get; set; }

        public virtual ICollection<GameSessionQuestions> GameSessionQuestions { get; set; }
    }
}

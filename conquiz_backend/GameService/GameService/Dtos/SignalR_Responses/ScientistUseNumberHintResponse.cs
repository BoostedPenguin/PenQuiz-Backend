using System.Text.Json.Serialization;

namespace GameService.Dtos.SignalR_Responses
{
    public class ScientistUseNumberHintResponse
    {
        public string MinRange { get; set; }
        public string MaxRange { get; set; }

        public QuestionClientResponse QuestionResponse { get; set; }

        [JsonIgnore]
        public string GameLink { get; set; }
        public int PlayerId { get; set; }
    }
}

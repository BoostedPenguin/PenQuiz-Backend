namespace GameService.Dtos.SignalR_Responses
{
    public class VikingUseFortifyResponse
    {
        public QuestionClientResponse QuestionResponse { get; set; }
        public int UsedInRoundId { get; set; }
        public string GameLink { get; set; }
    }
}

namespace GameService.Dtos.SignalR_Responses
{
    public class RoundingAttackerRes
    {
        public RoundingAttackerRes(int userId, string[] availableAttackTerritoriesNames)
        {
            AttackerId = userId;
            AvailableAttackTerritories = availableAttackTerritoriesNames;
        }
        public int AttackerId { get; set; } 
        public string[] AvailableAttackTerritories { get; set; }
    }
}

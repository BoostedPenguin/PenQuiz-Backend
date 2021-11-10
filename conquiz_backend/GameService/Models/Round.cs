using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Models
{
    public enum RoundStage
    {
        NOT_STARTED,
        CURRENT,
        FINISHED,
    }

    /// <summary>
    /// Shows what this round represents as an attack stage
    /// </summary>
    public enum AttackStage
    {
        MULTIPLE_NEUTRAL,
        NUMBER_NEUTRAL,
        MULTIPLE_PVP,
        NUMBER_PVP
    }

    public class Round
    {
        public int Id { get; set; }
        public RoundStage RoundStage { get; set; }
        public AttackStage AttackStage { get; set; }
        public int GameInstanceId { get; set; }
        public int GameRoundNumber { get; set; }
        public bool IsTerritoryVotingOpen { get; set; }
        public bool IsQuestionVotingOpen { get; set; }
        public string Description { get; set; }


        public virtual Questions Question { get; set; }
        public virtual GameInstance GameInstance { get; set; }
        public virtual NeutralRound NeutralRound { get; set; }
        public virtual PvpRound PvpRound { get; set; }
    }

    public class PvpRound
    {
        public PvpRound()
        {
            PvpRoundAnswers = new HashSet<PvpRoundAnswers>();
        }

        public int Id { get; set; }
        public int AttackerId { get; set; }
        public int? DefenderId { get; set; }
        public int? WinnerId { get; set; }
        public int AttackedTerritoryId { get; set; }
        public int NumberQuestionId { get; set; }
        public int RoundId { get; set; }

        public virtual Questions NumberQuestion { get; set; }
        public virtual ObjectTerritory AttackedTerritory { get; set; }
        public virtual Round Round { get; set; }
        public virtual ICollection<PvpRoundAnswers> PvpRoundAnswers { get; set; }
    }

    public class PvpRoundAnswers
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public int? MChoiceQAnswerId { get; set; }
        public int? NumberQAnswer { get; set; }
        public int PvpRoundId { get; set; }

        public virtual PvpRound PvpRound { get; set; }
    }

    public class NeutralRound
    {
        public NeutralRound()
        {
            TerritoryAttackers = new HashSet<AttackingNeutralTerritory>();
        }
        public int Id { get; set; }
        public int RoundId { get; set; }
        public int AttackOrderNumber { get; set; }

        public virtual Round Round { get; set; }
        public virtual ICollection<AttackingNeutralTerritory> TerritoryAttackers { get; set; }
    }

    public class AttackingNeutralTerritory
    {
        public int Id { get; set; }
        public int AttackOrderNumber { get; set; }
        public int? AttackedTerritoryId { get; set; }
        public bool? AttackerWon { get; set; }
        public int AttackerId { get; set; }
        public int NeutralRoundId { get; set; }
        public int? AttackerMChoiceQAnswerId { get; set; }
        public int? AttackerNumberQAnswer { get; set; }

        public virtual ObjectTerritory AttackedTerritory { get; set; }
        public virtual NeutralRound NeutralRound { get; set; }
    }
}

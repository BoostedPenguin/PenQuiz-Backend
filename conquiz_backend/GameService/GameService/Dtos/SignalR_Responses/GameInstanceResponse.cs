using GameService.Data.Models;
using System;
using System.Collections.Generic;

namespace GameService.Dtos.SignalR_Responses
{

    public class OnPlayerLoginResponse
    {
        public OnPlayerLoginResponse(GameInstanceResponse gameInstanceResponse, int userId, 
            RoundingAttackerRes roundingAttackerRes = null, 
            QuestionClientResponse questionClientResponse = null, 
            MCPlayerQuestionAnswers mCPlayerQuestionAnswers = null)
        {
            UserId = userId;
            RoundingAttackerRes = roundingAttackerRes;
            this.QuestionClientResponse = questionClientResponse;
            MCPlayerQuestionAnswers = mCPlayerQuestionAnswers;
            GameInstanceResponse = gameInstanceResponse;
        }

        public GameInstanceResponse GameInstanceResponse { get; set; }
        public int UserId { get; set; }
        public RoundingAttackerRes RoundingAttackerRes { get; set; }
        public QuestionClientResponse QuestionClientResponse { get; set; }
        public MCPlayerQuestionAnswers MCPlayerQuestionAnswers { get; }
    }
    public class GameInstanceResponse
    {
        public int Id { get; set; }
        public string GameGlobalIdentifier { get; set; }
        public GameType GameType { get; set; }
        public int Mapid { get; set; }
        public int ParticipantsId { get; set; }
        public int GameCreatorId { get; set; }
        public GameState GameState { get; set; }
        public string InvitationLink { get; set; }
        // It links to the currently playing round
        public int GameRoundNumber { get; set; }

        public virtual ICollection<ObjectTerritoryResponse> ObjectTerritory { get; set; }
        public virtual ICollection<ParticipantsResponse> Participants { get; set; }
        public virtual ICollection<RoundResponse> Rounds { get; set; }
    }


    #region Round

    public class RoundResponse
    {
        public int Id { get; set; }
        public AttackStage AttackStage { get; set; }
        public int GameInstanceId { get; set; }
        public int GameRoundNumber { get; set; }


        public virtual NeutralRoundResponse NeutralRound { get; set; }
        public virtual PvpRoundResponse PvpRound { get; set; }
    }


    public partial class AnswersResponse
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string Answer { get; set; }
        public bool Correct { get; set; }
    }

    public class PvpRoundResponse
    {
        public int Id { get; set; }
        public int AttackerId { get; set; }
        public int? DefenderId { get; set; }
        public int? WinnerId { get; set; }
        public int? AttackedTerritoryId { get; set; }
        public int RoundId { get; set; }

        // This should be true only after the main question has been resolved for isCapital attackedterritories

        public virtual ObjectTerritoryResponse AttackedTerritory { get; set; }

        public virtual ICollection<CapitalRoundResponse> CapitalRounds { get; set; }
    }

    public class CapitalRoundResponse
    {
        public int Id { get; set; }
        public CapitalRoundAttackStage CapitalRoundAttackStage { get; set; } = CapitalRoundAttackStage.MULTIPLE_CHOICE_QUESTION;
        public bool IsCompleted { get; set; } = false;
        public int PvpRoundId { get; set; }
        public bool IsQuestionVotingOpen { get; set; }
        public DateTime? QuestionOpenedAt { get; set; }
    }




    public class NeutralRoundResponse
    {
        public int Id { get; set; }
        public int RoundId { get; set; }
        public int AttackOrderNumber { get; set; }

        public virtual ICollection<AttackingNeutralTerritoryResponse> TerritoryAttackers { get; set; }
    }

    public class AttackingNeutralTerritoryResponse
    {
        public int Id { get; set; }
        public int AttackOrderNumber { get; set; }
        public int? AttackedTerritoryId { get; set; }
        public DateTime? AnsweredAt { get; set; }
        public bool? AttackerWon { get; set; }
        public int AttackerId { get; set; }
        public int NeutralRoundId { get; set; }
        public int? AttackerMChoiceQAnswerId { get; set; }
        public long? AttackerNumberQAnswer { get; set; }

        public virtual ObjectTerritoryResponse AttackedTerritory { get; set; }
    }

    #endregion


    public class ParticipantsResponse
    {
        public int Id { get; set; }
        public string AvatarName { get; set; }
        public int PlayerId { get; set; }
        public int GameId { get; set; }
        public bool IsAfk { get; set; }
        public int Score { get; set; }
        public int FinalQuestionScore { get; set; }

        public virtual UsersResponse Player { get; set; }
    }


    public class UsersResponse
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string UserGlobalIdentifier { get; set; }
        public bool IsBot { get; set; }

    }


    public class ObjectTerritoryResponse
    {
        public int Id { get; set; }
        public int MapTerritoryId { get; set; }
        public int GameInstanceId { get; set; }
        public bool IsCapital { get; set; }
        public int TerritoryScore { get; set; }
        public int? TakenBy { get; set; }
        public int? AttackedBy { get; set; }

        public virtual MapTerritoryResponse MapTerritory { get; set; }
    }


    public partial class MapTerritoryResponse
    {

        public int Id { get; set; }
        public string TerritoryName { get; set; }
    }
}

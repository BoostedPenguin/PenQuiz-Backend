using GameService.Context;
using GameService.Data;
using GameService.Data.Models;
using GameService.Dtos.SignalR_Responses;
using GameService.Services.Extensions;
using GameService.Services.GameTimerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services
{
    public interface IGameControlService
    {
        void AnswerQuestion(string answerIdString);
        SelectedTerritoryResponse SelectTerritory(string mapTerritoryName);
    }

    /// <summary>
    /// Handles the game flow and controls the timer callbacks
    /// </summary>
    public class GameControlService : IGameControlService
    {
        private readonly IGameTimerService gameTimerService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IGameTerritoryService gameTerritoryService;
        private readonly string DefaultMap = "Antarctica";
        public GameControlService(IGameTimerService gameTimerService, IHttpContextAccessor httpContextAccessor, IGameTerritoryService gameTerritoryService)
        {
            this.gameTimerService = gameTimerService;
            this.httpContextAccessor = httpContextAccessor;
            this.gameTerritoryService = gameTerritoryService;
        }

        public SelectedTerritoryResponse SelectTerritory(string mapTerritoryName)
        {
            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();

            var playerGameTimer = gameTimerService.GameTimers.FirstOrDefault(e =>
                e.Data.GameInstance.Participants.FirstOrDefault(e => e.Player.UserGlobalIdentifier == globalUserId) is not null);

            if(playerGameTimer == null)
            {
                throw new GameException("There is no open game where this player participates");
            }

            var gm = playerGameTimer.Data.GameInstance;

            var userId = gm.Participants.First(e => e.Player.UserGlobalIdentifier == globalUserId).PlayerId;

            var currentRoundOverview = gm.Rounds
                .Where(x =>
                    x.GameRoundNumber == x.GameInstance.GameRoundNumber)
                .Select(x => new
                {
                    RoundId = x.Id,
                    x.AttackStage,
                    x.IsTerritoryVotingOpen,
                    x.GameInstanceId,
                    x.GameInstance.InvitationLink
                })
                .FirstOrDefault();

            if (currentRoundOverview == null)
                throw new GameException("The current round isn't valid");

            if (!currentRoundOverview.IsTerritoryVotingOpen)
                throw new GameException("The round's territory voting stage isn't open");

            // Selecting territory for multiple choice neutral rounds
            if(currentRoundOverview.AttackStage == AttackStage.MULTIPLE_NEUTRAL)
            {
                var neutralRound = gm.Rounds
                    .Where(x => x.Id == currentRoundOverview.RoundId).FirstOrDefault();


                // Check if it's this player's turn for selecting a neutral territory or not

                var currentTurnsPlayer = neutralRound
                    .NeutralRound
                    .TerritoryAttackers
                    .FirstOrDefault(x => x.AttackOrderNumber == neutralRound.NeutralRound.AttackOrderNumber && x.AttackerId == userId);

                if (currentTurnsPlayer == null)
                    throw new GameException("Unknown player turn.");

                if (currentTurnsPlayer.AttackedTerritoryId != null)
                    throw new BorderSelectedGameException("You already selected a territory for this round");


                var mapTerritory = gm.Map.MapTerritory.Where(x => x.TerritoryName == mapTerritoryName).FirstOrDefault();

                if (mapTerritory == null)
                    throw new GameException($"A territory with name `{mapTerritoryName}` for map `{DefaultMap}` doesn't exist");

                var gameObjTerritory = gameTerritoryService
                    .SelectTerritoryAvailability(gm, userId, currentRoundOverview.GameInstanceId, mapTerritory.Id, true);

                if (gameObjTerritory == null)
                    throw new BorderSelectedGameException("The selected territory doesn't border any of your borders or is attacked by someone else");

                if (gameObjTerritory.TakenBy != null)
                    throw new BorderSelectedGameException("The selected territory is already taken by somebody else");

                // Set this territory as being attacked from this person
                currentTurnsPlayer.AttackedTerritoryId = gameObjTerritory.Id;

                // Set the ObjectTerritory as being attacked currently
                var obj = gm.ObjectTerritory.First(e => e.Id == gameObjTerritory.Id);
                obj.AttackedBy = currentTurnsPlayer.AttackerId;
                
                
                // Store it in state, and update when the closing event triggers
                // Makes sure entity does not track same entity twice

                //db.Update(obj);
                //await db.SaveChangesAsync();

                return new SelectedTerritoryResponse()
                {
                    GameLink = currentRoundOverview.InvitationLink,
                    AttackedById = userId,
                    TerritoryId = gameObjTerritory.Id
                };
            }

            // Selecting territory for multiple choice pvp rounds
            else if (currentRoundOverview.AttackStage == AttackStage.MULTIPLE_PVP)
            {
                var pvpRound = gm.Rounds.Where(x => x.Id == currentRoundOverview.RoundId).FirstOrDefault();

                // Person who selected a territory is the attacker
                if(pvpRound.PvpRound.AttackerId != userId)
                    throw new GameException("Not this players turn");

                if(pvpRound.PvpRound.AttackedTerritoryId != null)
                    throw new BorderSelectedGameException("You already selected a territory for this round");

                var mapTerritory = gm.Map.MapTerritory.Where(x => x.TerritoryName == mapTerritoryName).FirstOrDefault();

                if (mapTerritory == null)
                    throw new GameException($"A territory with name `{mapTerritoryName}` for map `{DefaultMap}` doesn't exist");

                var gameObjTerritory = gameTerritoryService
                    .SelectTerritoryAvailability(gm, userId, currentRoundOverview.GameInstanceId, mapTerritory.Id, false);

                if (gameObjTerritory == null)
                    throw new BorderSelectedGameException("The selected territory doesn't border any of your borders or is attacked by someone else");

                // Set this territory as being attacked from this person
                pvpRound.PvpRound.AttackedTerritoryId = gameObjTerritory.Id;
                pvpRound.PvpRound.DefenderId = gameObjTerritory.TakenBy;

                // Set the ObjectTerritory as being attacked currently
                var obj = gm.ObjectTerritory.First(e => e.Id == gameObjTerritory.Id);
                obj.AttackedBy = pvpRound.PvpRound.AttackerId;

                return new SelectedTerritoryResponse()
                {
                    GameLink = currentRoundOverview.InvitationLink,
                    AttackedById = userId,
                    TerritoryId = gameObjTerritory.Id
                };
            }
            else
            {
                throw new GameException("Current round isn't either multiple neutral nor multiple pvp");
            }
        }

        private static void CapitalStageAnswer(string answerIdString, ref Round currentRound, DateTime answeredAt, int userId)
        {
            var capitalRound =
                currentRound
                .PvpRound
                .CapitalRounds
                .FirstOrDefault(x => !x.IsCompleted && x.IsQuestionVotingOpen);

            if (capitalRound == null)
                throw new AnswerSubmittedGameException("This capital round is null. Fatal error");

            if (!capitalRound.IsQuestionVotingOpen)
                throw new AnswerSubmittedGameException("The voting stage for this question is either over or not started.");

            switch (capitalRound.CapitalRoundAttackStage)
            {
                case CapitalRoundAttackStage.MULTIPLE_CHOICE_QUESTION:
                    
                    bool success = int.TryParse(answerIdString, out int answerIdMPvp);
                    if (!success)
                        throw new AnswerSubmittedGameException("You didn't provide a valid number");

                    // Requesting user is the attacker
                    if (!capitalRound.CapitalRoundMultipleQuestion.Answers.Any(x => x.Id == answerIdMPvp))
                        throw new AnswerSubmittedGameException("The provided answerID isn't valid for this question.");

                    if (userId != currentRound.PvpRound.AttackerId && userId != currentRound.PvpRound.DefenderId)
                        throw new AnswerSubmittedGameException("You can't vote for this question");

                    var userAttacking = capitalRound.CapitalRoundUserAnswers
                        .FirstOrDefault(x => x.UserId == userId);

                    if (userAttacking != null && userAttacking.MChoiceQAnswerId != null)
                        throw new ArgumentException("This user already voted for this question");

                    var result = new CapitalRoundAnswers()
                    {
                        MChoiceQAnswerId = answerIdMPvp,
                        UserId = userId
                    };

                    capitalRound.CapitalRoundUserAnswers.Add(result);
                    break;

                case CapitalRoundAttackStage.NUMBER_QUESTION:

                    bool successNPvp = long.TryParse(answerIdString, out long answerIdNPvp);
                    if (!successNPvp)
                        throw new AnswerSubmittedGameException("You didn't provide a valid number");

                    var pvpAttacker = capitalRound.CapitalRoundUserAnswers
                        .FirstOrDefault(x => x.UserId == userId);

                    if (pvpAttacker == null)
                        throw new AnswerSubmittedGameException("User doesn't have an existing multiple choice answer. Fatal error.");

                    if (pvpAttacker.NumberQAnswer != null)
                        throw new AnswerSubmittedGameException("You already voted for this question");

                    pvpAttacker.NumberQAnsweredAt = answeredAt;
                    pvpAttacker.NumberQAnswer = answerIdNPvp;

                    break;
            }
        }

        public void AnswerFinalQuestion(string answerIdString, ref Round currentRound, int userId)
        {

            bool successNNeutral = long.TryParse(answerIdString, out long answerIdNNeutral);
            if (!successNNeutral)
                throw new AnswerSubmittedGameException("You didn't provide a valid number");

            var pAttacker = currentRound
                .NeutralRound
                .TerritoryAttackers
                .First(x => x.AttackerId == userId);

            if (pAttacker.AttackerNumberQAnswer != null)
                throw new AnswerSubmittedGameException("You already voted for this question");

            pAttacker.AnsweredAt = DateTime.Now;
            pAttacker.AttackerNumberQAnswer = answerIdNNeutral;

        }

        public void AnswerQuestion(string answerIdString)
        {
            var answeredAt = DateTime.Now;

            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();

            var playerGameTimer = gameTimerService.GameTimers.FirstOrDefault(e =>
                e.Data.GameInstance.Participants.FirstOrDefault(e => e.Player.UserGlobalIdentifier == globalUserId) is not null);

            if (playerGameTimer == null)
                throw new GameException("There is no open game where this player participates");

            var gm = playerGameTimer.Data.GameInstance;

            var currentRound = gm.Rounds
                .Where(x =>
                    x.GameRoundNumber == x.GameInstance.GameRoundNumber)
                .FirstOrDefault();

            var userId = gm.Participants.First(e => e.Player.UserGlobalIdentifier == globalUserId).PlayerId;



            if (currentRound == null)
                throw new AnswerSubmittedGameException("User isn't participating in any in progress games.");

            // Capital stage
            // Skip every check here and check externally
            if(currentRound.PvpRound?.IsCurrentlyCapitalStage == true)
            {

                // In-game-instance-uncertain
                CapitalStageAnswer(answerIdString, ref currentRound, answeredAt, userId);
                return;
            }

            if (!currentRound.IsQuestionVotingOpen)
                throw new AnswerSubmittedGameException("The voting stage for this question is either over or not started.");

            switch (currentRound.AttackStage)
            {
                case AttackStage.MULTIPLE_NEUTRAL:

                    bool successMNeutral = int.TryParse(answerIdString, out int answerIdMNeutral);
                    if (!successMNeutral)
                        throw new AnswerSubmittedGameException("You didn't provide a valid number");

                    if (!currentRound.Question.Answers.Any(x => x.Id == answerIdMNeutral))
                        throw new AnswerSubmittedGameException("The provided answerID isn't valid for this question.");

                    var playerAttacking = currentRound
                        .NeutralRound
                        .TerritoryAttackers
                        .First(x => x.AttackerId == userId);

                    if (playerAttacking.AttackerMChoiceQAnswerId != null)
                        throw new AnswerSubmittedGameException("You already voted for this question");

                    playerAttacking.AttackerMChoiceQAnswerId = answerIdMNeutral;
                    break;

                case AttackStage.NUMBER_NEUTRAL:

                    bool successNNeutral = long.TryParse(answerIdString, out long answerIdNNeutral);
                    if (!successNNeutral)
                        throw new AnswerSubmittedGameException("You didn't provide a valid number");

                    var pAttacker = currentRound
                        .NeutralRound
                        .TerritoryAttackers
                        .First(x => x.AttackerId == userId);

                    if(pAttacker.AttackerNumberQAnswer != null)
                        throw new AnswerSubmittedGameException("You already voted for this question");

                    pAttacker.AnsweredAt = DateTime.Now;
                    pAttacker.AttackerNumberQAnswer = answerIdNNeutral;
                    break;

                case AttackStage.MULTIPLE_PVP:

                    bool success = int.TryParse(answerIdString, out int answerIdMPvp);
                    if (!success)
                        throw new AnswerSubmittedGameException("You didn't provide a valid number");

                    // Requesting user is the attacker
                    if (!currentRound.Question.Answers.Any(x => x.Id == answerIdMPvp))
                        throw new AnswerSubmittedGameException("The provided answerID isn't valid for this question.");
                    
                    if (userId != currentRound.PvpRound.AttackerId && userId != currentRound.PvpRound.DefenderId)
                        throw new AnswerSubmittedGameException("You can't vote for this question");
                    
                    var userAttacking = currentRound
                        .PvpRound
                        .PvpRoundAnswers
                        .FirstOrDefault(x => x.UserId == userId);

                    if (userAttacking != null && userAttacking.MChoiceQAnswerId != null)
                        throw new ArgumentException("This user already voted for this question");

                    var result = new PvpRoundAnswers()
                    {
                        MChoiceQAnswerId = answerIdMPvp,
                        UserId = userId
                    };
                    currentRound.PvpRound.PvpRoundAnswers.Add(result);
                    break;

                case AttackStage.NUMBER_PVP:
                    bool successNPvp = long.TryParse(answerIdString, out long answerIdNPvp);
                    if (!successNPvp)
                        throw new AnswerSubmittedGameException("You didn't provide a valid number");

                    var pvpAttacker = currentRound
                        .PvpRound
                        .PvpRoundAnswers
                        .First(x => x.UserId == userId);

                    if (pvpAttacker.NumberQAnswer != null)
                        throw new AnswerSubmittedGameException("You already voted for this question");

                    pvpAttacker.NumberQAnsweredAt = DateTime.Now;
                    pvpAttacker.NumberQAnswer = answerIdNPvp;
                    break;
                case AttackStage.FINAL_NUMBER_PVP:
                    AnswerFinalQuestion(answerIdString, ref currentRound, userId);
                    break;
            }
        }
    }
}

using GameService.Context;
using GameService.Models;
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
        Task AnswerQuestion(string answerIdString);
        Task<GameInstance> SelectTerritory(string mapTerritoryName);
    }

    /// <summary>
    /// Handles the game flow and controls the timer callbacks
    /// </summary>
    public class GameControlService : IGameControlService
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IGameTerritoryService gameTerritoryService;
        private readonly string DefaultMap = "Antarctica";
        public GameControlService(IHttpContextAccessor httpContextAccessor, IDbContextFactory<DefaultContext> contextFactory, IGameTerritoryService gameTerritoryService)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.contextFactory = contextFactory;
            this.gameTerritoryService = gameTerritoryService;
        }

        public async Task<GameInstance> SelectTerritory(string mapTerritoryName)
        {
            using var db = contextFactory.CreateDbContext();
            var userId = httpContextAccessor.GetCurrentUserId();

            var currentRoundOverview = await db.Round
                .Include(x => x.GameInstance)
                .ThenInclude(x => x.Participants)
                .Where(x => 
                    x.GameInstance.GameState == GameState.IN_PROGRESS && 
                    x.GameRoundNumber == x.GameInstance.GameRoundNumber &&
                    x.GameInstance.Participants
                        .Any(y => y.PlayerId == userId))
                .Select(x => new
                {
                    RoundId = x.Id,
                    x.AttackStage,
                    x.IsTerritoryVotingOpen,
                    x.GameInstanceId
                })
                .FirstOrDefaultAsync();

            if (currentRoundOverview == null)
                throw new GameException("The current round isn't valid");

            if (!currentRoundOverview.IsTerritoryVotingOpen)
                throw new GameException("The round's territory voting stage isn't open");

            // Selecting territory for multiple choice neutral rounds
            if(currentRoundOverview.AttackStage == AttackStage.MULTIPLE_NEUTRAL)
            {
                var neutralRound = await db.Round
                    .Include(x => x.NeutralRound)
                    .ThenInclude(x => x.TerritoryAttackers)
                    .ThenInclude(x => x.AttackedTerritory)
                    .Where(x => x.Id == currentRoundOverview.RoundId)
                    .FirstOrDefaultAsync();

                // Check if it's this player's turn for selecting a neutral territory or not

                var currentTurnsPlayer = neutralRound
                    .NeutralRound
                    .TerritoryAttackers
                    .FirstOrDefault(x => x.AttackOrderNumber == neutralRound.NeutralRound.AttackOrderNumber && x.AttackerId == userId);

                if (currentTurnsPlayer == null)
                    throw new GameException("Unknown player turn.");

                if (currentTurnsPlayer.AttackedTerritoryId != null)
                    throw new BorderSelectedGameException("You already selected a territory for this round");

                var mapTerritory = await db.MapTerritory
                    .Include(x => x.Map)
                    .Where(x => x.TerritoryName == mapTerritoryName && x.Map.Name == DefaultMap)
                    .FirstOrDefaultAsync();

                if (mapTerritory == null)
                    throw new GameException($"A territory with name `{mapTerritoryName}` for map `{DefaultMap}` doesn't exist");

                var gameObjTerritory = await gameTerritoryService
                    .SelectTerritoryAvailability(db, userId, currentRoundOverview.GameInstanceId, mapTerritory.Id, true);

                if (gameObjTerritory == null)
                    throw new BorderSelectedGameException("The selected territory doesn't border any of your borders or is attacked by someone else");

                if (gameObjTerritory.TakenBy != null)
                    throw new BorderSelectedGameException("The selected territory is already taken by somebody else");

                // Set this territory as being attacked from this person
                currentTurnsPlayer.AttackedTerritoryId = gameObjTerritory.Id;

                // Set the ObjectTerritory as being attacked currently
                gameObjTerritory.AttackedBy = currentTurnsPlayer.AttackerId;
                db.Update(gameObjTerritory);
                db.Update(currentTurnsPlayer);

                await db.SaveChangesAsync();

                return await CommonTimerFunc.GetFullGameInstance(currentRoundOverview.GameInstanceId, db);

            }

            // Selecting territory for multiple choice pvp rounds
            else if (currentRoundOverview.AttackStage == AttackStage.MULTIPLE_PVP)
            {
                var pvpRound = await db.Round
                    .Include(x => x.PvpRound)
                    .ThenInclude(x => x.PvpRoundAnswers)
                    .Include(x => x.PvpRound)
                    .ThenInclude(x => x.AttackedTerritory)
                    .Where(x => x.Id == currentRoundOverview.RoundId)
                    .FirstOrDefaultAsync();

                // Person who selected a territory is the attacker
                if(pvpRound.PvpRound.AttackerId != userId)
                    throw new GameException("Not this players turn");

                if(pvpRound.PvpRound.AttackedTerritoryId != null)
                    throw new BorderSelectedGameException("You already selected a territory for this round");

                var mapTerritory = await db.MapTerritory
                    .Include(x => x.Map)
                    .Where(x => x.TerritoryName == mapTerritoryName && x.Map.Name == DefaultMap)
                    .FirstOrDefaultAsync();

                if (mapTerritory == null)
                    throw new GameException($"A territory with name `{mapTerritoryName}` for map `{DefaultMap}` doesn't exist");

                var gameObjTerritory = await gameTerritoryService
                    .SelectTerritoryAvailability(db, userId, currentRoundOverview.GameInstanceId, mapTerritory.Id, false);

                if (gameObjTerritory == null)
                    throw new BorderSelectedGameException("The selected territory doesn't border any of your borders or is attacked by someone else");

                // Set this territory as being attacked from this person
                pvpRound.PvpRound.AttackedTerritoryId = gameObjTerritory.Id;
                pvpRound.PvpRound.DefenderId = gameObjTerritory.TakenBy;

                // Set the ObjectTerritory as being attacked currently
                gameObjTerritory.AttackedBy = pvpRound.PvpRound.AttackerId;
                db.Update(gameObjTerritory);
                db.Update(pvpRound);

                await db.SaveChangesAsync();

                return await CommonTimerFunc.GetFullGameInstance(currentRoundOverview.GameInstanceId, db);
            }
            else
            {
                throw new GameException("Current round isn't either multiple neutral nor multiple pvp");
            }
        }

        public async Task AnswerQuestion(string answerIdString)
        {
            var answeredAt = DateTime.Now;

            using var db = contextFactory.CreateDbContext();

            var userId = httpContextAccessor.GetCurrentUserId();

            // Not sure about performanec wise, also what happens if you include a null of null
            var gm = await db.GameInstance
                .Include(x => x.Participants)
                .Include(x => x.Rounds)
                .ThenInclude(x => x.PvpRound)
                .ThenInclude(x => x.PvpRoundAnswers)
                .Include(x => x.Rounds)
                .ThenInclude(x => x.NeutralRound)
                .ThenInclude(x => x.TerritoryAttackers)
                .Include(x => x.Rounds)
                .ThenInclude(x => x.Question)
                .ThenInclude(x => x.Answers)
                .Where(x => x.GameState == GameState.IN_PROGRESS && x.Participants
                    .Any(y => y.PlayerId == userId))
                .Select(x => new
                {
                    CurrentRound = x.Rounds.First(y => y.GameRoundNumber == x.GameRoundNumber),
                })
                .FirstOrDefaultAsync();

            if (gm == null || gm.CurrentRound == null)
                throw new AnswerSubmittedGameException("User isn't participating in any in progress games.");

            if (!gm.CurrentRound.IsQuestionVotingOpen)
                throw new AnswerSubmittedGameException("The voting stage for this question is either over or not started.");

            switch (gm.CurrentRound.AttackStage)
            {
                case AttackStage.MULTIPLE_NEUTRAL:

                    bool successMNeutral = int.TryParse(answerIdString, out int answerIdMNeutral);
                    if (!successMNeutral)
                        throw new AnswerSubmittedGameException("You didn't provide a valid number");

                    if (!gm.CurrentRound.Question.Answers.Any(x => x.Id == answerIdMNeutral))
                        throw new AnswerSubmittedGameException("The provided answerID isn't valid for this question.");

                    var playerAttacking = gm.CurrentRound
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

                    var pAttacker = gm.CurrentRound
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
                    if (!gm.CurrentRound.Question.Answers.Any(x => x.Id == answerIdMPvp))
                        throw new AnswerSubmittedGameException("The provided answerID isn't valid for this question.");
                    
                    if (userId != gm.CurrentRound.PvpRound.AttackerId && userId != gm.CurrentRound.PvpRound.DefenderId)
                        throw new AnswerSubmittedGameException("You can't vote for this question");
                    
                    var userAttacking = gm.CurrentRound
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
                    gm.CurrentRound.PvpRound.PvpRoundAnswers.Add(result);
                    break;
            }

            db.Update(gm.CurrentRound);

            await db.SaveChangesAsync();
        }
    }
}

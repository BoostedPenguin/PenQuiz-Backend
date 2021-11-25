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
        Task AnswerMCQuestion(int answerId);
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

            switch (currentRoundOverview.AttackStage)
            {
                case AttackStage.MULTIPLE_NEUTRAL:
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

                    if(gameObjTerritory.TakenBy != null)
                        throw new BorderSelectedGameException("The selected territory is already taken by somebody else");

                    // Set this territory as being attacked from this person
                    currentTurnsPlayer.AttackedTerritoryId = gameObjTerritory.Id;

                    // Set the ObjectTerritory as being attacked currently
                    gameObjTerritory.AttackedBy = currentTurnsPlayer.AttackerId;
                    db.Update(gameObjTerritory);
                    db.Update(currentTurnsPlayer);

                    await db.SaveChangesAsync();

                    return await CommonTimerFunc.GetFullGameInstance(currentRoundOverview.GameInstanceId, db);

                case AttackStage.NUMBER_NEUTRAL:
                    throw new NotImplementedException();
                case AttackStage.MULTIPLE_PVP:
                    throw new NotImplementedException();
                case AttackStage.NUMBER_PVP:
                    throw new NotImplementedException();
            }
            throw new Exception("Unknown error occured");
        }

        public async Task AnswerMCQuestion(int answerId)
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

            if (!gm.CurrentRound.Question.Answers.Any(x => x.Id == answerId))
                throw new AnswerSubmittedGameException("The provided answerID isn't valid for this question.");

            switch (gm.CurrentRound.AttackStage)
            {
                case AttackStage.MULTIPLE_NEUTRAL:
                    var playerAttacking = gm.CurrentRound
                        .NeutralRound
                        .TerritoryAttackers
                        .First(x => x.AttackerId == userId);

                    if (playerAttacking.AttackerMChoiceQAnswerId != null)
                        throw new AnswerSubmittedGameException("You already voted for this question");

                    playerAttacking.AttackerMChoiceQAnswerId = answerId;
                    break;

                case AttackStage.MULTIPLE_PVP:
                    // Requesting user is the attacker
                    var userAttacking = gm.CurrentRound
                        .PvpRound
                        .PvpRoundAnswers
                        .First(x => x.UserId == userId);

                    if (userAttacking.MChoiceQAnswerId != null)
                        throw new ArgumentException("This user already voted for this question");

                    userAttacking.MChoiceQAnswerId = answerId;
                    break;
            }

            db.Update(gm.CurrentRound);

            await db.SaveChangesAsync();
        }

        public async Task AnswerNumberQuestion()
        {

        }
    }
}

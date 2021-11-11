using GameService.Context;
using GameService.Models;
using GameService.Services.Extensions;
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
    }

    /// <summary>
    /// Handles the game flow and controls the timer callbacks
    /// </summary>
    public class GameControlService : IGameControlService
    {
        private readonly IGameTimerService gameTimer;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IDbContextFactory<DefaultContext> contextFactory;

        public GameControlService(IGameTimerService gameTimer, IHttpContextAccessor httpContextAccessor, IDbContextFactory<DefaultContext> contextFactory)
        {
            this.gameTimer = gameTimer;
            this.httpContextAccessor = httpContextAccessor;
            this.contextFactory = contextFactory;
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
                throw new ArgumentException("User isn't participating in any in progress games.");

            if (!gm.CurrentRound.IsQuestionVotingOpen)
                throw new ArgumentException("The voting stage for this question is either over or not started.");

            if (!gm.CurrentRound.Question.Answers.Any(x => x.Id == answerId))
                throw new ArgumentException("The provided answerID isn't valid for this question.");

            switch (gm.CurrentRound.AttackStage)
            {
                case AttackStage.MULTIPLE_NEUTRAL:
                    var playerAttacking = gm.CurrentRound
                        .NeutralRound
                        .TerritoryAttackers
                        .First(x => x.AttackerId == userId);

                    if (playerAttacking.AttackerMChoiceQAnswerId != null)
                        throw new ArgumentException("This user already voted for this question");

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

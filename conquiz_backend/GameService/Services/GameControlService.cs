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
        private readonly IGameTimer gameTimer;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IDbContextFactory<DefaultContext> contextFactory;

        public GameControlService(IGameTimer gameTimer, IHttpContextAccessor httpContextAccessor, IDbContextFactory<DefaultContext> contextFactory)
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

            var gm = await db.GameInstance
                .Include(x => x.Participants)
                .Include(x => x.Rounds)
                .ThenInclude(x => x.RoundAnswers)
                .Include(x => x.Rounds)
                .ThenInclude(x => x.Questions)
                .ThenInclude(x => x.Answers)
                .Where(x => x.GameState == GameState.IN_PROGRESS && x.Participants
                    .Any(y => y.PlayerId == userId))
                .Select(x => new
                {
                    CurrentRound = x.Rounds.First(x => x.GameRoundNumber == x.GameRoundNumber),
                })
                .FirstOrDefaultAsync();

            if (gm == null || gm.CurrentRound == null)
                throw new ArgumentException("User isn't participating in any in progress games.");

            if (gm.CurrentRound.RoundAnswers.Any(x => x.UserId == userId))
                throw new ArgumentException("This user already voted for this round.");

            if (!gm.CurrentRound.Questions.First(x => x.Type == "multiple").Answers.Any(x => x.Id == answerId))
                throw new ArgumentException("The provided answerID isn't valid for this question.");

            if (!gm.CurrentRound.IsVotingOpen)
                throw new ArgumentException("The voting stage for this question is either over or not started.");


            // If it's a normal 1v1 matchup, check if the current user is either the attacker or defender
            // If he ain't then he can't vote
            //if(!gm.CurrentRound.)
            //{
            //    // If the current person is neither the attacker or defender
            //    if(!(gm.CurrentRound.AttackerId != null && gm.CurrentRound.AttackerId == userId) &&
            //        !(gm.CurrentRound.DefenderId != null && gm.CurrentRound.DefenderId == userId)
            //    )
            //    {
            //        throw new ArgumentException("This user is neither the attack nor the defender of a territory");
            //    }
            //}

            var roundAnswer = new RoundAnswers()
            {
                UserId = userId,
                AnswerId = answerId,
                AnsweredAt = answeredAt,
            };
            gm.CurrentRound.RoundAnswers.Add(roundAnswer);
            db.Update(gm.CurrentRound.RoundAnswers);

            await db.SaveChangesAsync();
        }

        public async Task AnswerNumberQuestion()
        {

        }
    }
}

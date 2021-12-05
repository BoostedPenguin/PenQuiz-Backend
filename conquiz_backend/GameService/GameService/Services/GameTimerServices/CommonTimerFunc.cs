using GameService.Context;
using GameService.Hubs;
using GameService.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.GameTimerServices
{
    public class UserAttackOrder
    {
        /// <summary>
        /// Goes like this:
        /// First 3 rounds are stored in a list
        /// Each person gets random attack order in them: 2, 3, 1
        /// Then that gets stored in a list itself
        /// 1 {1, 3, 2}   2 {2, 3, 1}  3{3, 1, 2} etc.
        /// </summary>
        public List<List<int>> UserRoundAttackOrders { get; set; }
        public int TotalTerritories { get; set; }
        public int LeftTerritories { get; set; }

        public UserAttackOrder(List<List<int>> userRoundAttackOrders, int totalTerritories, int leftTerritories)
        {
            this.UserRoundAttackOrders = userRoundAttackOrders;
            this.TotalTerritories = totalTerritories;
            this.LeftTerritories = leftTerritories;
        }
    }

    public class CommonTimerFunc
    {
        private static readonly Random r = new Random();
        public static async Task<GameInstance> GetFullGameInstance(int gameInstanceId, DefaultContext defaultContext)
        {
            var game = await defaultContext.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Include(x => x.Rounds)
                .ThenInclude(x => x.NeutralRound)
                .ThenInclude(x => x.TerritoryAttackers)
                .Include(x => x.Rounds)
                .ThenInclude(x => x.PvpRound)
                .ThenInclude(x => x.PvpRoundAnswers)
                .Include(x => x.ObjectTerritory)
                .ThenInclude(x => x.MapTerritory)
                .FirstOrDefaultAsync(x => x.Id == gameInstanceId);

            game.Rounds = game.Rounds.OrderBy(x => x.GameRoundNumber).ToList();

            var ss = game.Rounds.OrderBy(x => x.GameRoundNumber).ToList();
            foreach (var round in game.Rounds)
            {
                if(round.AttackStage == AttackStage.MULTIPLE_NEUTRAL || round.AttackStage == AttackStage.NUMBER_NEUTRAL)
                {
                    round.NeutralRound.TerritoryAttackers =
                        round.NeutralRound.TerritoryAttackers.OrderBy(x => x.AttackOrderNumber).ToList();
                }
            }

            return game;
        }

        public static UserAttackOrder GenerateAttackOrder(List<int> userIds, int totalTerritories, int RequiredPlayers, bool excludeCapitals = true)
        {
            if (userIds.Count != RequiredPlayers) throw new ArgumentException("There must be a total of 3 people in a game!");

            // 1 3 2   3 2 1

            // Removing the capital territories;
            int emptyTerritories = totalTerritories;

            if (excludeCapitals)
                emptyTerritories = totalTerritories - RequiredPlayers;

            if (emptyTerritories < RequiredPlayers) throw new ArgumentException("There are less than 3 territories left except the capital. Abort the game.");

            // Store 
            var attackOrder = new List<List<int>>();
            while (emptyTerritories >= RequiredPlayers)
            {
                var fullRound = new List<int>();

                while (fullRound.Count < RequiredPlayers)
                {
                    var person = userIds[r.Next(0, RequiredPlayers)];
                    if (!fullRound.Contains(person))
                    {
                        fullRound.Add(person);
                    }
                }
                attackOrder.Add(fullRound);
                emptyTerritories -= fullRound.Count();
            }

            return new UserAttackOrder(attackOrder, totalTerritories, emptyTerritories);
        }
    }
}

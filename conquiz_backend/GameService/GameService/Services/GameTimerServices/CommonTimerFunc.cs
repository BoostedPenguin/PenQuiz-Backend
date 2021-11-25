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
    public class CommonTimerFunc
    {
        public static async Task<GameInstance> GetFullGameInstance(int gameInstanceId, DefaultContext defaultContext)
        {
            var game = await defaultContext.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Include(x => x.Rounds)
                .ThenInclude(x => x.NeutralRound)
                .ThenInclude(x => x.TerritoryAttackers)
                .Include(x => x.ObjectTerritory)
                .ThenInclude(x => x.MapTerritory)
                .FirstOrDefaultAsync(x => x.Id == gameInstanceId);

            game.Rounds = game.Rounds.OrderBy(x => x.GameRoundNumber).ToList();

            var ss = game.Rounds.OrderBy(x => x.GameRoundNumber).ToList();
            foreach (var round in game.Rounds)
            {
                round.NeutralRound.TerritoryAttackers =
                    round.NeutralRound.TerritoryAttackers.OrderBy(x => x.AttackOrderNumber).ToList();
            }


            return game;
        }
    }
}

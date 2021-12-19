using GameService.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameService.Services.Extensions;
using GameService.Models;
using GameService.Dtos;

public interface IStatisticsService
{
    public Task<UserStatisticsDto> GetUserGameStatistics();
}

public class StatisticsService : IStatisticsService
{
    private readonly IDbContextFactory<DefaultContext> contextFactory;
    private readonly IHttpContextAccessor httpContextAccessor;

    public StatisticsService(IDbContextFactory<DefaultContext> contextFactory, IHttpContextAccessor httpContextAccessor)
    {
        this.contextFactory = contextFactory;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<UserStatisticsDto> GetUserGameStatistics()
    {
        using var db = contextFactory.CreateDbContext();
        var userId = httpContextAccessor.GetCurrentUserId();

        var games = await db.GameInstance
            .Include(x => x.Participants)
            .Where(x => x.Participants.Any(y => y.PlayerId == userId) && x.GameState == GameState.FINISHED)
            .AsNoTracking()
            .ToListAsync();

        if (games.Count() == 0)
            return new UserStatisticsDto()
            {
                GamesWon = 0,
                TotalGames = 0,
                WinPercentage = "0"
            };

        var gamesWon = 0;
        foreach(var game in games)
        {
            game.Participants = game.Participants.OrderByDescending(x => x.Score).ToList();
            if (game.Participants.First().PlayerId != userId) continue;
            gamesWon++;
        }

        var result = new UserStatisticsDto()
        {
            GamesWon = gamesWon,
            TotalGames = games.Count(),
            WinPercentage = (gamesWon * 100.0 / games.Count()).ToString("0.00")
        };

        return result;
    }
}
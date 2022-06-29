using GameService.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Shouldly;
using GameService.Data;
using GameService.Data.Models;
using GameService.Services.REST_Services;

namespace GameServiceUnitTests
{
    public class StatisticsServiceTest
    {
        IDbContextFactory<DefaultContext> mockContextFactory;
        IStatisticsService statisticsService;
        Mock<IHttpContextAccessor> mockHttpContextAccessor;

        public StatisticsServiceTest()
        {
            mockContextFactory = new TestDbContextFactory("StatServiceTest");
            mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
            };

            mockHttpContextAccessor.Setup(h => h.HttpContext.User.Claims).Returns(claims);
            statisticsService = new StatisticsService(mockContextFactory, mockHttpContextAccessor.Object);
        }

        [Fact]
        public async Task GetPlayerStatisticsTest()
        {
            using var db = mockContextFactory.CreateDbContext();

            // Arrange
            var playerOne = new Users()
            {
                UserGlobalIdentifier = "123",
                Username = "PlayerOne",
            };
            var playerTwo = new Users()
            {
                UserGlobalIdentifier = "152",
                Username = "PlayerTwo",
            };
            var playerThree = new Users()
            {
                UserGlobalIdentifier = "181",
                Username = "PlayerThree",
            };
            db.Add(playerOne);
            db.Add(playerTwo);
            db.Add(playerThree);
            var allGames = new List<GameInstance>();
            for (var i = 0; i < 4; i++)
            {
                var gm = new GameInstance()
                {
                    GameGlobalIdentifier = Guid.NewGuid().ToString(),
                    GameState = GameState.FINISHED,
                };
                gm.Participants.Add(new Participants()
                {
                    PlayerId = playerOne.Id,
                    Score = 1500,
                });
                gm.Participants.Add(new Participants()
                {
                    PlayerId = playerTwo.Id,
                    Score = 1000,
                });
                gm.Participants.Add(new Participants()
                {
                    PlayerId = playerThree.Id,
                    Score = 500,
                });

                allGames.Add(gm);
            }
            db.AddRange(allGames);
            await db.SaveChangesAsync();


            // Second user trying to join lobby
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, playerOne.UserGlobalIdentifier),
            };

            mockHttpContextAccessor.Setup(h => h.HttpContext.User.Claims).Returns(claims);

            var secondaryUserService = new StatisticsService(mockContextFactory, mockHttpContextAccessor.Object);

            var playerStats = await secondaryUserService.GetUserGameStatistics();
            playerStats.GamesWon.ShouldBe(4);
            playerStats.TotalGames.ShouldBe(4);
            playerStats.WinPercentage.ShouldBe("100.00");

        }
    }
}

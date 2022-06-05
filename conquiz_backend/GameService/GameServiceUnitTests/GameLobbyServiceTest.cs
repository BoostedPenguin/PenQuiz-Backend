using GameService.Context;
using GameService.Data;
using GameService.Data.Models;
using GameService.Dtos;
using GameService.MessageBus;
using GameService.Services;
using GameService.Services.GameLobbyServices;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace GameServiceUnitTests
{

    public class GameLobbyServiceTest
    {
        IDbContextFactory<DefaultContext> mockContextFactory;
        IGameLobbyService gameLobbyService;
        IMapGeneratorService mapGeneratorService;
        Mock<IHttpContextAccessor> mockHttpContextAccessor;
        ILogger<MapGeneratorService> loggerMoq = Mock.Of<ILogger<MapGeneratorService>>();
        public GameLobbyServiceTest()
        {
            mockContextFactory = new TestDbContextFactory("GameLobbyService");
            mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();


            // Arrange
            var playerOne = new Users()
            {
                UserGlobalIdentifier = "123",
                Username = "PlayerOne",
            };
            using var db = mockContextFactory.CreateDbContext();

            db.Add(playerOne);
            db.SaveChanges();

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, playerOne.UserGlobalIdentifier),
            };

            mockHttpContextAccessor.Setup(h => h.HttpContext.User.Claims).Returns(claims);

            mapGeneratorService = new MapGeneratorService(mockContextFactory, loggerMoq);

            gameLobbyService = new GameLobbyService(null, mockContextFactory, mockHttpContextAccessor.Object, new MapGeneratorService(mockContextFactory, loggerMoq));
        }


        [Fact]
        public async Task CreateLobbyNoConflictsTest()
        {
            var gameInstance = await gameLobbyService.CreateGameLobby();

            Assert.NotNull(gameInstance);
            Assert.True(gameInstance.GameState == GameState.IN_LOBBY);
        }

        [Fact]
        public async Task CreateLobbyExistingGameTest()
        {
            var firstGame = await gameLobbyService.CreateGameLobby();
            var secondGame = await gameLobbyService.CreateGameLobby();

            Assert.Same(firstGame.InvitationLink, secondGame.InvitationLink);
        }

        [Fact]
        public async Task JoinGameLobbyTest()
        {
            // Second user trying to join lobby
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();

            var playerTwo = new Users()
            {
                UserGlobalIdentifier = "152",
                Username = "PlayerTwo",
            };
            using var db = mockContextFactory.CreateDbContext();
            db.Add(playerTwo);
            await db.SaveChangesAsync();

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, playerTwo.UserGlobalIdentifier),
            };

            mockHttpContextAccessor.Setup(h => h.HttpContext.User.Claims).Returns(claims);

            var secondaryUserService = new GameLobbyService(null, mockContextFactory, mockHttpContextAccessor.Object,
                new MapGeneratorService(mockContextFactory, loggerMoq));


            var initialGame = await gameLobbyService.CreateGameLobby();
            var result = await secondaryUserService.JoinGameLobby(initialGame.InvitationLink);

            Assert.Same(initialGame.InvitationLink, result.GameInstance.InvitationLink);
        }

        [Fact]
        public async Task JoinPublicGameLobbyTest()
        {
            // Second user trying to join lobby
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            var playerTwo = new Users()
            {
                UserGlobalIdentifier = "152",
                Username = "PlayerTwo",
            };
            using var db = mockContextFactory.CreateDbContext();
            db.Add(playerTwo);
            await db.SaveChangesAsync();

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, playerTwo.UserGlobalIdentifier),
            };

            mockHttpContextAccessor.Setup(h => h.HttpContext.User.Claims).Returns(claims);

            var secondaryUserService = new GameLobbyService(null, mockContextFactory, mockHttpContextAccessor.Object,
                new MapGeneratorService(mockContextFactory, loggerMoq));


            var initialGame = await gameLobbyService.FindPublicMatch();
            var result = await secondaryUserService.JoinGameLobby(initialGame.GameInstance.InvitationLink);

            Assert.Same(initialGame.GameInstance.InvitationLink, result.GameInstance.InvitationLink);
        }

        [Fact]
        public async Task CreatePublicGameLobbyTest()
        {
            // Second user trying to join lobby
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, "2"),
            };

            mockHttpContextAccessor.Setup(h => h.HttpContext.User.Claims).Returns(claims);

            var secondaryUserService = new GameLobbyService(null, mockContextFactory, mockHttpContextAccessor.Object,
                new MapGeneratorService(mockContextFactory, loggerMoq));


            var initialGame = await gameLobbyService.FindPublicMatch();
            Assert.NotNull(initialGame);
        }

        [Fact]
        public async Task JoinNonExistingGameLobbyTest()
        {
            // Second user trying to join lobby
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, "2"),
            };

            mockHttpContextAccessor.Setup(h => h.HttpContext.User.Claims).Returns(claims);
            var secondaryUserService = new GameLobbyService(null, mockContextFactory, mockHttpContextAccessor.Object,
                new MapGeneratorService(mockContextFactory, loggerMoq));


            var initialGame = await gameLobbyService.CreateGameLobby();

            var error = await Should.ThrowAsync<JoiningGameException>(() =>
            secondaryUserService.JoinGameLobby("12131"));

            error.Message.ShouldBe("The invitation link is invalid");
        }

        [Fact]
        public async Task StartGameTest()
        {
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

            var mockContextFactory = new TestDbContextFactory("StartGameTest");
            await new MapGeneratorService(mockContextFactory, loggerMoq).ValidateMap(mockContextFactory.CreateDbContext());
            using var db = mockContextFactory.CreateDbContext();

            db.Add(playerOne);
            db.Add(playerTwo);
            db.Add(playerThree);

            var gm = new GameInstance()
            {
                GameGlobalIdentifier = Guid.NewGuid().ToString(),
                GameRoundNumber = 1,
                GameCreatorId = playerOne.Id,
                GameState = GameState.IN_LOBBY,
                InvitationLink = "1821",
                Mapid = 1,
            };

            gm.Participants.Add(new Participants()
            {
                PlayerId = playerOne.Id,
            });
            gm.Participants.Add(new Participants()
            {
                PlayerId = playerTwo.Id,
            });
            gm.Participants.Add(new Participants()
            {
                PlayerId = playerThree.Id,
            });

            db.Add(gm);
            await db.SaveChangesAsync();

            // Second user trying to join lobby
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var messageBusMock = new Mock<IMessageBusClient>();
            messageBusMock.Setup(x =>
                x.RequestQuestions(It.IsAny<RequestQuestionsDto>()));

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, playerOne.UserGlobalIdentifier),
            };

            mockHttpContextAccessor.Setup(h => h.HttpContext.User.Claims).Returns(claims);
            var gameLobbyService = new GameLobbyService(null, mockContextFactory, mockHttpContextAccessor.Object,
                new MapGeneratorService(mockContextFactory, loggerMoq));

            // Act
            var result = await gameLobbyService.StartGame();

            // Assert
            Assert.Equal(GameState.IN_PROGRESS, result.GameState);
            Assert.Equal(3, result.Participants.Count());
            result.ObjectTerritory.ShouldNotBeEmpty();
            result.ObjectTerritory
                .Where(x => x.IsCapital)
                .ToList()
                .Count()
                .ShouldBeEquivalentTo(3);
            result.Rounds.ShouldNotBeEmpty();
        }
    }
}

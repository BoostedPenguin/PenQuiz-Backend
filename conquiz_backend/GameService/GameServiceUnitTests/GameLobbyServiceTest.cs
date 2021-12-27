﻿using GameService.Context;
using GameService.Dtos;
using GameService.MessageBus;
using GameService.Models;
using GameService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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
        public GameLobbyServiceTest()
        {
            mockContextFactory = new TestDbContextFactory("GameLobbyService");
            mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
            };

            mockHttpContextAccessor.Setup(h => h.HttpContext.User.Claims).Returns(claims);
            mapGeneratorService = new MapGeneratorService(mockContextFactory);

            gameLobbyService = new GameLobbyService(mockContextFactory, mockHttpContextAccessor.Object, new MapGeneratorService(mockContextFactory), null);
        }


        [Fact]
        public async Task CreateLobbyNoConflictsTest()
        {
            var gameInstance = await gameLobbyService.CreateGameLobby();

            Assert.NotNull(gameInstance);
            Assert.True(gameInstance.GameState == GameService.Models.GameState.IN_LOBBY);
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
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, "2"),
            };

            mockHttpContextAccessor.Setup(h => h.HttpContext.User.Claims).Returns(claims);

            var secondaryUserService = new GameLobbyService(mockContextFactory, mockHttpContextAccessor.Object, 
                new MapGeneratorService(mockContextFactory), null);


            var initialGame = await gameLobbyService.CreateGameLobby();
            var result = await secondaryUserService.JoinGameLobby(initialGame.InvitationLink);

            Assert.Same(initialGame.InvitationLink, result.InvitationLink);
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
            var secondaryUserService = new GameLobbyService(mockContextFactory, mockHttpContextAccessor.Object,
                new MapGeneratorService(mockContextFactory), null);


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
                ExternalId = 123,
                Username = "PlayerOne",
            };
            var playerTwo = new Users()
            {
                ExternalId = 152,
                Username = "PlayerTwo",
            };
            var playerThree = new Users()
            {
                ExternalId = 181,
                Username = "PlayerThree",
            };

            var mockContextFactory = new TestDbContextFactory("StartGameTest");
            await new MapGeneratorService(mockContextFactory).ValidateMap();
            using var db = mockContextFactory.CreateDbContext();

            db.Add(playerOne);
            db.Add(playerTwo);
            db.Add(playerThree);

            var gm = new GameInstance()
            {
                GameRoundNumber = 1,
                GameCreatorId = playerOne.Id,
                GameState = GameState.IN_LOBBY,
                InvitationLink = "1821",
                Mapid = 1,
            };

            gm.Participants.Add(new Participants()
            {
                AvatarName = "penguinAvatar1",
                PlayerId = playerOne.Id,
            });
            gm.Participants.Add(new Participants()
            {
                AvatarName = "penguinAvatar2",
                PlayerId = playerTwo.Id,
            });
            gm.Participants.Add(new Participants()
            {
                AvatarName = "penguinAvatar3",
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
                new Claim(ClaimTypes.NameIdentifier, playerOne.Id.ToString()),
            };

            mockHttpContextAccessor.Setup(h => h.HttpContext.User.Claims).Returns(claims);
            var gameLobbyService = new GameLobbyService(mockContextFactory, mockHttpContextAccessor.Object,
                new MapGeneratorService(mockContextFactory), messageBusMock.Object);

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
﻿using GameService.Context;
using GameService.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace GameServiceUnitTests
{
    public class MapGeneratorServiceTest
    {
        IDbContextFactory<DefaultContext> mockContextFactory;
        DefaultContext context;
        MapGeneratorService service;
        public MapGeneratorServiceTest()
        {
            mockContextFactory = new TestDbContextFactory("MapGenService");
            context = mockContextFactory.CreateDbContext();

            service = new MapGeneratorService(mockContextFactory);
            _ = service.ValidateMap();
        }

        [Fact]
        public async Task TestBorderingTerritories()
        {
            var result = await service.AreTheyBorders("Vibri", "Ranku", "Antarctica");

            Assert.True(result);
        }

        [Fact]
        public async Task TestNonBorderingTerritories()
        {
            var result = await service.AreTheyBorders("Dager", "Lisu", "Antarctica");

            Assert.False(result);
        }

        [Fact]
        public async Task TestNumberOfTerritories()
        {
            var mapId = await context.Maps.Select(x => x.Id).FirstAsync();

            var amount = await service.GetAmountOfTerritories(mapId);

            Assert.Equal(20, amount);
        }
    }
}

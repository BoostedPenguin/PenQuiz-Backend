using GameService.Context;
using GameService.Data;
using GameService.Services;
using GameService.Services.GameTimerServices;
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
        public async Task TestValidateMap()
        {
            await service.ValidateMap();
            var db = mockContextFactory.CreateDbContext();
            var maps = db.Maps.ToList();

            Assert.Single(maps);
        }

        [Fact]
        public async Task TestNonBorderingTerritories()
        {
            var result = await service.AreTheyBorders("Dager", "Lisu", "Antarctica");

            Assert.False(result);
        }

        [Fact]
        public async Task TestNonBordersById()
        {
            var db = mockContextFactory.CreateDbContext();
            var lisu = db.MapTerritory.First(x => x.TerritoryName == "Lisu");
            var dager = db.MapTerritory.First(x => x.TerritoryName == "Dager");
            var result = await service.AreTheyBorders(dager.Id, lisu.Id);

            Assert.False(result);
        }

        [Fact]
        public async Task TestBordersById()
        {
            var db = mockContextFactory.CreateDbContext();
            var Vibri = db.MapTerritory.First(x => x.TerritoryName == "Vibri");
            var Ranku = db.MapTerritory.First(x => x.TerritoryName == "Ranku");
            var result = await service.AreTheyBorders(Vibri.Id, Ranku.Id);

            Assert.True(result);
        }

        [Fact]
        public async Task TestNumberOfTerritories()
        {
            var mapId = await context.Maps.Select(x => x.Id).FirstAsync();

            var amount = await service.GetAmountOfTerritories(mapId);

            Assert.Equal(20, amount);
        }

        [Fact]
        public async Task TestTimeElevation()
        {
            // Now || 15 seconds in the pass
            var mapId = await context.Maps.Select(x => x.Id).FirstAsync();

            var amount = await service.GetAmountOfTerritories(mapId);

            Assert.Equal(20, amount);
        }
    }
}

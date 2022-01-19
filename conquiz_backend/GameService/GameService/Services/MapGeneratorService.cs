using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using GameService.Context;
using GameService.Services.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameService.Data.Models;
using GameService.Data;
using Microsoft.Extensions.Logging;

namespace GameService.Services
{
    public interface IMapGeneratorService
    {
        /// <summary>
        /// Validate if the map and it's territories are in the database. If not regenerate the map itself.
        /// </summary>
        /// <returns></returns>
        Task ValidateMap();
        Task<bool> AreTheyBorders(DefaultContext context, string territoryName, string territoryName2, string mapName);
        Task<bool> AreTheyBorders(DefaultContext context, int territoryId, int territoryId2);
        Task<MapTerritory[]> GetBorders(DefaultContext context, string territoryName, string mapName);
        Task<MapTerritory[]> GetBorders(DefaultContext context, int territoryId);
        Task<MapTerritory[]> GetBorders(DefaultContext context, int[] territoryId);
        Task<int> GetAmountOfTerritories(DefaultContext context, int mapId);
    }

    public class MapGeneratorService : DataService<DefaultModel>, IMapGeneratorService
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly ILogger<MapGeneratorService> logger;
        private const string defaultMapFile = "Antarctica";
        public MapGeneratorService(IDbContextFactory<DefaultContext> _contextFactory, ILogger<MapGeneratorService> logger) : base(_contextFactory)
        {
            contextFactory = _contextFactory;
            this.logger = logger;
        }

        private async Task<bool> AddBorderIfNotExistant(int territoryId, int territoryId2)
        {
            using var a = contextFactory.CreateDbContext();
            var areBorders = await AreTheyBorders(a, territoryId, territoryId2);

            if (!areBorders)
            {
                a.Add(new Borders() { ThisTerritory = territoryId, NextToTerritory = territoryId2 });
                await a.SaveChangesAsync();

                return true;
            }

            return false;
        }

        private async Task<bool> AddBorderIfNotExistant(string territoryName, string territoryName2)
        {
            using var a = contextFactory.CreateDbContext();

            var bothTerritories = await a.MapTerritory
                .Where(x => x.TerritoryName == territoryName || x.TerritoryName == territoryName2).ToListAsync();

            if (bothTerritories.Count < 2) throw new ArgumentException("There was 1 or more territory name which didn't exist in our db.");
            if (bothTerritories.Count > 2) throw new ArgumentException("There was 1 or more territory name which was duplicated in our db.");

            var areBorders = await AreTheyBorders(a, bothTerritories[0].Id, bothTerritories[1].Id);

            if (!areBorders)
            {
                a.Add(new Borders() { ThisTerritory = bothTerritories[0].Id, NextToTerritory = bothTerritories[1].Id });
                await a.SaveChangesAsync();

                return true;
            }

            return false;
        }

        public async Task<MapTerritory[]> GetBorders(DefaultContext context, string territoryName, string mapName)
        {
            using var a = contextFactory.CreateDbContext();
            var territory = await context.MapTerritory
                .Include(x => x.Map)
                .Where(x => x.Map.Name == mapName)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TerritoryName == territoryName);

            if (territory == null) throw new ArgumentException("This territory doesn't belong to this map or doesn't exist at all");

            return await GetBorders(context, territory.Id);
        }

        public async Task<MapTerritory[]> GetBorders(DefaultContext context, int[] territoryIds)
        {
            var nonRepeatingBorders = new List<MapTerritory>();
            foreach(var territoryId in territoryIds)
            {
                var theseBorders = await GetBorders(context, territoryId);
                var uniqueBorders = theseBorders
                    .Where(x => nonRepeatingBorders.All(y => y.TerritoryName != x.TerritoryName)).ToList();
                nonRepeatingBorders.AddRange(uniqueBorders);
            }

            return nonRepeatingBorders.ToArray();
        }


        public async Task<MapTerritory[]> GetBorders(DefaultContext context, int territoryId)
        {
            var borderTerritories = await context.MapTerritory
                .Include(x => x.Map)
                .Include(x => x.BordersNextToTerritoryNavigation)
                .ThenInclude(x => x.ThisTerritoryNavigation)

                .Include(x => x.BordersThisTerritoryNavigation)
                .ThenInclude(x => x.NextToTerritoryNavigation)

                .Where(x => x.Id == territoryId)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync();


            var btRelations = new
            {
                left = borderTerritories.BordersNextToTerritoryNavigation
                        .Select(x => x.ThisTerritory == territoryId ? x.NextToTerritoryNavigation : x.ThisTerritoryNavigation).ToList(),
                right = borderTerritories.BordersThisTerritoryNavigation
                        .Select(x => x.ThisTerritory == territoryId ? x.NextToTerritoryNavigation : x.ThisTerritoryNavigation).ToList()
            };

            return btRelations.left.Concat(btRelations.right).ToArray();
        }

        /// <summary>
        /// Search database if the the given territory ID's are next to each other.
        /// You don't need to provide a mapId, because it's not relevant for the search query.
        /// As long as the database borders are setup properly, the map itself isn't of any concern.
        /// </summary>
        /// <param name="territoryId"></param>
        /// <param name="territoryId2"></param>
        /// <returns></returns>
        public async Task<bool> AreTheyBorders(DefaultContext context, int territoryId, int territoryId2)
        {
            var borders = await context.Borders
                .Where(x => (x.NextToTerritory == territoryId && x.ThisTerritory == territoryId2) || (x.NextToTerritory == territoryId2 && x.ThisTerritory == territoryId))
                .AsNoTracking()
                .ToListAsync();

            return borders.Count != 0;
        }

        private async Task GenerateDefaultMap()
        {
            // Read json file
            Dictionary<string, List<string>> borders = ReadMapJson();

            // Generate map with territories
            await GenerateMap(borders);

            // Generate borders for each territory
            await GenerateBorders(borders);
        }

        private async Task GenerateMap(Dictionary<string, List<string>> borders)
        {
            using var a = contextFactory.CreateDbContext();
            var firstMap = new Maps() { Name = defaultMapFile };

            var mapTerritories = new List<MapTerritory>();


            foreach (var mainTerritory in borders)
            {
                mapTerritories.Add(new MapTerritory() { TerritoryName = mainTerritory.Key });
            }

            firstMap.MapTerritory = mapTerritories;

            a.Add(firstMap);
            await a.SaveChangesAsync();
        }

        private async Task GenerateBorders(Dictionary<string, List<string>> borders)
        {
            // Add borders
            foreach (var mainTerritory in borders)
            {
                foreach (var border in mainTerritory.Value)
                {
                    await AddBorderIfNotExistant(mainTerritory.Key, border);
                }
            }
        }

        private Dictionary<string, List<string>> ReadMapJson()
        {
            using StreamReader r = new StreamReader($"{defaultMapFile}.json");

            string json = r.ReadToEnd();
            return JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
        }

        /// <summary>
        /// Should be ran on every start to validate if map exists or not
        /// </summary>
        /// <returns></returns>
        public async Task ValidateMap()
        {
            using var a = contextFactory.CreateDbContext();
            var fileTerritories = ReadMapJson();

            var antarctica = await a.Maps
                .Include(x => x.MapTerritory)
                .Where(x => x.Name == defaultMapFile)
                .FirstOrDefaultAsync();

            if (antarctica == null)
            {
                logger.LogInformation($"`{defaultMapFile}` doesn't exist. Attempting to re-create.");
                await GenerateDefaultMap();
                logger.LogInformation($"`{defaultMapFile}` successfully re-created.");
                return;
            }

            // Regenerate everything
            if (antarctica.MapTerritory.Count != fileTerritories.Count)
            {
                a.Remove(antarctica);
                a.RemoveRange(antarctica.MapTerritory);
                await a.SaveChangesAsync();

                await GenerateDefaultMap();
                return;
            }

            foreach (var territory in ReadMapJson())
            {
                if (antarctica.MapTerritory.FirstOrDefault(x => x.TerritoryName == territory.Key) == null)
                {
                    // This territory is missing
                    throw new ArgumentException($"`{territory.Key}` territory is missing from the database. Please notify an administrator.");
                }
            }

            logger.LogInformation($"`{defaultMapFile}` map validated.");
        }

        public async Task<bool> AreTheyBorders(DefaultContext context, string territoryName, string territoryName2, string mapName)
        {
            var bothTerritories = await context.MapTerritory
                .Include(x => x.Map)
                .Where(x => x.Map.Name == mapName)
                .Where(x => x.TerritoryName == territoryName || x.TerritoryName == territoryName2)
                .AsNoTracking()
                .ToListAsync();

            if (bothTerritories.Count < 2) throw new ArgumentException("There was 1 or more territory name, bound to this map, which didn't exist in our db.");
            if (bothTerritories.Count > 2) throw new ArgumentException("There was 1 or more territory name, bound to this map, which was duplicated in our db.");

            return await AreTheyBorders(context, bothTerritories[0].Id, bothTerritories[1].Id);
        }

        public async Task<int> GetAmountOfTerritories(DefaultContext context, int mapId)
        {
            var totalTerritories = await context.MapTerritory.Where(x => x.MapId == mapId).AsNoTracking().CountAsync();

            return totalTerritories;
        }
    }
}

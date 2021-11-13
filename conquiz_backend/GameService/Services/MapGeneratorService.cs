using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using GameService.Context;
using GameService.Models;
using GameService.Services.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services
{
    public interface IMapGeneratorService
    {
        /// <summary>
        /// Validate if the map and it's territories are in the database. If not regenerate the map itself.
        /// </summary>
        /// <returns></returns>
        Task ValidateMap();
        Task<bool> AreTheyBorders(string territoryName, string territoryName2, string mapName);
        Task<bool> AreTheyBorders(int territoryId, int territoryId2);
        Task<MapTerritory[]> GetBorders(string territoryName, string mapName);
        Task<MapTerritory[]> GetBorders(int territoryId);
        Task<int> GetAmountOfTerritories(int mapId);
        Task<ObjectTerritory> GetRandomMCTerritoryNeutral(int userId, int gameInstanceId);
        Task<ObjectTerritory> SelectTerritoryAvailability(DefaultContext db, int userId, int gameInstanceId, int selectedMapTerritoryId);
    }

    public class MapGeneratorService : DataService<DefaultModel>, IMapGeneratorService
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IHttpContextAccessor httpContextAccessor;
        private const string defaultMapFile = "Antarctica";
        private readonly Random r = new Random();
        public MapGeneratorService(IDbContextFactory<DefaultContext> _contextFactory, IHttpContextAccessor httpContextAccessor) : base(_contextFactory)
        {
            contextFactory = _contextFactory;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task Testing()
        {

            // End

            var borders = await GetBorders("Dager", "Antarctica");


            // IsAttackPossible
            //var result = await areTheyBorders(4, 1);
            var result = await AreTheyBorders("Dager", "Ronetia", "Antarctica");

            await AddBorderIfNotExistant(5, 3);



            // Do something
        }

        private class UserBorderInformation
        {
            public List<ObjectTerritory> UserTerritories { get; set; }
            public List<ObjectTerritory> UntakenBorders { get; set; }
            public List<ObjectTerritory> MatchingBorders { get; set; }
            public MapTerritory[] AllPlayerBorders { get; set; }
        }

        private async Task<UserBorderInformation> GetBorderInformation(DefaultContext db, int userId, int gameInstanceId, bool takenByCheck = true)
        {
            var userTerritories = await db.ObjectTerritory
                .Include(x => x.MapTerritory)
                .Where(x => x.GameInstanceId == gameInstanceId && x.TakenBy == userId)
                .ToListAsync();


            // There is a possibility where people get blocked off
            // So in case all takenterritories don't border anything, you need a random select
            var allPlayerBorders = await GetBorders(userTerritories.Select(x => x.MapTerritoryId).ToArray());

            List<ObjectTerritory> untakenBorder;

            // Takes into account takenBy to ensure that the territory doesnt belong to anyone
            if(takenByCheck)
            {
                untakenBorder = await db.ObjectTerritory
                .Include(x => x.MapTerritory)
                .Where(x =>
                    x.GameInstanceId == gameInstanceId &&
                    x.TakenBy == null &&
                    x.AttackedBy == null)
                .ToListAsync();
            }
            else
            {
                untakenBorder = await db.ObjectTerritory
                    .Include(x => x.MapTerritory)
                    .Where(x =>
                        x.GameInstanceId == gameInstanceId &&
                        x.AttackedBy == null)
                    .ToListAsync();
            }

            var matchingBorders = untakenBorder.Where(x => allPlayerBorders.Any(y => x.MapTerritoryId == y.Id)).ToList();

            return new UserBorderInformation()
            {
                UserTerritories = userTerritories,
                AllPlayerBorders = allPlayerBorders,
                UntakenBorders = untakenBorder,
                MatchingBorders = matchingBorders
            };
        }

        /// <summary>
        /// Needs to handle both neutral territory attacking and pvp
        /// Do NOT check for if the territory is taken, we don't care if it is, we check later
        /// </summary>
        /// <param name="db"></param>
        /// <param name="userId"></param>
        /// <param name="gameInstanceId"></param>
        /// <param name="selectedMapTerritoryId"></param>
        /// <returns></returns>
        public async Task<ObjectTerritory> SelectTerritoryAvailability(DefaultContext db, int userId, int gameInstanceId, int selectedMapTerritoryId)
        {
            var userBorderInfo = await GetBorderInformation(db, userId, gameInstanceId, false);

            var selectedTerritory = userBorderInfo
                .MatchingBorders
                .FirstOrDefault(x => x.MapTerritoryId == selectedMapTerritoryId);

            // The selected map territory isn't a border with the user's current territories
            if(selectedTerritory == null)
            {
                return null;
            }
            // The selected map territory is a border with the user's current territories
            else
            {
                // The selected territory is untaken
                return selectedTerritory;
            }
        }

        public async Task<ObjectTerritory> GetRandomMCTerritoryNeutral(int userId, int gameInstanceId)
        {
            using var db = contextFactory.CreateDbContext();

            var userBorderInfo = await GetBorderInformation(db, userId, gameInstanceId);

            if (userBorderInfo.MatchingBorders.Count() > 0)
            {
                return userBorderInfo.MatchingBorders[r.Next(0, userBorderInfo.MatchingBorders.Count())];
            }
            else
            {
                return userBorderInfo.UntakenBorders[r.Next(0, userBorderInfo.UntakenBorders.Count())];
            }
        }

        private async Task<bool> AddBorderIfNotExistant(int territoryId, int territoryId2)
        {
            using var a = contextFactory.CreateDbContext();
            var areBorders = await AreTheyBorders(territoryId, territoryId2);

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

            var areBorders = await AreTheyBorders(bothTerritories[0].Id, bothTerritories[1].Id);

            if (!areBorders)
            {
                a.Add(new Borders() { ThisTerritory = bothTerritories[0].Id, NextToTerritory = bothTerritories[1].Id });
                await a.SaveChangesAsync();

                return true;
            }

            return false;
        }

        public async Task<MapTerritory[]> GetBorders(string territoryName, string mapName)
        {
            using var a = contextFactory.CreateDbContext();
            var territory = await a.MapTerritory
                .Include(x => x.Map)
                .Where(x => x.Map.Name == mapName)
                .FirstOrDefaultAsync(x => x.TerritoryName == territoryName);

            if (territory == null) throw new ArgumentException("This territory doesn't belong to this map or doesn't exist at all");

            return await GetBorders(territory.Id);
        }

        public async Task<MapTerritory[]> GetBorders(int[] territoryIds)
        {
            using var a = contextFactory.CreateDbContext();

            var nonRepeatingBorders = new List<MapTerritory>();
            foreach(var territoryId in territoryIds)
            {
                var theseBorders = await GetBorders(territoryId);
                nonRepeatingBorders.AddRange(theseBorders.Except(nonRepeatingBorders));
            }

            return nonRepeatingBorders.ToArray();
        }


        public async Task<MapTerritory[]> GetBorders(int territoryId)
        {
            using var a = contextFactory.CreateDbContext();

            var borderTerritories = await a.MapTerritory
                .Include(x => x.Map)
                .Include(x => x.BordersNextToTerritoryNavigation)
                .Include(x => x.BordersThisTerritoryNavigation)
                .Where(x => x.Id == territoryId)
                .Select(x => new
                {
                    left = x.BordersNextToTerritoryNavigation
                        .Select(x => x.ThisTerritory == territoryId ? x.NextToTerritoryNavigation : x.ThisTerritoryNavigation).ToList(),
                    right = x.BordersThisTerritoryNavigation
                        .Select(x => x.ThisTerritory == territoryId ? x.NextToTerritoryNavigation : x.ThisTerritoryNavigation).ToList()
                })
                .FirstOrDefaultAsync();

            return borderTerritories.left.Concat(borderTerritories.right).ToArray();
        }

        /// <summary>
        /// Search database if the the given territory ID's are next to each other.
        /// You don't need to provide a mapId, because it's not relevant for the search query.
        /// As long as the database borders are setup properly, the map itself isn't of any concern.
        /// </summary>
        /// <param name="territoryId"></param>
        /// <param name="territoryId2"></param>
        /// <returns></returns>
        public async Task<bool> AreTheyBorders(int territoryId, int territoryId2)
        {
            using var a = contextFactory.CreateDbContext();
            
            var borders = await a.Borders
                .Where(x => (x.NextToTerritory == territoryId && x.ThisTerritory == territoryId2) || (x.NextToTerritory == territoryId2 && x.ThisTerritory == territoryId))
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
                Console.WriteLine($"`{defaultMapFile}` doesn't exist. Attempting to re-create.");
                await GenerateDefaultMap();
                Console.WriteLine($"`{defaultMapFile}` successfully re-created.");
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

            Console.WriteLine($"`{defaultMapFile}` map validated.");
        }

        public async Task<bool> AreTheyBorders(string territoryName, string territoryName2, string mapName)
        {
            using var a = contextFactory.CreateDbContext();

            var bothTerritories = await a.MapTerritory
                .Include(x => x.Map)
                .Where(x => x.Map.Name == mapName)
                .Where(x => x.TerritoryName == territoryName || x.TerritoryName == territoryName2).ToListAsync();

            if (bothTerritories.Count < 2) throw new ArgumentException("There was 1 or more territory name, bound to this map, which didn't exist in our db.");
            if (bothTerritories.Count > 2) throw new ArgumentException("There was 1 or more territory name, bound to this map, which was duplicated in our db.");

            return await AreTheyBorders(bothTerritories[0].Id, bothTerritories[1].Id);
        }

        public async Task<int> GetAmountOfTerritories(int mapId)
        {
            using var a = contextFactory.CreateDbContext();

            var totalTerritories = await a.MapTerritory.Where(x => x.MapId == mapId).CountAsync();

            return totalTerritories;
        }
    }
}

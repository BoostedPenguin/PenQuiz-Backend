using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using net_core_backend.Context;
using net_core_backend.Models;
using net_core_backend.Services.Interfaces;
using net_core_backend.Services.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace net_core_backend.Services
{
    public class ExampleService : DataService<DefaultModel>, IExampleService
    {
        private readonly IContextFactory contextFactory;
        private readonly IHttpContextAccessor httpContext;

        public ExampleService(IContextFactory _contextFactory, IHttpContextAccessor httpContextAccessor) : base(_contextFactory)
        {
            contextFactory = _contextFactory;
            httpContext = httpContextAccessor;
        }

        public async Task DoSomething()
        {
            await DoesThisMapExist();


            // End

            var borders = await getBorders("Dager");


            // IsAttackPossible
            //var result = await areTheyBorders(4, 1);
            var result = await areTheyBorders("Dager", "Ronetia");

            await addBorderIfNotExistant(5, 3);



            // Do something
        }

        async Task<bool> addBorderIfNotExistant(int territoryId, int territoryId2)
        {
            using var a = contextFactory.CreateDbContext();
            var areBorders = await areTheyBorders(territoryId, territoryId2);

            if (!areBorders)
            {
                a.Add(new Borders() { ThisTerritory = territoryId, NextToTerritory = territoryId2 });
                await a.SaveChangesAsync();

                return true;
            }

            return false;
        }

        async Task<bool> addBorderIfNotExistant(string territoryName, string territoryName2)
        {
            using var a = contextFactory.CreateDbContext();

            var bothTerritories = await a.MapTerritory
                .Where(x => x.TerritoryName == territoryName || x.TerritoryName == territoryName2).ToListAsync();

            if (bothTerritories.Count < 2) throw new ArgumentException("There was 1 or more territory name which didn't exist in our db.");
            if (bothTerritories.Count > 2) throw new ArgumentException("There was 1 or more territory name which was duplicated in our db.");

            var areBorders = await areTheyBorders(bothTerritories[0].Id, bothTerritories[1].Id);

            if (!areBorders)
            {
                a.Add(new Borders() { ThisTerritory = bothTerritories[0].Id, NextToTerritory = bothTerritories[1].Id });
                await a.SaveChangesAsync();

                return true;
            }

            return false;
        }

        async Task<MapTerritory[]> getBorders(string territoryName)
        {
            using var a = contextFactory.CreateDbContext();
            var territory = await a.MapTerritory.FirstOrDefaultAsync(x => x.TerritoryName == territoryName);
            
            return await getBorders(territory.Id);
        }

        async Task<MapTerritory[]> getBorders(int territoryId)
        {
            using var a = contextFactory.CreateDbContext();

            var borderTerritories = await a.MapTerritory
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

        async Task<bool> areTheyBorders(int territoryId, int territoryId2)
        {
            using var a = contextFactory.CreateDbContext();
            var borders = await a.Borders
                .Where(x => (x.NextToTerritory == territoryId && x.ThisTerritory == territoryId2) || (x.NextToTerritory == territoryId2 && x.ThisTerritory == territoryId))
                .ToListAsync();

            return borders.Count != 0;
        }

        async Task GenerateDefaultMap()
        {
            // Read json file
            Dictionary<string, List<string>> borders = ReadMapJson();

            // Generate map with territories
            await generateMap(borders);

            // Generate borders for each territory
            await generateBorders(borders);
        }

        private async Task generateMap(Dictionary<string, List<string>> borders)
        {
            using var a = contextFactory.CreateDbContext();
            var firstMap = new Maps() { Name = "Antarctica" };

            var mapTerritories = new List<MapTerritory>();


            foreach (var mainTerritory in borders)
            {
                mapTerritories.Add(new MapTerritory() { TerritoryName = mainTerritory.Key });
            }

            firstMap.MapTerritory = mapTerritories;

            a.Add(firstMap);
            await a.SaveChangesAsync();
        }

        private async Task generateBorders(Dictionary<string, List<string>> borders)
        {
            // Add borders
            foreach (var mainTerritory in borders)
            {
                foreach (var border in mainTerritory.Value)
                {
                    await addBorderIfNotExistant(mainTerritory.Key, border);
                }
            }
        }

        private Dictionary<string, List<string>> ReadMapJson()
        {
            using StreamReader r = new StreamReader("antarcticaborders.json");
            
            string json = r.ReadToEnd();
            return JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
        }

        /// <summary>
        /// Should be ran on every start to validate if map exists or not
        /// </summary>
        /// <returns></returns>
        async Task DoesThisMapExist()
        {
            using var a = contextFactory.CreateDbContext();
            var fileTerritories = ReadMapJson();

            var antarctica = await a.Maps
                .Include(x => x.MapTerritory)
                .Where(x => x.Name == "Antarctica")
                .FirstOrDefaultAsync();

            if(antarctica == null)
            {
                await GenerateDefaultMap();
                return;
            }

            // Regenerate everything
            if(antarctica.MapTerritory.Count != fileTerritories.Count)
            {
                a.Remove(antarctica);
                a.RemoveRange(antarctica.MapTerritory);
                await a.SaveChangesAsync();

                await GenerateDefaultMap();
                return;
            }

            foreach(var territory in ReadMapJson())
            {
                if(antarctica.MapTerritory.FirstOrDefault(x => x.TerritoryName == territory.Key) == null)
                {
                    // This territory is missing
                    throw new ArgumentException($"`{territory.Key}` territory is missing from the database. Please notify an administrator.");
                }
            }
        }

        async Task<bool> areTheyBorders(string territoryName, string territoryName2)
        {
            using var a = contextFactory.CreateDbContext();

            var bothTerritories = await a.MapTerritory
                .Where(x => x.TerritoryName == territoryName || x.TerritoryName == territoryName2).ToListAsync();

            if (bothTerritories.Count < 2) throw new ArgumentException("There was 1 or more territory name which didn't exist in our db.");
            if (bothTerritories.Count > 2) throw new ArgumentException("There was 1 or more territory name which was duplicated in our db.");

            return await areTheyBorders(bothTerritories[0].Id, bothTerritories[1].Id);
        }
    }
}

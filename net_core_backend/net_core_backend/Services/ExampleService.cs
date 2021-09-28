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
            AddMapTerritoryBorders();

            // End

            var borders = await getBorders("Ronetia");


            // IsAttackPossible
            //var result = await areTheyBorders(4, 1);
            var result = await areTheyBorders("Dager", "Ronetia");

            await addBorderIfNotExistant(5, 3);



            // Do something
        }

        async Task AddMapTerritoryBorders()
        {
            var a = contextFactory.CreateDbContext();

            await a.Database.EnsureDeletedAsync();
            await a.Database.EnsureCreatedAsync();

            var firstMap = new Maps() { Name = "Antarctica map" };

            var mapTerritories = new List<MapTerritory>()
            {
                new MapTerritory() {TerritoryName = "Vibri"},
                new MapTerritory() {TerritoryName = "Ranku"},
                new MapTerritory() {TerritoryName = "Dager"},
                new MapTerritory() {TerritoryName = "Ramac"},
                new MapTerritory() {TerritoryName = "Napana"},
                new MapTerritory() {TerritoryName = "Rilanor"},
                new MapTerritory() {TerritoryName = "Tustra"},
                new MapTerritory() {TerritoryName = "Sopore"},
                new MapTerritory() {TerritoryName = "Caba"},
                new MapTerritory() {TerritoryName = "Lisu"},
                new MapTerritory() {TerritoryName = "Bavi"},
                new MapTerritory() {TerritoryName = "Kide"},
                new MapTerritory() {TerritoryName = "Wistan"},
                new MapTerritory() {TerritoryName = "Ronetia"},
                new MapTerritory() {TerritoryName = "Caydo"},
                new MapTerritory() {TerritoryName = "Prusnia"},
                new MapTerritory() {TerritoryName = "Rospa"},
                new MapTerritory() {TerritoryName = "Laly"},
                new MapTerritory() {TerritoryName = "Sona"},
                new MapTerritory() {TerritoryName = "Renyt"},
            };

            firstMap.MapTerritory = mapTerritories;
            a.Add(firstMap);
            await a.SaveChangesAsync();

            // Add borders to elements
            await addBorderIfNotExistant("Vibri", "Ranku");
            await addBorderIfNotExistant("Vibri", "Dager");
            await addBorderIfNotExistant("Vibri", "Ramac");

            await addBorderIfNotExistant("Ranku", "Vibri");
            await addBorderIfNotExistant("Ranku", "Dager");

            await addBorderIfNotExistant("Dager", "Ranku");
            await addBorderIfNotExistant("Dager", "Vibri");
            await addBorderIfNotExistant("Dager", "Ramac");
            await addBorderIfNotExistant("Dager", "Renyt");
            await addBorderIfNotExistant("Dager", "Ronetia");

            await addBorderIfNotExistant("Ramac", "Vibri");
            await addBorderIfNotExistant("Ramac", "Dager");
            await addBorderIfNotExistant("Ramac", "Renyt");
            await addBorderIfNotExistant("Ramac", "Rilanor");
            await addBorderIfNotExistant("Ramac", "Napana");

            await addBorderIfNotExistant("Napana", "Ramac");
            await addBorderIfNotExistant("Napana", "Rilanor");
            await addBorderIfNotExistant("Napana", "Tustra");

            await addBorderIfNotExistant("Tustra", "Napana");
            await addBorderIfNotExistant("Tustra", "Rilanor");
            await addBorderIfNotExistant("Tustra", "Sopore");

            await addBorderIfNotExistant("Sopore", "Tustra");
            await addBorderIfNotExistant("Sopore", "Rilanor");
            await addBorderIfNotExistant("Sopore", "Lisu");
            await addBorderIfNotExistant("Sopore", "Caydo");

            await addBorderIfNotExistant("Caydo", "Sopore");
            await addBorderIfNotExistant("Caydo", "Lisu");

            await addBorderIfNotExistant("Rilanor", "Ramac");
            await addBorderIfNotExistant("Rilanor", "Napana");
            await addBorderIfNotExistant("Rilanor", "Tustra");
            await addBorderIfNotExistant("Rilanor", "Sopore");
            await addBorderIfNotExistant("Rilanor", "Lisu");
            await addBorderIfNotExistant("Rilanor", "Renyt");

            await addBorderIfNotExistant("Lisu", "Rilanor");
            await addBorderIfNotExistant("Lisu", "Sopore");
            await addBorderIfNotExistant("Lisu", "Caydo");
            await addBorderIfNotExistant("Lisu", "Laly");
            await addBorderIfNotExistant("Lisu", "Kide");
            await addBorderIfNotExistant("Lisu", "Renyt");

            await addBorderIfNotExistant("Renyt", "Dager");
            await addBorderIfNotExistant("Renyt", "Ramac");
            await addBorderIfNotExistant("Renyt", "Rilanor");
            await addBorderIfNotExistant("Renyt", "Lisu");
            await addBorderIfNotExistant("Renyt", "Kide");
            await addBorderIfNotExistant("Renyt", "Ronetia");

            await addBorderIfNotExistant("Kide", "Renyt");
            await addBorderIfNotExistant("Kide", "Lisu");
            await addBorderIfNotExistant("Kide", "Laly");
            await addBorderIfNotExistant("Kide", "Sona");
            await addBorderIfNotExistant("Kide", "Ronetia");

            await addBorderIfNotExistant("Laly", "Lisu");
            await addBorderIfNotExistant("Laly", "Kide");
            await addBorderIfNotExistant("Laly", "Sona");
            await addBorderIfNotExistant("Laly", "Caba");

            await addBorderIfNotExistant("Caba", "Laly");
            await addBorderIfNotExistant("Caba", "Sona");
            await addBorderIfNotExistant("Caba", "Wistan");

            await addBorderIfNotExistant("Sona", "Kide");
            await addBorderIfNotExistant("Sona", "Laly");
            await addBorderIfNotExistant("Sona", "Caba");
            await addBorderIfNotExistant("Sona", "Wistan");
            await addBorderIfNotExistant("Sona", "Ronetia");

            await addBorderIfNotExistant("Ronetia", "Dager");
            await addBorderIfNotExistant("Ronetia", "Renyt");
            await addBorderIfNotExistant("Ronetia", "Kide");
            await addBorderIfNotExistant("Ronetia", "Sona");
            await addBorderIfNotExistant("Ronetia", "Wistan");
            await addBorderIfNotExistant("Ronetia", "Prusnia");

            await addBorderIfNotExistant("Prusnia", "Ronetia");
            await addBorderIfNotExistant("Prusnia", "Wistan");
            await addBorderIfNotExistant("Prusnia", "Rospa");
            await addBorderIfNotExistant("Prusnia", "Bavi");

            await addBorderIfNotExistant("Wistan", "Caba");
            await addBorderIfNotExistant("Wistan", "Sona");
            await addBorderIfNotExistant("Wistan", "Ronetia");
            await addBorderIfNotExistant("Wistan", "Prusnia");
            await addBorderIfNotExistant("Wistan", "Rospa");

            await addBorderIfNotExistant("Rospa", "Wistan");
            await addBorderIfNotExistant("Rospa", "Prusnia");
            await addBorderIfNotExistant("Rospa", "Bavi");

            await addBorderIfNotExistant("Bavi", "Rospa");
            await addBorderIfNotExistant("Bavi", "Prusnia");
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

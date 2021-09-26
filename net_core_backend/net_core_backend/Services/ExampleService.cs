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

            var a = contextFactory.CreateDbContext();
            var firstMap = new Maps() { Name = "First map" };

            var ter1 = new MapTerritory()
            {
                TerritoryName = "First territory",
            };

            var ter2 = new MapTerritory()
            {
                TerritoryName = "Second territory",
            };

            var ter3 = new MapTerritory()
            {
                TerritoryName = "Third territory",
            };

            var ter4 = new MapTerritory()
            {
                TerritoryName = "Fourth territory",
            };

            var ter5 = new MapTerritory()
            {
                TerritoryName = "Fifth territory",
            };

            firstMap.MapTerritory.Add(ter1);
            firstMap.MapTerritory.Add(ter2);
            firstMap.MapTerritory.Add(ter3);
            firstMap.MapTerritory.Add(ter4);
            firstMap.MapTerritory.Add(ter5);

            a.Add(firstMap);
            await a.SaveChangesAsync();


            // Add borders to first element

            await addBorderIfNotExistant(ter1.Id, ter2.Id);
            await addBorderIfNotExistant(ter1.Id, ter3.Id);
            await addBorderIfNotExistant(ter1.Id, ter5.Id);

            await addBorderIfNotExistant(ter2.Id, ter1.Id);
            await addBorderIfNotExistant(ter2.Id, ter3.Id);

            await addBorderIfNotExistant(ter3.Id, ter2.Id);
            await addBorderIfNotExistant(ter3.Id, ter1.Id);
            await addBorderIfNotExistant(ter3.Id, ter5.Id);
            await addBorderIfNotExistant(ter3.Id, ter4.Id);

            await addBorderIfNotExistant(ter4.Id, ter3.Id);
            await addBorderIfNotExistant(ter4.Id, ter5.Id);


            async Task<bool> addBorderIfNotExistant(int territoryId, int territoryId2)
            {
                var areBorders = await areTheyBorders(territoryId, territoryId2);

                if(!areBorders)
                {
                    a.Add(new Borders() { ThisTerritory = territoryId, NextToTerritory = territoryId2 });
                    await a.SaveChangesAsync();

                    return true;
                }

                return false;
            }


            async Task<bool> areTheyBorders(int territoryId, int territoryId2)
            {
                var borders = await a.Borders
                    .Where(x => (x.NextToTerritory == territoryId && x.ThisTerritory == territoryId2) || (x.NextToTerritory == territoryId2 && x.ThisTerritory == territoryId))
                    .ToListAsync();

                return borders.Count != 0;
            }

            async Task<bool> isAttackPossible(int territoryId, int enemyTerritoryId)
            {
                return await areTheyBorders(territoryId, enemyTerritoryId);
            }

            async Task<MapTerritory[]> getBorders(int territoryId)
            {
                var borderTerritories = await a.MapTerritory
                    .Include(x => x.BordersNextToTerritoryReference)
                    .Include(x => x.BordersThisTerritoryReference)
                    .Where(x => x.Id == territoryId) 
                    .Select(x => new
                    {
                        left = x.BordersNextToTerritoryReference
                            .Select(x => x.ThisTerritory == territoryId ? x.ThisTerritoryReference : x.NextToTerritoryReference).ToList(),
                        right = x.BordersThisTerritoryReference
                            .Select(x => x.ThisTerritory == territoryId ? x.ThisTerritoryReference : x.NextToTerritoryReference).ToList()
                    })
                    .FirstOrDefaultAsync();

                return borderTerritories.left.Concat(borderTerritories.right).ToArray();
            }

            var borders = await getBorders(1);


            var result = await isAttackPossible(2, 5);

            await addBorderIfNotExistant(1, 3);



            // Do something
        }
    }
}

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

            firstMap.MapTerritory.Add(ter1);
            firstMap.MapTerritory.Add(ter2);
            firstMap.MapTerritory.Add(ter3);
            firstMap.MapTerritory.Add(ter4);

            a.Add(firstMap);
            await a.SaveChangesAsync();


            // Add borders to first element

            a.Add(new Borders() { BordersTer = ter2.Id, ThisTer = ter1.Id });
            a.Add(new Borders() { BordersTer = ter4.Id, ThisTer = ter1.Id });
            a.Add(new Borders() { BordersTer = ter3.Id, ThisTer = ter2.Id });
            a.Add(new Borders() { BordersTer = ter3.Id, ThisTer = ter4.Id });

            await a.SaveChangesAsync();

            // Get third territory with all it's border

            var thirdTerritory = await
                a.Maps
                .Include(x => x.MapTerritory)
                .ThenInclude(x => x.BordersBordersTerNavigation)
                .ThenInclude(x => x.BordersTerNavigation)
                .SelectMany(x => x.MapTerritory)
                .FirstOrDefaultAsync(x => x.TerritoryName == "Third territory");


            var thirdTerritoryBorders = thirdTerritory
                .BordersBordersTerNavigation
                .Concat(thirdTerritory.BordersThisTerNavigation)
                .ToList();


            async Task<bool> addBorderIfNotExistant(int territoryId, int territoryId2)
            {
                var areBorders = await areTheyBorders(territoryId, territoryId2);

                if(!areBorders)
                {
                    a.Add(new Borders() { ThisTer = territoryId, BordersTer = territoryId2 });
                    await a.SaveChangesAsync();

                    return true;
                }

                return false;
            }


            async Task<bool> areTheyBorders(int territoryId, int territoryId2)
            {
                var borders = await a.Borders
                    .Where(x => (x.BordersTer == territoryId && x.ThisTer == territoryId2) || (x.BordersTer == territoryId2 && x.ThisTer == territoryId))
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
                    .Include(x => x.BordersBordersTerNavigation)
                    .Include(x => x.BordersThisTerNavigation)
                    .Where(x => x.Id == territoryId) 
                    .Select(x => new
                    {
                        left = x.BordersBordersTerNavigation
                            .Select(x => x.ThisTer == territoryId ? x.BordersTerNavigation : x.ThisTerNavigation).ToList(),
                        right = x.BordersThisTerNavigation
                            .Select(x => x.ThisTer == territoryId ? x.BordersTerNavigation : x.ThisTerNavigation).ToList()
                    })
                    .FirstOrDefaultAsync();

                return borderTerritories.left.Concat(borderTerritories.right).ToArray();
            }

            await getBorders(2);


            var result = await isAttackPossible(4, 1);

            await addBorderIfNotExistant(1, 3);



            // Do something
        }
    }
}

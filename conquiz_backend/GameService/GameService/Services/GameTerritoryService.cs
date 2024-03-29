﻿using GameService.Context;
using GameService.Data;
using GameService.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services
{
    public interface IGameTerritoryService
    {
        string[] GetAvailableAttackTerritoriesNames(GameInstance gm, int userId, int gameInstanceId, bool isNeutral);
        ObjectTerritory GetRandomTerritory(GameInstance gm, int userId, int gameInstanceId, bool takenByCheck = true);
        ObjectTerritory SelectTerritoryAvailability(GameInstance gm, int userId, int gameInstanceId, int selectedMapTerritoryId, bool isNeutral);
    }

    public class GameTerritoryService : IGameTerritoryService
    {
        private readonly Random r = new Random();
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IMapGeneratorService mapGeneratorService;

        public GameTerritoryService(IDbContextFactory<DefaultContext> _contextFactory, IMapGeneratorService mapGeneratorService)
        {
            contextFactory = _contextFactory;
            this.mapGeneratorService = mapGeneratorService;
        }

        private class UserBorderInformation
        {
            /// <summary>
            /// All current instance territories the user controls
            /// </summary>
            public List<ObjectTerritory> UserTerritories { get; set; }

            /// <summary>
            /// All untaken Territories on the map<br/>
            /// Untaken can be considered a territory which is neutral OR owned by someone (but isn't actively being attacked)
            /// </summary>
            public List<ObjectTerritory> UntakenBorders { get; set; }
            /// <summary>
            /// All untaken borders that are next to a user's border
            /// </summary>
            public List<ObjectTerritory> MatchingBorders { get; set; }
            /// <summary>
            /// All player map territory borders
            /// </summary>
            public MapTerritory[] AllPlayerBorders { get; set; }
        }

        private UserBorderInformation GetBorderInformation(GameInstance gm, int userId, int gameInstanceId, bool takenByCheck = true)
        {
            var userTerritories = gm.ObjectTerritory.Where(x => x.TakenBy == userId);


            // There is a possibility where people get blocked off
            // So in case all takenterritories don't border anything, you need a random select
            var allPlayerBorders = mapGeneratorService.GetBorders(userTerritories.Select(x => x.MapTerritoryId).ToArray());

            List<ObjectTerritory> untakenBorder;

            // Takes into account takenBy to ensure that the territory doesnt belong to anyone
            if (takenByCheck)
            {
                untakenBorder = gm.ObjectTerritory
                    .Where(x => x.TakenBy == null && x.AttackedBy == null)
                    .ToList();
            }
            else
            {
                untakenBorder = gm.ObjectTerritory
                    .Where(x => x.AttackedBy == null)
                    .ToList();
            }

            untakenBorder = untakenBorder.Except(userTerritories).ToList();
            var matchingBorders = untakenBorder.Where(x => allPlayerBorders.Any(y => x.MapTerritoryId == y.Id)).ToList();
            return new UserBorderInformation()
            {
                UserTerritories = userTerritories.ToList(),
                AllPlayerBorders = allPlayerBorders,
                UntakenBorders = untakenBorder,
                MatchingBorders = matchingBorders
            };
        }

        /// <summary>
        /// Gets all available to attack territories
        /// </summary>
        /// <param name="db"></param>
        /// <param name="userId"></param>
        /// <param name="gameInstanceId"></param>
        /// <returns></returns>
        public string[] GetAvailableAttackTerritoriesNames(GameInstance gm, int userId, int gameInstanceId, bool isNeutral)
        {
            var userBorderInfo = GetBorderInformation(gm, userId, gameInstanceId, isNeutral);


            // If matching borders are available return them
            if (userBorderInfo.MatchingBorders.Count > 0)
            {
                return userBorderInfo.MatchingBorders.Select(x => x.MapTerritory.TerritoryName).ToArray();
            }

            return userBorderInfo.UntakenBorders.Select(x => x.MapTerritory.TerritoryName).ToArray();


            // If there aren't any matching borders 
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
        public ObjectTerritory SelectTerritoryAvailability(GameInstance gm, int userId, int gameInstanceId, int selectedMapTerritoryId, bool isNeutral)
        {
            var userBorderInfo = GetBorderInformation(gm, userId, gameInstanceId, isNeutral);

            var selectedTerritory = userBorderInfo
                .MatchingBorders
                .FirstOrDefault(x => x.MapTerritoryId == selectedMapTerritoryId);


            // The selected map territory isn't a border with the user's current territories
            if (selectedTerritory == null)
            {
                // Untaken borders are all UNTAKEN or UNATTACKED territories on the map
                if (userBorderInfo.MatchingBorders.Count > 0)
                {
                    // There are available borders next to the user taken territories, but the selected one 
                    // Isn't one of them
                    throw new BorderSelectedGameException("Attack all neightbour territories first");
                }
                else
                {
                    // If there arent any available matching borders and if the selected one is
                    // Available (not attacked)
                    var notConnectedSelectedTerritory = userBorderInfo.UntakenBorders
                        .FirstOrDefault(x => x.MapTerritoryId == selectedMapTerritoryId);

                    return notConnectedSelectedTerritory;
                }
            }
            // The selected map territory is a border with the user's current territories
            else
            {
                // The selected territory is untaken
                return selectedTerritory;
            }
        }

        public ObjectTerritory GetRandomTerritory(GameInstance gm, int userId, int gameInstanceId, bool takenByCheck = true)
        {
            var userBorderInfo = GetBorderInformation(gm, userId, gameInstanceId, takenByCheck);

            if (userBorderInfo.MatchingBorders.Count > 0)
            {
                return userBorderInfo.MatchingBorders[r.Next(0, userBorderInfo.MatchingBorders.Count)];
            }
            else
            {
                return userBorderInfo.UntakenBorders[r.Next(0, userBorderInfo.UntakenBorders.Count)];
            }
        }
    }
}

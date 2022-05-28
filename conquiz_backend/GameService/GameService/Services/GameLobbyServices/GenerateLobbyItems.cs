using GameService.Data;
using GameService.Data.Models;
using GameService.Services.GameTimerServices;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.GameLobbyServices
{
    public partial class GameLobbyService : IGameLobbyService
    {
        private async Task<GameInstance> CreateGameInstance(DefaultContext db, GameType gameType, Users user)
        {
            var map = await db.Maps.Where(x => x.Name == DefaultMap).FirstOrDefaultAsync();

            if (map == null)
            {
                await mapGeneratorService.ValidateMap(db);
            }

            var invitationLink = GenerateInvCode();

            while (await db.GameInstance
                .Where(x => x.InvitationLink == invitationLink &&
                    (x.GameState == GameState.IN_LOBBY || x.GameState == GameState.IN_PROGRESS))
                .FirstOrDefaultAsync() != null)
            {
                invitationLink = GenerateInvCode();
            }

            var gameInstance = new GameInstance()
            {
                GameGlobalIdentifier = Guid.NewGuid().ToString(),
                GameType = gameType,
                GameState = GameState.IN_LOBBY,
                InvitationLink = invitationLink,
                GameCreatorId = user.Id,
                Map = map,
                StartTime = DateTime.Now,
                QuestionTimerSeconds = 30,
            };

            var newParticipant = await GenerateParticipant(db, null, user.Id);

            newParticipant.Player = user;
            gameInstance.Participants.Add(newParticipant);

            return gameInstance;
        }

        private string GenerateInvCode()
        {
            var invitationLink = "";
            for (var i = 0; i < InvitationCodeLength; i++)
            {
                invitationLink += r.Next(0, 9).ToString();
            }
            return invitationLink;
        }

        private async Task<bool> CanPersonJoin(int userId)
        {
            using var db = contextFactory.CreateDbContext();

            var userGames = await db.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Include(e => e.Participants)
                .ThenInclude(e => e.GameCharacter)
                .ThenInclude(e => e.Character)
                .Where(x => (x.GameState == GameState.IN_LOBBY || x.GameState == GameState.IN_PROGRESS) && x.Participants
                    .Any(y => y.PlayerId == userId))
                .ToListAsync();


            // IF THERE ARE ANY IN_PROGRESS INSTANCES WITH THIS PLAYER PREVENT LOBBY CREATION
            if (userGames.Any(x => x.GameState == GameState.IN_PROGRESS))
            {
                throw new JoiningGameException("You already have a game in progress. It has to finish first.");
            }

            var lobbyGames = userGames.Where(x => x.GameState == GameState.IN_LOBBY).ToList();

            // You shouldn't be able to participate in more than 1 lobby game open.
            // It happend because some error. Close all game lobbies.
            if (lobbyGames.Count > 1)
            {
                lobbyGames.ForEach(x => x.GameState = GameState.CANCELED);

                db.UpdateRange(lobbyGames);
                await db.SaveChangesAsync();
                throw new JoiningGameException("Oops. There was an internal server error. Please, start a new game lobby");
            }


            // User participates already in an open lobby.
            // Redirect him to this instead of creating a new instance.
            if (lobbyGames.Count == 1)
            {
                throw new ExistingLobbyGameException(lobbyGames[0], 
                    lobbyGames[0].Participants.Where(e => e.PlayerId == userId).Select(e => e.GameCharacter).FirstOrDefault(), 
                    "User participates already in an open lobby");
            }

            return true;
        }



        private async Task<Round[]> CreateNeutralAttackRounding(DefaultContext context, int mapId, List<Participants> allPlayers, int gameInstanceId)
        {
            var totalTerritories = await mapGeneratorService.GetAmountOfTerritories(context, mapId);

            var order = CommonTimerFunc.GenerateAttackOrder(allPlayers.Select(x => x.PlayerId).ToList(), totalTerritories, RequiredPlayers);

            // Create default rounds
            var finalRounds = new List<Round>();

            // Stores game round number for each round
            var gameRoundNumber = 1;

            // Full rounds
            for (var i = 0; i < order.UserRoundAttackOrders.Count(); i++)
            {
                var baseRound = new Round
                {
                    GameRoundNumber = gameRoundNumber++,
                    AttackStage = AttackStage.MULTIPLE_NEUTRAL,
                    Description = $"MultipleChoice question. Attacker vs NEUTRAL territory",
                    IsQuestionVotingOpen = false,
                    IsTerritoryVotingOpen = false,
                };

                baseRound.NeutralRound = new NeutralRound()
                {
                    AttackOrderNumber = 1,
                };

                var attackOrderNumber = 1;
                foreach (var roundAttackerId in order.UserRoundAttackOrders[i])
                {
                    baseRound.NeutralRound.TerritoryAttackers.Add(new AttackingNeutralTerritory()
                    {
                        AttackerId = roundAttackerId,
                        AttackOrderNumber = attackOrderNumber++,
                    });
                }

                finalRounds.Add(baseRound);
            }

            var result = finalRounds.ToArray();

            return result;
        }


        private static async Task<ObjectTerritory[]> CreateGameTerritories(DefaultContext a, int mapId, int gameInstanceId)
        {
            var originTerritories = await a.MapTerritory.Where(x => x.MapId == mapId).ToListAsync();

            var gameTer = new List<ObjectTerritory>();
            foreach (var ter in originTerritories)
            {
                gameTer.Add(new ObjectTerritory()
                {
                    TerritoryScore = DefaultTerritoryScore,
                    MapTerritoryId = ter.Id,
                    GameInstanceId = gameInstanceId,
                });
            }
            return gameTer.ToArray();
        }

        private async Task<ObjectTerritory[]> ChooseCapitals(DefaultContext a, ObjectTerritory[] allTerritories, Participants[] participants)
        {
            var capitals = new List<ObjectTerritory>();

            // Get 3 capitals
            while (capitals.Count() < RequiredPlayers)
            {
                var randomTerritory = allTerritories[r.Next(0, allTerritories.Count())];

                // Capital already here
                if (capitals.Contains(randomTerritory)) continue;

                var borders = await mapGeneratorService.GetBorders(a, randomTerritory.MapTerritoryId);

                var borderWithOtherCapitals = capitals.Where(x => borders.Any(y => y.Id == x.MapTerritoryId)).FirstOrDefault();

                if (borderWithOtherCapitals != null) continue;

                randomTerritory.TerritoryScore = DefaultCapitalScore;
                capitals.Add(randomTerritory);
            }

            // Give each capital to a random player
            while (capitals.Where(x => x.TakenBy != null).ToList().Count() < RequiredPlayers)
            {
                var randomPlayer = participants[r.Next(0, RequiredPlayers)];
                if (capitals.Any(x => x.TakenBy == randomPlayer.PlayerId)) continue;

                var chosen = capitals.First(x => x.TakenBy == null);
                chosen.TakenBy = randomPlayer.PlayerId;
                chosen.IsCapital = true;
            }

            // TEST IF ALLTERRITORIES CHANGES OR NOT BASED ON REFERENCE !!!!

            return allTerritories;
        }


        private int GetRandomNumber(Participants[] participants)
        {
            var allNumbers = new int[3] { 1, 2, 3 };

            int selectedNumber = 0;

            while (selectedNumber == 0)
            {
                var randomNumber = allNumbers[r.Next(0, allNumbers.Length)];

                var duplicate = participants
                    ?.FirstOrDefault(e => e.InGameParticipantNumber == randomNumber);

                if (duplicate != null)
                    continue;

                selectedNumber = randomNumber;
            }

            return selectedNumber;
        }

        private async Task<Participants> GenerateParticipant(DefaultContext db, Participants[] participants, int userId)
        {
            var freeGameCharacters = await db.Characters.Where(e => e.PricingType == CharacterPricingType.FREE).ToArrayAsync();


            // This is the first player, he gets automatically selected character
            var randomCharacter = GetRandomCharacter(participants, freeGameCharacters);

            var randomInGameNumber = GetRandomNumber(participants);

            var selectedGameCharacter = new GameCharacter(randomCharacter);

            return new Participants(selectedGameCharacter, userId, randomInGameNumber);
        }

        private Character GetRandomCharacter(Participants[] participants, Character[] characters)
        {
            if (characters.Length < 3)
                throw new ArgumentException("There aren't enough playable characters for every player. Contact an administrator.");

            Character selectedCharacter = null;

            while (selectedCharacter == null)
            {
                var randomCharacter = characters[r.Next(0, characters.Length)];

                var duplicate = participants
                    ?.FirstOrDefault(e => e.GameCharacter?.GetCharacterType == randomCharacter.CharacterType);

                if (duplicate != null)
                    continue;

                selectedCharacter = randomCharacter;
            }

            return selectedCharacter;

        }


        /// <summary>
        /// Creates a new User with a "Bot" status
        /// </summary>
        /// <returns></returns>
        public async Task<Users> CreateGameBot(DefaultContext db)
        {
            var botUsername = $"[BOT]Penguin-{r.Next(0, 10000)}";
            var botUser = new Users()
            {
                IsBot = true,
                Username = botUsername,
            };

            await db.AddAsync(botUser);

            return botUser;
        }

        public async Task<Users> GetRandomGameBot(DefaultContext db)
        {
            var availableBot = await db.Users.FirstOrDefaultAsync(e => e.IsBot &&
                (e.Participants == null || e.Participants.All(y => y.Game.GameState == GameState.CANCELED || y.Game.GameState == GameState.FINISHED)));

            if (availableBot is null)
                return await CreateGameBot(db);

            return availableBot;
        }
    }
}

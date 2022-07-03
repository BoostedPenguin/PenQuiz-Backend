using GameService.Data;
using GameService.Data.Models;
using GameService.Dtos.SignalR_Responses;
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

        private async Task<CharacterResponse[]> GetAllCharacters(DefaultContext context)
        {
            return mapper.Map<CharacterResponse[]>(await context.Characters.Include(e => e.BelongToUsers).ToArrayAsync());
        }

        private async Task<CharacterResponse[]> GetThisUserAvailableCharacters(DefaultContext context, int userId)
        {
            var player = await context.Users.Include(e => e.OwnedCharacters).FirstOrDefaultAsync(e => e.Id == userId);

            // All free characters
            var characters = await context.Characters.Where(e => e.PricingType == CharacterPricingType.FREE).ToListAsync();

            var ownedFreeCharacters = mapper.Map<List<CharacterResponse>>(characters);


            // Premium owned characters
            var ownedCharacters = player.OwnedCharacters;

            var ownedCharacterRes = mapper.Map<List<CharacterResponse>>(ownedCharacters);
            
            ownedCharacterRes.AddRange(ownedFreeCharacters);

            return ownedCharacterRes.ToArray();
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
                var availableCharacters = await GetAllCharacters(db);
                throw new ExistingLobbyGameException(lobbyGames[0],
                    "User participates already in an open lobby",
                    availableCharacters
                    );
            }

            return true;
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
            //var randomCharacter = GetRandomCharacter(participants, freeGameCharacters);

            var randomInGameNumber = GetRandomNumber(participants);

            return new Participants(userId, randomInGameNumber);
        }

        private Character GetRandomCharacter(Participants[] participants, Character[] characters)
        {
            if (characters.Length < 3)
                throw new ArgumentException("There aren't enough playable characters for every player. Contact an administrator.");

            Character selectedCharacter = null;

            // DEBUG PURPOSES ONLY, THIS ASSIGNS THE CREATOR A SPECIFIC CHARACTER
            if(participants == null || participants.Length == 0)
            {
                return characters.First(e => e.CharacterType == CharacterType.SCIENTIST);
            }

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

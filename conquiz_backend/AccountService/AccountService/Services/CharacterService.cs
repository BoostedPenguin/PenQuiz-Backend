using AccountService.Data;
using AccountService.Data.Models;
using AccountService.Services.Extensions;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AccountService.Services
{
    public interface ICharacterService
    {
        Task<Character[]> GetOwnCharacters();
        Task GiftCharacter(string userGlobalId, string characterGlobalId);
    }

    public class CharacterService : ICharacterService
    {
        private readonly IDbContextFactory<AppDbContext> contextFactory;
        private readonly IHttpContextAccessor httpContext;
        private readonly IMapper mapper;

        public CharacterService(IDbContextFactory<AppDbContext> contextFactory, IHttpContextAccessor httpContext, IMapper mapper)
        {
            this.contextFactory = contextFactory;
            this.httpContext = httpContext;
            this.mapper = mapper;
        }

        public async Task<Character[]> GetOwnCharacters()
        {
            using var db = contextFactory.CreateDbContext();

            var globalId = httpContext.GetCurrentUserGlobalId();

            var ownedCharacters = await db.Users
                .Where(e => e.UserGlobalIdentifier == globalId)
                .Select(e => e.OwnedCharacters)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return ownedCharacters.ToArray();
        }

        public async Task GiftCharacter(string userGlobalId, string characterGlobalId)
        {
            using var db = contextFactory.CreateDbContext();

            var character = await db.Characters.FirstOrDefaultAsync(e => e.CharacterGlobalIdentifier == characterGlobalId);

            var user = await db.Users.Include(e => e.OwnedCharacters).FirstOrDefaultAsync(e => e.UserGlobalIdentifier == userGlobalId);

            var existingCharacter = user.OwnedCharacters.FirstOrDefault(e => e.CharacterGlobalIdentifier == characterGlobalId);

            if (existingCharacter != null)
                throw new ArgumentException($"This person already has the character of type {existingCharacter.CharacterType}");

            user.OwnedCharacters.Add(character);

            db.Update(user);
            await db.SaveChangesAsync();
        }
    }
}

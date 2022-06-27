using AutoMapper;
using GameService.Data;
using GameService.Dtos.SignalR_Responses;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace GameService.Services.REST_Services
{
    public interface ICharacterService
    {
        Task<CharacterResponse[]> GetAllCharacters();
        Task<CharacterResponse> GetCharacter(string globalId);
    }

    public class CharacterService : ICharacterService
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IMapper mapper;

        public CharacterService(IDbContextFactory<DefaultContext> contextFactory, IMapper mapper)
        {
            this.contextFactory = contextFactory;
            this.mapper = mapper;
        }

        public async Task<CharacterResponse[]> GetAllCharacters()
        {
            using var db = contextFactory.CreateDbContext();

            var allCharacters = await db.Characters.ToArrayAsync();

            var charactersRes = mapper.Map<CharacterResponse[]>(allCharacters);

            return charactersRes;
        }

        public async Task<CharacterResponse> GetCharacter(string globalId)
        {
            using var db = contextFactory.CreateDbContext();

            var character = 
                await db.Characters.FirstOrDefaultAsync(e => e.CharacterGlobalIdentifier == globalId);

            var characterRes = mapper.Map<CharacterResponse>(character);

            return characterRes;
        }
    }
}

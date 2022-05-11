using GameService.Data;
using GameService.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.CharacterActions
{
    public class CharacterValidation
    {
        public static async Task ValidateCharacters(DefaultContext db)
        {
            var allCharacter = Enum.GetValues(typeof(CharacterType)).Cast<CharacterType>();

            var allCharactersInDb = await db.Characters
                .Where(e => allCharacter.Contains(e.CharacterType))
                .AsNoTracking()
                .ToListAsync();

            var charactersNotInDb = allCharacter
                .Except(allCharactersInDb.Select(e => e.CharacterType)).ToList();

            // If all characters exist in the db exit
            if (charactersNotInDb.Count == 0)
                return;

            foreach(var cNotInDb in charactersNotInDb)
            {
                var g = cNotInDb switch
                {
                    CharacterType.KING => GenerateKing(),
                    CharacterType.WIZARD => GenerateWizard(),
                    CharacterType.VIKING => GenerateViking(),
                    _ => throw new ArgumentException("The missing character ENUM does not have a dedicated character template"),
                };

                await db.AddAsync(g);
            }

            await db.SaveChangesAsync();
        }

        private static Character GenerateViking()
        {
            return new Character()
            {
                Name = "Viking",
                Description = "Some description",
                PricingType = CharacterPricingType.FREE,
                CharacterGlobalIdentifier = Guid.NewGuid().ToString(),
                AbilityDescription = $"Can fortify his capital against attacks, increasing the amount of required consecutive wins for the enemy. Amount of times he can fortify his capital",
                CharacterType = CharacterType.VIKING,
                AvatarName = "penguinAvatarViking",
            };
        }

        private static Character GenerateKing()
        {
            return new Character()
            {
                Name = "King",
                Description = "Some description",
                PricingType = CharacterPricingType.FREE,
                CharacterGlobalIdentifier = Guid.NewGuid().ToString(),
                AbilityDescription = $"Has a permanent score bonus multiplier when you capture a territory. Multiplier: {CharacterAbilitiesGlobalValues.KingCharacterPointsMultiplier * 100}%",
                CharacterType = CharacterType.KING,
                AvatarName = "penguinAvatarKing",
            };
        }

        private static Character GenerateWizard()
        {
            return new Character()
            {
                Name = "Wizard",
                Description = "Some description",
                PricingType = CharacterPricingType.FREE,
                CharacterGlobalIdentifier = Guid.NewGuid().ToString(),
                AbilityDescription = $"Can remove half the options to select from in a multiple choice question. Ability can be used: {CharacterAbilitiesGlobalValues.WizardCharacterMCQuestionHintMaxUseCount}",
                AvatarName = "penguinAvatarWizard",
                CharacterType = CharacterType.WIZARD,
            };
        }
    }
}

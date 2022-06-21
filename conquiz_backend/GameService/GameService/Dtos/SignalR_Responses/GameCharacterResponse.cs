using GameService.Data.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameService.Dtos.SignalR_Responses
{
    public class GameCharacterResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        // Relates to the Game Service inner user id, not global
        public int GameInstanceId { get; set; }

        public GameCharacterAbilitiesResponse CharacterAbilities { get; set; }

        /// <summary>
        /// Identify the person who chose this character for this game
        /// </summary>
        public virtual CharacterResponse Character { get; set; }
    }

    public class CharacterResponse
    {
        public int Id { get; set; }
        public string CharacterGlobalIdentifier { get; set; }
        public string Name { get; set; }
        public string AvatarName { get; set; }
        public string Description { get; set; }
        public string AbilityDescription { get; set; }
        public CharacterPricingType PricingType { get; set; }
        public CharacterType CharacterType { get; set; }
        public double? Price { get; set; }
    }

    public class GameCharacterAbilitiesResponse
    {
        public CharacterType CharacterType { get; set; }

        public int GameCharacterId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual VikingCharacterAbilitiesResponse VikingCharacterAbilitiesResponse { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual KingCharacterAbilitiesResponse KingCharacterAbilitiesResponse { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual WizardCharacterAbilitiesResponse WizardCharacterAbilitiesResponse { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual ScientistCharacterAbilitiesResponse ScientistCharacterAbilitiesResponse { get; set; }
    }

    public class VikingCharacterAbilitiesResponse
    {
        public int FortifyCapitalUseCount { get; set; }
        public List<int> AbilityUsedInRounds { get; set; }
        public int FortifyCapitalMaxUseCount { get; set; } = CharacterAbilitiesGlobalValues.VikingCharacterFortifyCapitalMaxUseCount;
    }

    public class KingCharacterAbilitiesResponse
    {
        public double CurrentBonusPoints { get; set; }
        public double PointsMultiplier { get; set; } = CharacterAbilitiesGlobalValues.KingCharacterPointsMultiplier;
    }

    public class WizardCharacterAbilitiesResponse
    {
        public int MCQuestionHintUseCount { get; set; }
        public List<int> AbilityUsedInRounds { get; set; }
        public int MCQuestionHintMaxUseCount { get; set; } = CharacterAbilitiesGlobalValues.WizardCharacterMCQuestionHintMaxUseCount;
    }

    public class ScientistCharacterAbilitiesResponse
    {
        public int NumberQuestionHintUseCount { get; set; }
        public List<int> AbilityUsedInRounds { get; set; }
        public int NumberQuestionHintMaxUseCount { get; set; } = CharacterAbilitiesGlobalValues.ScientistCharacterNumberQuestionHintMaxUseCount;
    }
}

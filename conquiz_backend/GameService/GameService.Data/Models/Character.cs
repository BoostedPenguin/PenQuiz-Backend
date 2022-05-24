﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameService.Data.Models
{
    public static class CharacterAbilitiesGlobalValues
    {
        public const double KingCharacterPointsMultiplier = 0.1;
        public const int WizardCharacterMCQuestionHintMaxUseCount = 3;
        public const int VikingCharacterFortifyCapitalMaxUseCount = 2;
    }
    public enum CharacterPricingType
    {
        FREE,
        PREMIUM
    }

    public enum CharacterType
    {
        WIZARD,
        KING,
        VIKING
    }

    /// <summary>
    /// Character primary data
    /// Serves as a read-only solution for in-game logic
    /// Regarding instances of character data, use CharacterAbilities
    /// </summary>
    public class Character
    {
        public Character()
        {
            GameCharacters = new HashSet<GameCharacter>();
        }

        public int Id { get; set; }
        public string CharacterGlobalIdentifier { get; set; }
        public string Name { get; set; }
        public string AvatarName { get; set; }
        public string Description { get; set; }
        public string AbilityDescription { get; set; }
        public CharacterPricingType PricingType { get; set; }
        public CharacterType CharacterType { get; set; }
        public double? Price { get; set; }

        public ICollection<GameCharacter> GameCharacters { get; set; }
    }

    /// <summary>
    /// In-game instance blueprint of character abilities
    /// Used to track ability use count and per-character logic
    /// </summary>
    public abstract class GameCharacterAbilities
    {
        public GameCharacterAbilities()
        {

        }
        public int Id { get; set; }
        public CharacterType CharacterType { get; set; }

        [ForeignKey("GameCharacter")]
        public int GameCharacterId { get; set; }
        public virtual GameCharacter GameCharacter { get; set; }
    }

    public class VikingCharacterAbilities : GameCharacterAbilities
    {
        public int FortifyCapitalUseCount { get; set; }

        [NotMapped]
        public int FortifyCapitalMaxUseCount { get; set; } = CharacterAbilitiesGlobalValues.VikingCharacterFortifyCapitalMaxUseCount;
    }

    public class KingCharacterAbilities : GameCharacterAbilities
    {
        public double CurrentBonusPoints { get; set; }
        [NotMapped]
        public double PointsMultiplier { get; set; } = CharacterAbilitiesGlobalValues.KingCharacterPointsMultiplier;
    }

    public class WizardCharacterAbilities : GameCharacterAbilities
    {
        public bool IsMCHintsAvailable => MCQuestionHintUseCount < MCQuestionHintMaxUseCount;

        public int MCQuestionHintUseCount { get; set; }

        [NotMapped]
        public int MCQuestionHintMaxUseCount { get; set; } = CharacterAbilitiesGlobalValues.WizardCharacterMCQuestionHintMaxUseCount;
    }
}
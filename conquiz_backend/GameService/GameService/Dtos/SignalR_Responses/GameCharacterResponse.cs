using GameService.Data.Models;
using System.Text.Json.Serialization;

namespace GameService.Dtos.SignalR_Responses
{
    public class GameCharacterResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        // Relates to the Game Service inner user id, not global
        public int GameInstanceId { get; set; }

        public GameCharacterAbilities CharacterAbilities { get; set; }

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

}

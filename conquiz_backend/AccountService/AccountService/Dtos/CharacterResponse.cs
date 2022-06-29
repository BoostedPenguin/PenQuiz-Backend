using AccountService.Data.Models;

namespace AccountService.Dtos
{
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

        public string Event { get; set; }
    }
}

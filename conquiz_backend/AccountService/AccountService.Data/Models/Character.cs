using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountService.Data.Models
{
    public enum CharacterPricingType
    {
        FREE,
        PREMIUM
    }

    public enum CharacterType
    {
        WIZARD,
        KING,
        VIKING,
        SCIENTIST,
    }

    public class Character
    {
        public int Id { get; set; }
        public string CharacterGlobalIdentifier { get; set; }
        public string Name { get; set; }
        public CharacterPricingType PricingType { get; set; }
        public CharacterType CharacterType { get; set; }
        public double? Price { get; set; }
    }
}

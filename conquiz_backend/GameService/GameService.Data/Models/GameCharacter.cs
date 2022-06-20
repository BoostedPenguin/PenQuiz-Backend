using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameService.Data.Models
{
    /// <summary>
    /// Serves as an instance of a character during a game
    /// </summary>
    public class GameCharacter
    {
        public GameCharacter()
        {

        }
        public GameCharacter(Character character)
        {
            this.Character = character;

            switch (character.CharacterType)
            {
                case CharacterType.WIZARD:
                    this.CharacterAbilities = new WizardCharacterAbilities()
                    {
                        CharacterType = CharacterType.WIZARD,
                    };
                    break;
                case CharacterType.KING:
                    this.CharacterAbilities = new KingCharacterAbilities()
                    {
                        CharacterType = CharacterType.KING,
                    };
                    break;
                case CharacterType.VIKING:
                    this.CharacterAbilities = new VikingCharacterAbilities()
                    {
                        CharacterType = CharacterType.VIKING,
                    };
                    break;
            }
        }
        public int Id { get; set; }
        public int ParticipantId { get; set; }
        public int CharacterId { get; set; }
        public GameCharacterAbilities CharacterAbilities { get; set; }

        public CharacterType GetCharacterType => Character.CharacterType;

        /// <summary>
        /// Identify the person who chose this character for this game
        /// </summary>
        public virtual Participants Participant { get; set; }
        public virtual Character Character { get; set; }
    }
}

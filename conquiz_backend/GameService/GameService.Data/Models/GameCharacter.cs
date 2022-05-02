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
        public GameCharacter(Character character)
        {
            this.Character = character;

            switch (character.CharacterType)
            {
                case CharacterType.WIZARD:
                    this.CharacterAbilities = new WizardCharacterAbilities();
                    break;
                case CharacterType.KING:
                    this.CharacterAbilities = new KingCharacterAbilities();
                    break;
                case CharacterType.VIKING:
                    this.CharacterAbilities = new VikingCharacterAbilities();
                    break;
            }
        }

        public int ParticipantId { get; set; }
        public int CharacterId { get; set; }
        public GameCharacterAbilities CharacterAbilities { get; set; }

        public CharacterType GetCharacterType => Character.CharacterType;

        #region Sample Data

        public void OnCharacterSelected()
        {
            // When assigning a participant to a game, give him the appropriate character type
            // In this case give him wizard

            var gm = new GameInstance();

            var wizard = new Character()
            {
                Id = 5,
                AbilityDescription = "smth",
                CharacterType = CharacterType.WIZARD
            };

            //

            var particip = new Participants()
            {
                PlayerId = 5,
                Score = 0,
                GameCharacter = new GameCharacter(wizard)
            };




            gm.Participants.Add(particip);
        }

        public void Sss()
        {
            void DoWizardStuff(WizardCharacterAbilities wizard)
            {

            }
            void DoKingStuff(KingCharacterAbilities king)
            {

            }

            // Presume that all 3 characters have an effect triggered on this round
            // Optimal way to trigger all 3 of the changes
            // Just sample data, no need to create obj from all
            var gm = new GameInstance();


            foreach(var part in gm.Participants)
            {
                var characterAbilities = part.GameCharacter.CharacterAbilities;
                switch (part.GameCharacter.GetCharacterType)
                {
                    case CharacterType.WIZARD:
                        DoWizardStuff(characterAbilities as WizardCharacterAbilities);
                        break;
                    case CharacterType.KING:
                        DoKingStuff(characterAbilities as KingCharacterAbilities);
                        break;
                    case CharacterType.VIKING:
                        return;
                }
            }
        }

        public void OnShowMc()
        {
            // Check how easy or not it will be to give a wizard his hint
            // Just sample data, no need to create obj from all
            var gm = new GameInstance();


            if (gm.Participants
                .FirstOrDefault(e => e.GameCharacter.GetCharacterType == CharacterType.WIZARD)
                ?.GameCharacter
                ?.CharacterAbilities is not WizardCharacterAbilities wizard) return;

            if (wizard.MCQuestionHintUseCount < wizard.MCQuestionHintMaxUseCount)
            {
                // Can do smth cuz he wizard bby
            }



            foreach (var particip in gm.Participants)
            {
                if(particip.GameCharacter.GetCharacterType == CharacterType.WIZARD)
                {
                    // This particip is a wizard
                    var participantWizardAbilties = particip.GameCharacter.CharacterAbilities as WizardCharacterAbilities;
                    if(participantWizardAbilties.MCQuestionHintUseCount < participantWizardAbilties.MCQuestionHintMaxUseCount)
                    {
                        // Can do smth cuz he wizard bby
                    }
                }
            }
        }

        public void GetCharacterAbilities()
        {
            switch (Character.CharacterType)
            {
                case CharacterType.WIZARD:
                    break;
                case CharacterType.KING:
                    var i = CharacterAbilities as KingCharacterAbilities;
                    break;
                case CharacterType.VIKING:
                    break;
            }
        }

        #endregion


        /// <summary>
        /// Identify the person who chose this character for this game
        /// </summary>
        public virtual Participants Participant { get; set; }
        public virtual Character Character { get; set; }
    }
}

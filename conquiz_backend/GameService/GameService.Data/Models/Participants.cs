﻿using System;
using System.Collections.Generic;

namespace GameService.Data.Models
{
    public partial class Participants
    {
        public Participants()
        {

        }

        public Participants(GameCharacter character, int playerId, int inGameParticipantNumber)
        {
            PlayerId = playerId;
            GameCharacter = character;
            Score = 0;
            InGameParticipantNumber = inGameParticipantNumber;
        }

        // Randomly assigned unique number from 1-3 for each participant in a lobby
        public int InGameParticipantNumber { get; set; }

        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int GameId { get; set; }
        public bool IsBot { get; set; }
        public int Score { get; set; }
        public int FinalQuestionScore { get; set; }

        public virtual GameCharacter GameCharacter { get; set; }
        public virtual GameInstance Game { get; set; }
        public virtual Users Player { get; set; }
    }
}

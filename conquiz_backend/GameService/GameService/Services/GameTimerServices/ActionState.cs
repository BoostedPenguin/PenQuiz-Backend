using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.GameTimerServices
{
    public enum ActionState
    {
        GAME_START_PREVIEW_TIME,

        // Rounding
        OPEN_PLAYER_ATTACK_VOTING,
        CLOSE_PLAYER_ATTACK_VOTING,


        // Open MC Question
        SHOW_MULTIPLE_CHOICE_QUESTION,
        END_MULTIPLE_CHOICE_QUESTION,
    }
}

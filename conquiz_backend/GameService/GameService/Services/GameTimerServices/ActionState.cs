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

        // Open Number Question
        SHOW_PREVIEW_GAME_MAP,
        SHOW_NUMBER_QUESTION,
        END_NUMBER_QUESTION,

        SHOW_MAP_NUMBER_QUESTION,

        // Pvp rounding
        OPEN_PVP_PLAYER_ATTACK_VOTING,
        CLOSE_PVP_PLAYER_ATTACK_VOTING,

        SHOW_PVP_MULTIPLE_CHOICE_QUESTION,
        END_PVP_MULTIPLE_CHOICE_QUESTION,



        SHOW_PVP_NUMBER_QUESTION,
        END_PVP_NUMBER_QUESTION,


        // Capital
        SHOW_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION,
        SHOW_CAPITAL_PVP_NUMBER_QUESTION,

        END_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION,
        END_CAPITAL_PVP_NUMBER_QUESTION,

        // Final round
        SHOW_FINAL_PVP_NUMBER_QUESTION,
        END_FINAL_PVP_NUMBER_QUESTION,

        // Game end
        END_GAME,
    }
}

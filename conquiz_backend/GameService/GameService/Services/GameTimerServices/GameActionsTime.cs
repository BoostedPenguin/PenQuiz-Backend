using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.GameTimerServices
{
    public class GameActionsTime
    {
        public static int DefaultPreviewTime = 3000;
        public static int NumberQuestionPreviewTime = 6000;

        public static int GetServerActionsTime(ActionState actionState)
        {
            return actionState switch
            {
                ActionState.GAME_START_PREVIEW_TIME => 3000,
                ActionState.OPEN_PLAYER_ATTACK_VOTING => 4000,
                ActionState.CLOSE_PLAYER_ATTACK_VOTING => throw new NotImplementedException(),
                ActionState.SHOW_MULTIPLE_CHOICE_QUESTION => 3000,
                ActionState.END_MULTIPLE_CHOICE_QUESTION => throw new NotImplementedException(),
                ActionState.SHOW_NUMBER_QUESTION => 15000,
                ActionState.END_NUMBER_QUESTION => throw new NotImplementedException(),
                ActionState.SHOW_MAP_NUMBER_QUESTION => throw new NotImplementedException(),
                ActionState.SHOW_PREVIEW_GAME_MAP => throw new NotImplementedException(),
                ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING => 8000,
                ActionState.CLOSE_PVP_PLAYER_ATTACK_VOTING => throw new NotImplementedException(),
                ActionState.SHOW_PVP_MULTIPLE_CHOICE_QUESTION => 15000,
                ActionState.END_PVP_MULTIPLE_CHOICE_QUESTION => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };
        }
    }
}

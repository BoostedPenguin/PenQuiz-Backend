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
                ActionState.OPEN_PLAYER_ATTACK_VOTING => 3000,
                ActionState.SHOW_MULTIPLE_CHOICE_QUESTION => 3000,
                ActionState.SHOW_NUMBER_QUESTION => 3000,
                ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING => 3000,
                ActionState.SHOW_PVP_MULTIPLE_CHOICE_QUESTION => 3000,
                _ => throw new NotImplementedException(),
            };
        }
    }
}

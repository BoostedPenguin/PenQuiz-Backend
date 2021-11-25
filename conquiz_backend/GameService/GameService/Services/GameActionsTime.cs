using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services
{
    public static class GameActionsTime
    {
        public static int DefaultPreviewTime = 3000;

        public static int GetServerActionsTime(ActionState actionState)
        {
            return actionState switch
            {
                ActionState.GAME_START_PREVIEW_TIME => 3000,
                ActionState.OPEN_PLAYER_ATTACK_VOTING => 4000,
                ActionState.CLOSE_PLAYER_ATTACK_VOTING => throw new NotImplementedException(),
                ActionState.SHOW_MULTIPLE_CHOICE_QUESTION => 5000,
                ActionState.END_MULTIPLE_CHOICE_QUESTION => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };
        }
    }
}

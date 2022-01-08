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

        public static int GetTime(ActionState action)
        {
            return action switch
            {
                ActionState.GAME_START_PREVIEW_TIME => GlobalValues(ActionState.GAME_START_PREVIEW_TIME),
                ActionState.OPEN_PLAYER_ATTACK_VOTING => DefaultPreviewTime,
                ActionState.CLOSE_PLAYER_ATTACK_VOTING => GlobalValues(ActionState.OPEN_PLAYER_ATTACK_VOTING),
                ActionState.SHOW_MULTIPLE_CHOICE_QUESTION => DefaultPreviewTime,
                ActionState.END_MULTIPLE_CHOICE_QUESTION => GlobalValues(ActionState.SHOW_MULTIPLE_CHOICE_QUESTION),
                ActionState.SHOW_PREVIEW_GAME_MAP => DefaultPreviewTime,
                ActionState.SHOW_NUMBER_QUESTION => throw new NotImplementedException(),
                ActionState.END_NUMBER_QUESTION => throw new NotImplementedException(),
                ActionState.SHOW_MAP_NUMBER_QUESTION => throw new NotImplementedException(),
                ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING => throw new NotImplementedException(),
                ActionState.CLOSE_PVP_PLAYER_ATTACK_VOTING => throw new NotImplementedException(),
                ActionState.SHOW_PVP_MULTIPLE_CHOICE_QUESTION => throw new NotImplementedException(),
                ActionState.END_PVP_MULTIPLE_CHOICE_QUESTION => throw new NotImplementedException(),
                ActionState.SHOW_PVP_NUMBER_QUESTION => throw new NotImplementedException(),
                ActionState.END_PVP_NUMBER_QUESTION => throw new NotImplementedException(),
                ActionState.SHOW_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION => throw new NotImplementedException(),
                ActionState.SHOW_CAPITAL_PVP_NUMBER_QUESTION => throw new NotImplementedException(),
                ActionState.END_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION => throw new NotImplementedException(),
                ActionState.END_CAPITAL_PVP_NUMBER_QUESTION => throw new NotImplementedException(),
                ActionState.SHOW_FINAL_PVP_NUMBER_QUESTION => throw new NotImplementedException(),
                ActionState.END_FINAL_PVP_NUMBER_QUESTION => throw new NotImplementedException(),
                ActionState.END_GAME => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };
        }

        public static int GetServerActionsTime(ActionState actionState)
        {
            return actionState switch
            {
                ActionState.GAME_START_PREVIEW_TIME => 3000,
                ActionState.OPEN_PLAYER_ATTACK_VOTING => 20000,
                ActionState.SHOW_MULTIPLE_CHOICE_QUESTION => 8000,
                ActionState.SHOW_NUMBER_QUESTION => 12000,
                ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING => 8000,
                ActionState.SHOW_PVP_MULTIPLE_CHOICE_QUESTION => 8000,
                _ => throw new NotImplementedException(),
            };
        }

        private static int GlobalValues(ActionState actionState)
        {
            return actionState switch
            {
                ActionState.GAME_START_PREVIEW_TIME => 3000,
                ActionState.OPEN_PLAYER_ATTACK_VOTING => 6000,
                ActionState.SHOW_MULTIPLE_CHOICE_QUESTION => 8000,
                ActionState.SHOW_NUMBER_QUESTION => 12000,
                ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING => 8000,
                ActionState.SHOW_PVP_MULTIPLE_CHOICE_QUESTION => 8000,
                _ => throw new NotImplementedException(),
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.GameTimerServices
{
    public class GameActionsTime
    {
        public const int BotTerritorySelectTime = 1500;
        public const int BotVsBotQuestionTime = 4000;

        public const int DefaultPreviewTime = 3000;
        public const int NumberQuestionPreviewTime = 6000;

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
                ActionState.SHOW_NUMBER_QUESTION => NumberQuestionPreviewTime,
                ActionState.END_NUMBER_QUESTION => GlobalValues(ActionState.SHOW_NUMBER_QUESTION),
                ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING => DefaultPreviewTime,
                ActionState.CLOSE_PVP_PLAYER_ATTACK_VOTING => GlobalValues(ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING),
                ActionState.SHOW_PVP_MULTIPLE_CHOICE_QUESTION => DefaultPreviewTime,
                ActionState.END_PVP_MULTIPLE_CHOICE_QUESTION => GlobalValues(ActionState.SHOW_PVP_MULTIPLE_CHOICE_QUESTION),
                ActionState.SHOW_PVP_NUMBER_QUESTION => DefaultPreviewTime,
                ActionState.END_PVP_NUMBER_QUESTION => GlobalValues(ActionState.SHOW_NUMBER_QUESTION),
                ActionState.SHOW_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION => DefaultPreviewTime,
                ActionState.SHOW_CAPITAL_PVP_NUMBER_QUESTION => DefaultPreviewTime,
                ActionState.END_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION => GlobalValues(ActionState.SHOW_PVP_MULTIPLE_CHOICE_QUESTION),
                ActionState.END_CAPITAL_PVP_NUMBER_QUESTION => GlobalValues(ActionState.SHOW_NUMBER_QUESTION),
                ActionState.SHOW_FINAL_PVP_NUMBER_QUESTION => DefaultPreviewTime,
                ActionState.END_FINAL_PVP_NUMBER_QUESTION => GlobalValues(ActionState.SHOW_NUMBER_QUESTION),
                ActionState.END_GAME => DefaultPreviewTime,
                _ => throw new NotImplementedException(),
            };
        }

        private static int GlobalValues(ActionState actionState)
        {
            return actionState switch
            {
                // On game start preview time, not same as defaultpreviewtime
                ActionState.GAME_START_PREVIEW_TIME => 3000,

                // Neutral round time to attack neutral territory
                ActionState.OPEN_PLAYER_ATTACK_VOTING => 8000,

                // Neutral round time to answer multiple choice question
                ActionState.SHOW_MULTIPLE_CHOICE_QUESTION => 12000,

                // Universal, time to answer number question
                ActionState.SHOW_NUMBER_QUESTION => 12000,

                // PVP Rounds time to attack enemy territory
                ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING => 8000,

                // PVP Round time to answer multiple choice question
                ActionState.SHOW_PVP_MULTIPLE_CHOICE_QUESTION => 12000,
                _ => throw new NotImplementedException(),
            };
        }
    }
}

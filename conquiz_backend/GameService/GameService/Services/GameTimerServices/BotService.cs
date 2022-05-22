using System;

namespace GameService.Services.GameTimerServices
{
    public static class BotService
    {
        public const int BOT_MC_WIN_PERCENT = 40;
        public const int BOT_NEUTRAL_CORRECT_RANGE_PERCENT = 30;

        private static readonly Random r = new();

        public static bool DidBotWinMultiple()
        {
            var randomNumber = r.Next(0, 100);

            if (randomNumber <= BOT_MC_WIN_PERCENT)
                return true;

            return false;
        }

        //public static bool DidBotWinNumber(long correctNumber)
        //{
        //    var topNumberRange = correctNumber + ((BOT_NEUTRAL_CORRECT_RANGE_PERCENT / 100) * correctNumber);
        //    var botNumberRange = correctNumber - ((BOT_NEUTRAL_CORRECT_RANGE_PERCENT / 100) * correctNumber);

        //    r.NextInt64
        //    r.Next(topNumberRange, botNumberRange);
        //}
    }
}

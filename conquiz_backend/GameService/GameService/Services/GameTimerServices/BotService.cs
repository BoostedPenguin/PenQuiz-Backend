using GameService.Data.Models;
using System;
using System.Linq;

namespace GameService.Services.GameTimerServices
{
    public static class BotService
    {
        private const int BOT_MC_WIN_PERCENT = 40;
        private const int NUMBER_NON_YEAR_CORRECT_RANGE_PERCENT = 50;
        private const int NUMBER_LOWER_YEAR_THRESHHOLD = 1750;
        private const int NUMBER_UPPER_YEAR_THRESHHOLD = 2025;


        private static readonly Random r = new();

        private static bool DidBotWinMultiple()
        {
            var randomNumber = r.Next(0, 100);

            if (randomNumber <= BOT_MC_WIN_PERCENT)
                return true;

            return false;
        }

        public static int GenerateBotMCAnswerId(Answers[] answers)
        {
            var res = DidBotWinMultiple();

            if (res)
            {
                return answers.First(e => e.Correct).Id;
            }
            else
            {
                var randomWrongAnswer = r.Next(0, 3);

                return answers.Where(e => !e.Correct)
                    .ToArray()[randomWrongAnswer]
                    .Id;
            }
        }

        public static long GenerateBotNumberAnswer(long correctNumber)
        {

            if(correctNumber > 100)
            {
                var topNumberRange = Convert.ToInt64(correctNumber + ((NUMBER_NON_YEAR_CORRECT_RANGE_PERCENT / 100.00) * correctNumber));
                var botNumberRange = Convert.ToInt64(correctNumber - ((NUMBER_NON_YEAR_CORRECT_RANGE_PERCENT / 100.00) * correctNumber));

                if (correctNumber > 1000 && correctNumber < NUMBER_UPPER_YEAR_THRESHHOLD && topNumberRange > NUMBER_UPPER_YEAR_THRESHHOLD) 
                    topNumberRange = NUMBER_UPPER_YEAR_THRESHHOLD;

                // Mostly years
                if(correctNumber > NUMBER_LOWER_YEAR_THRESHHOLD && correctNumber < NUMBER_UPPER_YEAR_THRESHHOLD)
                {
                    topNumberRange = NUMBER_UPPER_YEAR_THRESHHOLD;
                    botNumberRange = NUMBER_LOWER_YEAR_THRESHHOLD;
                }

                return r.NextInt64(botNumberRange, topNumberRange);
            }


            var minDigitCountString = "1";

            for(var i = 0; i < correctNumber.ToString().Length - 1; i++)
            {
                minDigitCountString += "0";
            }

            var maxDigitCountString = minDigitCountString.Replace("0", "9");
            maxDigitCountString = maxDigitCountString.Replace("1", "9");

            var answer = r.NextInt64(Convert.ToInt64(minDigitCountString), Convert.ToInt64(maxDigitCountString));

            return answer;
        }
    }
}

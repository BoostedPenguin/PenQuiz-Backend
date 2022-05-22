using GameService.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.GameTimerServices.NeutralTimerServices
{
    public partial class NeutralMCTimerEvents
    {
        private static List<Round> Create_Neutral_Number_Rounds(GameInstance gm, TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;
            var untakenTerritoriesCount = gm.ObjectTerritory
                .Where(x => x.TakenBy == null && x.GameInstanceId == data.GameInstanceId)
                .Count();

            var participantsIds = gm.Participants.Select(x => x.PlayerId).ToList();

            var numberQuestionRounds = new List<Round>();
            for (var i = 0; i < untakenTerritoriesCount; i++)
            {
                var baseRound = new Round()
                {
                    // After this method executes we switch to the next round automatically thus + 1 now
                    GameRoundNumber = gm.GameRoundNumber + i,
                    AttackStage = AttackStage.NUMBER_NEUTRAL,
                    Description = $"Number question. Attacker vs NEUTRAL territory",
                    IsQuestionVotingOpen = false,
                    IsTerritoryVotingOpen = false,
                    GameInstanceId = data.GameInstanceId
                };

                baseRound.NeutralRound = new NeutralRound()
                {
                    AttackOrderNumber = 0,
                };

                foreach (var participId in participantsIds)
                {
                    baseRound.NeutralRound.TerritoryAttackers.Add(new AttackingNeutralTerritory()
                    {
                        AttackerId = participId,
                        AttackOrderNumber = 0,
                    });
                }

                numberQuestionRounds.Add(baseRound);
            }

            return numberQuestionRounds;
        }


        public async Task Debug_Start_Number_Neutral(TimerWrapper timerWrapper)
        {
            timerWrapper.Stop();
            using var db = contextFactory.CreateDbContext();
            var data = timerWrapper.Data;

            var gm = data.GameInstance;


            // Go to next round
            gm.GameRoundNumber = data.LastNeutralMCRound + 1;

            // Create number question rounds if gm multiple choice neutral rounds are over
            var rounds = Create_Neutral_Number_Rounds(gm, timerWrapper);

            rounds.ForEach(e => gm.Rounds.Add(e));

            db.Update(gm);
            await db.SaveChangesAsync();

            data.LastNeutralNumberRound = gm.Rounds.Where(e => e.AttackStage == AttackStage.NUMBER_NEUTRAL)
                .OrderByDescending(e => e.GameRoundNumber)
                .Select(e => e.GameRoundNumber).First();

            CommonTimerFunc.RequestQuestions(messageBus, data.GameGlobalIdentifier, rounds, true);

            // Next action should be a number question related one
            timerWrapper.StartTimer(ActionState.SHOW_PREVIEW_GAME_MAP);
        }
    }
}

using GameService.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.GameTimerServices.NeutralTimerServices
{
    public partial class NeutralNumberTimerEvents
    {

        public async Task Debug_Assign_All_Territories_Start_Pvp(TimerWrapper timerWrapper)
        {
            timerWrapper.Stop();
            using var db = contextFactory.CreateDbContext();

            var data = timerWrapper.Data;
            var gm = data.GameInstance;

            var particip = gm.Participants.ToList();

            var untakenTer = gm.ObjectTerritory.Where(x => x.TakenBy == null).ToList();

            for (var i = 0; i < 5; i++)
            {
                untakenTer.First(x => x.TakenBy == null).TakenBy = particip[0].PlayerId;
                untakenTer.First(x => x.TakenBy == null).TakenBy = particip[1].PlayerId;
            }

            untakenTer.ForEach(x =>
            {
                if (x.TakenBy != null) return;
                x.TakenBy = particip[2].PlayerId;
            });

            // PVP Rounds are always 18 rounds (3x6)
            gm.GameRoundNumber = 41;

            var rounds = Create_Pvp_Rounds(gm, gm.Participants.Select(x => x.PlayerId).ToList());

            foreach (var round in rounds)
            {
                // Multiple
                round.Question = new Questions()
                {
                    Question = "When was bulgaria created?",
                    Type = "multiple",
                };
                round.Question.Answers.Add(new Answers()
                {
                    Correct = true,
                    Answer = "681",
                });
                round.Question.Answers.Add(new Answers()
                {
                    Correct = false,
                    Answer = "15",
                });
                round.Question.Answers.Add(new Answers()
                {
                    Correct = false,
                    Answer = "22",
                });
                round.Question.Answers.Add(new Answers()
                {
                    Correct = false,
                    Answer = "512",
                });

                // Number
                round.PvpRound.NumberQuestion = new Questions()
                {
                    Question = "When was covid discovered?",
                    Type = "number"
                };
                round.PvpRound.NumberQuestion.Answers.Add(new Answers()
                {
                    Correct = true,
                    Answer = "2019"
                });
            }

            // PVP Rounds are always 18 rounds (3x6)
            // Base round is 41
            // 41 + 18 = 59 is last round
            gm.GameRoundNumber = 48;


            rounds.ForEach(e => gm.Rounds.Add(e));
            db.Update(gm);
            await db.SaveChangesAsync();

            data.LastPvpRound = gm.Rounds.OrderByDescending(e => e.GameRoundNumber).Select(e => e.GameRoundNumber).First();

            timerWrapper.StartTimer(ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING);
        }

        private List<Round> Create_Pvp_Rounds(GameInstance gm, List<int> userIds)
        {
            int RequiredPlayers = 3;

            var totalTerritories = mapGeneratorService.GetAmountOfTerritories(gm);

            var order = CommonTimerFunc.GenerateAttackOrder(userIds, totalTerritories, RequiredPlayers, false);

            var finalRounds = new List<Round>();

            var roundCounter = gm.GameRoundNumber;

            foreach (var fullRound in order.UserRoundAttackOrders)
            {
                foreach (var roundAttackerId in fullRound)
                {
                    var baseRound = new Round()
                    {
                        GameInstanceId = gm.Id,
                        GameRoundNumber = roundCounter++,
                        AttackStage = AttackStage.MULTIPLE_PVP,
                        Description = $"MultipleChoice question. Attacker vs PVP territory",
                        IsQuestionVotingOpen = false,
                        IsTerritoryVotingOpen = false,
                    };
                    baseRound.PvpRound = new PvpRound()
                    {
                        AttackerId = roundAttackerId,
                    };

                    finalRounds.Add(baseRound);
                }
            }

            var result = finalRounds.ToList();

            return result;
        }
    }
}

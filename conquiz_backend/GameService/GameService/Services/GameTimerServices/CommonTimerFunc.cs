using GameService.Context;
using GameService.Data;
using GameService.Data.Models;
using GameService.Dtos;
using GameService.Hubs;
using GameService.MessageBus;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.GameTimerServices
{
    public class UserAttackOrder
    {
        /// <summary>
        /// Goes like this:
        /// First 3 rounds are stored in a list
        /// Each person gets random attack order in them: 2, 3, 1
        /// Then that gets stored in a list itself
        /// 1 {1, 3, 2}   2 {2, 3, 1}  3{3, 1, 2} etc.
        /// </summary>
        public List<List<int>> UserRoundAttackOrders { get; set; }
        public int TotalTerritories { get; set; }
        public int LeftTerritories { get; set; }

        public UserAttackOrder(List<List<int>> userRoundAttackOrders, int totalTerritories, int leftTerritories)
        {
            this.UserRoundAttackOrders = userRoundAttackOrders;
            this.TotalTerritories = totalTerritories;
            this.LeftTerritories = leftTerritories;
        }
    }

    public class CommonTimerFunc
    {
        public enum PvpStageIsGameOver
        {
            GAME_OVER,
            GAME_CONTINUING,
            REQUEST_FINAL_QUESTION,
        }

        private static readonly Random r = new Random();

        /// <summary>
        /// Gets all game instance details <br />
        /// Do not call this method often as it is slow. Use more precise queries instead.
        /// </summary>
        /// <param name="gameInstanceId"></param>
        /// <param name="defaultContext"></param>
        /// <returns></returns>
        public static async Task<GameInstance> GetFullGameInstance(int gameInstanceId, DefaultContext defaultContext)
        {
            var game = await defaultContext.GameInstance
                .Include(x => x.Rounds)
                .ThenInclude(e => e.Question)
                .ThenInclude(e => e.Answers)

                .Include(x => x.Participants)   
                .ThenInclude(x => x.Player)

                .Include(x => x.Rounds)
                .ThenInclude(x => x.NeutralRound)
                .ThenInclude(x => x.TerritoryAttackers)
                .ThenInclude(x => x.AttackedTerritory)

                .Include(x => x.Rounds)
                .ThenInclude(x => x.PvpRound)
                .ThenInclude(x => x.PvpRoundAnswers)

                .Include(x => x.ObjectTerritory)
                .ThenInclude(x => x.MapTerritory)

                .Include(x => x.Map)
                .ThenInclude(x => x.MapTerritory)

                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.Id == gameInstanceId);

            foreach (var particip in game.Participants)
            {
                var totalParticipScore = game.ObjectTerritory
                    .Where(x => x.TakenBy == particip.PlayerId)
                    .Sum(x => x.TerritoryScore);

                totalParticipScore += particip.FinalQuestionScore;

                if (particip.Score != totalParticipScore)
                {
                    particip.Score = totalParticipScore;
                    defaultContext.Update(particip);
                }
            }

            await defaultContext.SaveChangesAsync();


            foreach (var round in game.Rounds)
            {
                if (round.AttackStage == AttackStage.MULTIPLE_NEUTRAL || round.AttackStage == AttackStage.NUMBER_NEUTRAL)
                {
                    round.NeutralRound.TerritoryAttackers =
                        round.NeutralRound.TerritoryAttackers.OrderBy(x => x.AttackOrderNumber).ToList();
                }
            }

            game.Rounds = game.Rounds.OrderBy(x => x.GameRoundNumber).ToList();
            return game;
        }
        public static async Task<PvpStageIsGameOver> PvpStage_IsGameOver(TimerWrapper timerWrapper, PvpRound round, DefaultContext db, IMessageBusClient messageBus)
        {
            var gm = timerWrapper.Data.GameInstance;
            var data = timerWrapper.Data;

            // Check if there are any non-attacker territories left

            var nonAttackerTerritoriesCount = gm.ObjectTerritory
                .Where(x => x.GameInstanceId == data.GameInstanceId && x.TakenBy != round.AttackerId)
                .Count();


            // This attacker controls all territories. Skip any other rounds and declare him winner.
            if (nonAttackerTerritoriesCount == 0)
            {
                return PvpStageIsGameOver.GAME_OVER;
            }

            // Check if last pvp round
            if (data.CurrentGameRoundNumber > data.LastPvpRound)
            {
                var allPlayerTerritoriesWoCapital = db.ObjectTerritory
                    .Where(x => !x.IsCapital).ToList();

                var groupedBy = allPlayerTerritoriesWoCapital
                    .GroupBy(x => x.TakenBy)
                    .OrderBy(x => x.Count())
                    .ToList();

                var identicalScores = groupedBy
                    .Where(x => groupedBy
                        .Where(y => y != x)
                        .Any(y => x.Count() == y.Count()));

                // On final pvp round and there are at least 2 people with the same score
                // Request additional number question to make sure scoring for each user is unique
                if (identicalScores.Count() >= 2)
                {
                    var baseRound = new Round()
                    {
                        GameRoundNumber = data.CurrentGameRoundNumber,
                        AttackStage = AttackStage.FINAL_NUMBER_PVP,
                        Description = $"Final Number question. Attacker vs other attackers",
                        IsQuestionVotingOpen = false,
                        IsTerritoryVotingOpen = false,
                        GameInstanceId = data.GameInstanceId,
                    };

                    baseRound.NeutralRound = new NeutralRound()
                    {
                        AttackOrderNumber = 0
                    };

                    foreach (var person in identicalScores)
                    {
                        baseRound.NeutralRound.TerritoryAttackers.Add(new AttackingNeutralTerritory()
                        {
                            AttackerId = person.Key.Value,
                            AttackOrderNumber = 0,
                        });
                    }

                    await db.AddAsync(baseRound);
                    await db.SaveChangesAsync();

                    RequestFinalNumberQuestion(messageBus, data.GameGlobalIdentifier, baseRound.Id);

                    return PvpStageIsGameOver.REQUEST_FINAL_QUESTION;
                }

                return PvpStageIsGameOver.GAME_OVER;
            }

            return PvpStageIsGameOver.GAME_CONTINUING;
        }

        public static UserAttackOrder GenerateAttackOrder(List<int> userIds, int totalTerritories, int RequiredPlayers, bool excludeCapitals = true)
        {
            if (userIds.Count != RequiredPlayers) throw new ArgumentException("There must be a total of 3 people in a game!");

            // 1 3 2   3 2 1

            // Removing the capital territories;
            int emptyTerritories = totalTerritories;

            if (excludeCapitals)
                emptyTerritories = totalTerritories - RequiredPlayers;

            if (emptyTerritories < RequiredPlayers) throw new ArgumentException("There are less than 3 territories left except the capital. Abort the game.");

            // Store 
            var attackOrder = new List<List<int>>();
            while (emptyTerritories >= RequiredPlayers)
            {
                var fullRound = new List<int>();

                while (fullRound.Count < RequiredPlayers)
                {
                    var person = userIds[r.Next(0, RequiredPlayers)];
                    if (!fullRound.Contains(person))
                    {
                        fullRound.Add(person);
                    }
                }
                attackOrder.Add(fullRound);
                emptyTerritories -= fullRound.Count();
            }

            return new UserAttackOrder(attackOrder, totalTerritories, emptyTerritories);
        }

        public static void RequestCapitalQuestions(IMessageBusClient messageBus, string gameGlobalIdentifier, List<int> capitalRoundsIds)
        {
            // Request questions only for the initial multiple questions for neutral attacking order
            // After multiple choices are over, request a new batch for number questions for all untaken territories
            messageBus.RequestQuestions(new RequestCapitalQuestionsDto()
            {
                Event = "Capital_Question_Request",
                GameGlobalIdentifier = gameGlobalIdentifier,
                QuestionsCapitalRoundId = capitalRoundsIds,
            });
        }

        public static void RequestFinalNumberQuestion(IMessageBusClient messageBus, string gameGlobalIdentifier, int finalRoundId)
        {
            // Request final score determining number question
            messageBus.RequestFinalNumberQuestion(new RequestFinalNumberQuestionDto()
            {
                Event = "FinalNumber_Question_Request",
                GameGlobalIdentifier = gameGlobalIdentifier,
                QuestionFinalRoundId = finalRoundId,
            });
        }

        public static void RequestQuestions(IMessageBusClient messageBus, string gameGlobalIdentifier, IEnumerable<Round> rounds, bool isNeutralGeneration = false)
        {
            // Request questions only for the initial multiple questions for neutral attacking order
            // After multiple choices are over, request a new batch for number questions for all untaken territories
            messageBus.RequestQuestions(new RequestQuestionsDto()
            {
                Event = "Question_Request",
                GameGlobalIdentifier = gameGlobalIdentifier,
                MultipleChoiceQuestionsRoundId = rounds.Where(x => x.AttackStage == AttackStage.MULTIPLE_NEUTRAL ||
                        x.AttackStage == AttackStage.MULTIPLE_PVP)
                    .Select(x => x.Id)
                    .ToList(),
                NumberQuestionsRoundId = rounds.Where(x => x.AttackStage == AttackStage.NUMBER_NEUTRAL ||
                        x.AttackStage == AttackStage.NUMBER_PVP)
                    .Select(x => x.Id)
                    .ToList(),
                IsNeutralGeneration = isNeutralGeneration,
            });
        }
    }
}

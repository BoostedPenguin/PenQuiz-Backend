using GameService.Context;
using GameService.Dtos;
using GameService.Hubs;
using GameService.MessageBus;
using GameService.Models;
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
        private static readonly Random r = new Random();
        public static async Task<GameInstance> GetFullGameInstance(int gameInstanceId, DefaultContext defaultContext)
        {
            var game = await defaultContext.GameInstance
                .Include(x => x.Participants)   
                .ThenInclude(x => x.Player)
                .Include(x => x.Rounds)
                .ThenInclude(x => x.NeutralRound)
                .ThenInclude(x => x.TerritoryAttackers)
                .Include(x => x.Rounds)
                .ThenInclude(x => x.PvpRound)
                .ThenInclude(x => x.PvpRoundAnswers)
                .Include(x => x.ObjectTerritory)
                .ThenInclude(x => x.MapTerritory)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.Id == gameInstanceId);

            foreach (var particip in game.Participants)
            {
                var totalParticipScore = game.ObjectTerritory
                    .Where(x => x.TakenBy == particip.PlayerId)
                    .Sum(x => x.TerritoryScore);

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

        public static async Task<bool> PvpStage_IsGameOver(TimerWrapper timerWrapper, PvpRound round, DefaultContext db)
        {
            var data = timerWrapper.Data;

            // Check if there are any non-attacker territories left
            var nonAttackerTerritoriesCount = await db.ObjectTerritory
                .Where(x => x.GameInstanceId == data.GameInstanceId && x.TakenBy != round.AttackerId)
                .CountAsync();


            if (nonAttackerTerritoriesCount == 0)
            {
                var allPlayerTerritoriesWoCapital = db.ObjectTerritory
                    .Where(x => x.GameInstanceId == data.GameInstanceId && !x.IsCapital).ToList();

                var groupedBy = allPlayerTerritoriesWoCapital
                    .GroupBy(x => x.TakenBy)
                    .OrderBy(x => x.Count())
                    .ToList();

                var identicalScores = groupedBy
                    .Where(x => groupedBy
                        .Where(y => y != x)
                        .Any(y => x.Count() == y.Count()));

                // All 3 players have same score
                // Ask everyone a question
                if(identicalScores.Count() == 3)
                {
                    var baseRound = new Round()
                    {
                        GameRoundNumber = data.CurrentGameRoundNumber,
                        AttackStage = AttackStage.NUMBER_NEUTRAL,
                        Description = $"Number question. Attacker vs NEUTRAL territory",
                        IsQuestionVotingOpen = false,
                        IsTerritoryVotingOpen = false,
                        GameInstanceId = data.GameInstanceId,
                    };

                    baseRound.NeutralRound = new NeutralRound()
                    {
                        AttackOrderNumber = 0
                    };

                    foreach(var person in identicalScores)
                    {
                        baseRound.NeutralRound.TerritoryAttackers.Add(new AttackingNeutralTerritory()
                        {
                            //AttackerId = person.Select(x => x.TakenBy) ?? 0,
                            AttackOrderNumber = 0,
                        });
                    }
                }
                
                // 2 players have same score
                if(identicalScores.Count() == 2)
                {

                }


                return true;
            }

            // Check if last pvp round
            if (data.CurrentGameRoundNumber > data.LastPvpRound)
            {
                return true;
            }

            return false;
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

        public static void RequestCapitalQuestions(IMessageBusClient messageBus, int gameInstanceId, List<int> capitalRoundsIds)
        {
            // Request questions only for the initial multiple questions for neutral attacking order
            // After multiple choices are over, request a new batch for number questions for all untaken territories
            messageBus.RequestQuestions(new RequestCapitalQuestionsDto()
            {
                Event = "Capital_Question_Request",
                GameInstanceId = gameInstanceId,
                QuestionsCapitalRoundId = capitalRoundsIds,
            });
        }

        public static void RequestFinalNumberQuestion(IMessageBusClient messageBus, int gameInstanceId, int finalRoundId)
        {
            // Request final score determining number question
            messageBus.RequestFinalNumberQuestion(new RequestFinalNumberQuestionDto()
            {
                Event = "FinalNumber_Question_Request",
                GameInstanceId = gameInstanceId,
                QuestionFinalRoundId = finalRoundId,
            });
        }

        public static void RequestQuestions(IMessageBusClient messageBus, int gameInstanceId, Round[] rounds, bool isNeutralGeneration = false)
        {
            // Request questions only for the initial multiple questions for neutral attacking order
            // After multiple choices are over, request a new batch for number questions for all untaken territories
            messageBus.RequestQuestions(new RequestQuestionsDto()
            {
                Event = "Question_Request",
                GameInstanceId = gameInstanceId,
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

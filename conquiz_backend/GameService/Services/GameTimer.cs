using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using GameService.Context;
using GameService.Hubs;
using GameService.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.EntityFrameworkCore;

namespace GameService.Services
{
    public interface IGameTimer
    {
        void OnGameStart(GameInstance gm);
    }

    /// <summary>
    /// Handles all game instance timers and callbacks the appropriate GameControlService functions
    /// </summary>
    public class GameTimer : DataService<DefaultModel>, IGameTimer
    {
        
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        
        // GameId<Game> | CurrentTimer 
        public static List<TimerWrapper> GameTimers = new List<TimerWrapper>();
        
        public GameTimer(IDbContextFactory<DefaultContext> _contextFactory, IHttpContextAccessor httpContextAccessor, IHubContext<GameHub, IGameHub> hubContext) : base(_contextFactory)
        {
            contextFactory = _contextFactory;
            this.httpContextAccessor = httpContextAccessor;
            this.hubContext = hubContext;
        }
        public enum NumberQuestionActions
        {
            SHOW_SCREEN,
            START_VOTING,
            END_VOTING,
            SHOW_RESULTS,
            PREVIEW_RESULTS,
            CLOSE_SCREEN
        }

        public enum MultipleChoiceQuestionActions
        {
            SHOW_SCREEN,
            START_VOTING,
            END_VOTING,
            SHOW_RESULTS,
            PREVIEW_RESULTS,
            CLOSE_SCREEN
        }

        public enum AttackingTerritory
        {
            SHOW_SCREEN,
            START_PLAYER_TERRITORY_SELECT,
            END_PLAYER_TERRITORY_SELECT,
            PREVIEW_ATTACK,
            CLOSE_SCREEN
        }

        public enum States
        {
            NUMBER_QUESTION,
            MULTIPLE_CHOICE_QUESTION,
            ATTACKING_NEUTRAL_TERRITORY,
        }
        public class TimerWrapper : Timer
        {
            public TimerData Data { get; set; }

            public TimerWrapper(int gameInstanceId, string gameLink)
            {
                Data = new TimerData(gameInstanceId, gameLink);
            }


            public class TimerData
            {
                public TimerData(int gameInstanceId, string gameLink)
                {
                    GameInstanceId = gameInstanceId;
                    GameLink = gameLink;
                }
                public int GameInstanceId { get; set; }


                // This is the invitation link which also acts as a group ID for signalR
                public string GameLink { get; set; }
                public int CurrentGameRoundNumber { get; set; }
                public States NextAction { get; set; }
            }
        }

        const int START_PREVIEW_TIME = 5000;

        public void OnGameStart(GameInstance gm)
        {
            if(GameTimers.FirstOrDefault(x => x.Data.GameInstanceId == gm.Id) != null)
                throw new ArgumentException("Timer already exists for this game instance");

            var actionTimer = new TimerWrapper(gm.Id, gm.InvitationLink)
            {
                AutoReset = false,
                Interval = START_PREVIEW_TIME,
            };

            actionTimer.Data.NextAction = States.ATTACKING_NEUTRAL_TERRITORY;

            GameTimers.Add(actionTimer);

            // Start timer
            actionTimer.Elapsed += ActionTimer_Elapsed;
            actionTimer.Start();
        }

        private void ActionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var timer = (TimerWrapper)sender;

            switch (timer.Data.NextAction)
            {
                case States.NUMBER_QUESTION:
                    break;
                case States.MULTIPLE_CHOICE_QUESTION:
                    break;
                case States.ATTACKING_NEUTRAL_TERRITORY:
                    break;
            }
        }

        private async Task Show_MultipleChoice_Screen(TimerWrapper timerWrapper)
        {
            // Stop timer until we calculate the next action and client event
            timerWrapper.Stop();

            // Get the question and show it to the clients
            var data = timerWrapper.Data;
            var db = contextFactory.CreateDbContext();
            
            var question = await db.Questions
                .Include(x => x.Answers)
                .Include(x => x.Rounds)
                .Where(x => x.Rounds.GameRoundNumber == data.CurrentGameRoundNumber 
                    && x.Rounds.GameInstanceId == data.GameInstanceId)
                .FirstOrDefaultAsync();

            // Open this question for voting
            question.Rounds.IsVotingOpen = true;
            db.Update(question.Rounds);
            await db.SaveChangesAsync();


            await hubContext.Clients.Group(data.GameLink).GetRoundQuestion(question);

            // Decide who can vote
            var attackerId = question.Rounds.AttackerId;
            var defenderId = question.Rounds.DefenderId;
            
            // If it's versus NEUTRAL territory, defenderId can be null
            await hubContext.Clients.User(attackerId.ToString()).CanPerformActions();

            if(defenderId != null)
                await hubContext.Clients.User(defenderId.ToString()).CanPerformActions();

        }

        private async Task Close_MultipleChoice_Voting(TimerWrapper timerWrapper)
        {
            // Can disable voting on start, however even 0-1s delay wouldn't be game breaking and would ease performance
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            var db = contextFactory.CreateDbContext();

            var currentRound = 
                await db.Rounds
                .Include(x => x.RoundAnswers)
                .ThenInclude(x => x.Answer)
                .ThenInclude(x => x.Question)
                .FirstOrDefaultAsync(x => x.GameRoundNumber == data.CurrentGameRoundNumber 
                    && x.GameInstanceId == data.GameInstanceId);
            
            currentRound.IsVotingOpen = false;

            var playerCorrect = new Dictionary<int, bool>();

            foreach(var pAnswer in currentRound.RoundAnswers)
            {
                playerCorrect[pAnswer.UserId] = pAnswer.Answer.Correct;
            }

            // Both players answered correctly
            switch(playerCorrect.Values.Count(x => x))
            {
                // No one answered correctly
                case 0:
                    currentRound.RoundWinnerId = currentRound.DefenderId;
                    break;

                // One person answered correctly
                case 1:
                    var b = playerCorrect.First(x => x.Value == true);
                    
                    // Attacker won
                    if(b.Key == currentRound.AttackerId)
                    {
                        currentRound.AttackedTerritory.TakenBy = currentRound.AttackerId;
                        currentRound.RoundWinnerId = currentRound.AttackerId;
                    }
                    // Defender won automatically
                    else
                    {
                        currentRound.RoundWinnerId = currentRound.DefenderId;
                    }
                    break;

                // Two people answered correctly
                // Happens only 
                case 2:
                    break;

                default:
                    throw new ArgumentException("Internal server error. Unknown result for voting.");
            }
        }
    }
}

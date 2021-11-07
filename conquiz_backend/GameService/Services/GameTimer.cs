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
using AutoMapper;
using GameService.Dtos;
using GameService.Dtos.SignalR_Responses;

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
        private readonly IMapper mapper;

        // GameId<Game> | CurrentTimer 
        public static List<TimerWrapper> GameTimers = new List<TimerWrapper>();
        
        public GameTimer(IDbContextFactory<DefaultContext> _contextFactory, IHttpContextAccessor httpContextAccessor, IHubContext<GameHub, IGameHub> hubContext, IMapper mapper) : base(_contextFactory)
        {
            contextFactory = _contextFactory;
            this.httpContextAccessor = httpContextAccessor;
            this.hubContext = hubContext;
            this.mapper = mapper;
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
            //NUMBER_QUESTION,
            //MULTIPLE_CHOICE_QUESTION,
            //ATTACKING_NEUTRAL_TERRITORY,


            SHOW_SCREEN,
            START_VOTING,
            END_VOTING,
            SHOW_RESULTS,
            PREVIEW_RESULTS,
            CLOSE_SCREEN
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

            //TODO
            actionTimer.Data.NextAction = States.CLOSE_SCREEN;

            GameTimers.Add(actionTimer);

            // Start timer
            actionTimer.Elapsed += ActionTimer_Elapsed;
            actionTimer.Start();
        }

        //TODO
        private void ActionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var timer = (TimerWrapper)sender;

            switch (timer.Data.NextAction)
            {
                case States.CLOSE_SCREEN:
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
            
            // Show the primary question to the user
            var question = await db.Questions
                .Include(x => x.Answers)
                .Include(x => x.Rounds)
                .Where(x => x.Type == "multiple" && x.Rounds.GameRoundNumber == data.CurrentGameRoundNumber 
                    && x.Rounds.GameInstanceId == data.GameInstanceId)
                .FirstOrDefaultAsync();

            // Open this question for voting
            question.Rounds.IsVotingOpen = true;
            db.Update(question.Rounds);
            await db.SaveChangesAsync();

            await SendQuestionHub(data.GameLink, question);
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

            // Get the primary question answers
            foreach(var pAnswer in currentRound.RoundAnswers.Where(x => x.Answer.Question.Type == "multiple"))
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
                // Happens only in PVP
                case 2:
                    // Switches to second question
                    await Open_FromMultiple_NumberChoice_Voting(db, currentRound, timerWrapper);
                    return;

                default:
                    throw new ArgumentException("Internal server error. Unknown result for voting.");
            }

            db.Update(currentRound);
            await db.SaveChangesAsync();

            var result = mapper.Map<QuestionResultResponse>(currentRound.Questions.First(x => x.Type == "multiple"));

            await hubContext.Clients.Group(data.GameLink).PreviewResult(result);

            timerWrapper.Data.NextAction = States.CLOSE_SCREEN;
            timerWrapper.Interval = 3000;
            timerWrapper.Start();
        }

        private async Task Close_Screen(TimerWrapper timerWrapper)
        {
            // Previewing elapsed, move to next round
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();

            var gm = await db.GameInstance
                .Include(x => x.Rounds)
                .Include(x => x.Participants)
                .Include(x => x.ObjectTerritory)
                .Include(x => x.Rounds)
                .Where(x => x.Id == data.GameInstanceId && x.Rounds
                    .Any(y => y.GameRoundNumber == data.CurrentGameRoundNumber || y.GameRoundNumber == data.CurrentGameRoundNumber + 1))
                .FirstOrDefaultAsync();

            // Move to next round
            gm.GameRoundNumber++;

            gm.Rounds.First(x => x.GameRoundNumber == data.CurrentGameRoundNumber).RoundStage = RoundStage.FINISHED;
            gm.Rounds.First(x => x.GameRoundNumber == data.CurrentGameRoundNumber + 1).RoundStage = RoundStage.CURRENT;

            db.Update(gm);
            await db.SaveChangesAsync();
            
            await hubContext.Clients.Group(data.GameLink).CloseQuestionScreen();
            await hubContext.Clients.Group(data.GameLink).GetGameInstance(gm);
        }


        public async Task SendQuestionHub(string groupId, Questions question)
        {
            // Mask answers and etc.
            var response = mapper.Map<QuestionClientResponse>(question);

            await hubContext.Clients.Group(groupId).GetRoundQuestion(response);

            // Decide who can vote
            var attackerId = question.Rounds.AttackerId;
            var defenderId = question.Rounds.DefenderId;

            // If it's versus NEUTRAL territory, defenderId can be null
            await hubContext.Clients.User(attackerId.ToString()).CanPerformActions();

            if (defenderId != null)
                await hubContext.Clients.User(defenderId.ToString()).CanPerformActions();
        }

        public async Task Open_FromMultiple_NumberChoice_Voting(DefaultContext db, Rounds currentRound, TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;
            currentRound.FollowUpNumberQuestion = true;
            currentRound.IsVotingOpen = true;


            // Open this question for voting
            db.Update(currentRound);
            await db.SaveChangesAsync();

            var numberQuestion = currentRound.Questions.First(x => x.Type == "number");

            await SendQuestionHub(data.GameLink, numberQuestion);
        }
    }
}

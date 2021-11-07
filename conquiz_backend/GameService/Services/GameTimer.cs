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
        private readonly IMapGeneratorService mapGeneratorService;

        // GameId<Game> | CurrentTimer 
        public static List<TimerWrapper> GameTimers = new List<TimerWrapper>();
        
        public GameTimer(IDbContextFactory<DefaultContext> _contextFactory, IHttpContextAccessor httpContextAccessor, IHubContext<GameHub, IGameHub> hubContext, IMapper mapper, IMapGeneratorService mapGeneratorService) : base(_contextFactory)
        {
            contextFactory = _contextFactory;
            this.httpContextAccessor = httpContextAccessor;
            this.hubContext = hubContext;
            this.mapper = mapper;
            this.mapGeneratorService = mapGeneratorService;
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

        private async Task Close_MultipleChoice_Natural_Territory_Selecting(TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();

            var currentRound = await db.Round
                .Include(x => x.NeutralRound)
                .ThenInclude(x => x.TerritoryAttackers)
                .ThenInclude(x => x.AttackedTerritory)
                .Where(x => x.GameRoundNumber == data.CurrentGameRoundNumber)
                .FirstOrDefaultAsync();

            currentRound.IsTerritoryVotingOpen = false;

            var notAnswered = currentRound.NeutralRound.TerritoryAttackers
                    .Where(x => x.AttackedTerritoryId == null).ToList();

            // Assign random territories to people who didn't choose anything.
            foreach (var userAttack in notAnswered)
            {
                var randomTerritory = await mapGeneratorService
                    .GetRandomMCTerritoryNeutral(userAttack.AttackerId, data.GameInstanceId);
                
                // Set this territory as being attacked from this person
                userAttack.AttackedTerritoryId = randomTerritory.Id;

                // Set the ObjectTerritory as being attacked currently
                userAttack.AttackedTerritory.IsAttacked = true;
            }
            db.Update(currentRound);
            await db.SaveChangesAsync();
        }

        private async Task Show_MultipleChoice_Screen(TimerWrapper timerWrapper, bool isNeutral)
        {
            // Stop timer until we calculate the next action and client event
            timerWrapper.Stop();

            // Get the question and show it to the clients
            var data = timerWrapper.Data;
            var db = contextFactory.CreateDbContext();

            // Show the question to the user
            var question = await db.Questions
                .Include(x => x.Answers)
                .Include(x => x.Round)
                .Where(x => x.Round.GameRoundNumber == data.CurrentGameRoundNumber
                    && x.Round.GameInstanceId == data.GameInstanceId)
                .FirstOrDefaultAsync();


            // Open this question for voting
            question.Round.IsQuestionVotingOpen = true;
            db.Update(question.Round);
            await db.SaveChangesAsync();

            //await SendQuestionHub(data.GameLink, question);

            var response = mapper.Map<QuestionClientResponse>(question);

            await hubContext.Clients.Group(data.GameLink).GetRoundQuestion(response);

            // If the round is a neutral one, then everyone can attack
            if(isNeutral)
            {
                await hubContext.Clients.Group(data.GameLink).CanPerformActions();
            }
            else
            {
                var participants = await db.PvpRounds
                    .Include(x => x.Round)
                    .Where(x => x.Round.GameRoundNumber == data.CurrentGameRoundNumber &&
                        x.Round.GameInstanceId == data.GameInstanceId)
                    .Select(x => new
                    {
                        x.AttackerId,
                        x.DefenderId
                    })
                    .FirstOrDefaultAsync();

                await hubContext.Clients.User(participants.AttackerId.ToString()).CanPerformActions();
                await hubContext.Clients.User(participants.DefenderId.ToString()).CanPerformActions();
            }
        }

        private async Task Close_MultipleChoice_Neutral_Voting(TimerWrapper timerWrapper)
        {
            // Can disable voting on start, however even 0-1s delay wouldn't be game breaking and would ease performance
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            var db = contextFactory.CreateDbContext();

            var currentRound =
                await db.Round
                .Include(x => x.Question)
                .ThenInclude(x => x.Answers)
                .Include(x => x.NeutralRound)
                .ThenInclude(x => x.TerritoryAttackers)
                .Where(x => x.GameRoundNumber == data.CurrentGameRoundNumber
                    && x.GameInstanceId == data.GameInstanceId)
                .FirstOrDefaultAsync();

            currentRound.IsQuestionVotingOpen = false;

            var playerCorrect = new Dictionary<int, bool>();

            foreach (var p in currentRound.NeutralRound.TerritoryAttackers)
            {
                var isThisPlayerAnswerCorrect =
                    currentRound.Question.Answers.FirstOrDefault(x => x.Id == p.AttackerMChoiceQAnswerId);

                // Player hasn't answered anything for this question, he loses
                if (isThisPlayerAnswerCorrect == null)
                {
                    p.AttackerWon = false;
                    p.AttackedTerritory.IsAttacked = false;
                    p.AttackedTerritory.TakenBy = null;
                    continue;
                }

                if (isThisPlayerAnswerCorrect.Correct)
                {
                    playerCorrect.Add(
                        p.AttackerId,
                        isThisPlayerAnswerCorrect.Correct
                    );

                    // Player answered correctly, he gets the territory
                    p.AttackerWon = true;
                    p.AttackedTerritory.IsAttacked = false;
                    p.AttackedTerritory.TakenBy = p.AttackerId;
                }
                else
                {
                    // Player answered incorrecly, release isattacked lock on objterritory
                    p.AttackerWon = false;
                    p.AttackedTerritory.IsAttacked = false;
                    p.AttackedTerritory.TakenBy = null;
                }
            }
        }

        private async Task Close_MultipleChoice_Pvp_Voting(TimerWrapper timerWrapper)
        {
            // Can disable voting on start, however even 0-1s delay wouldn't be game breaking and would ease performance
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            var db = contextFactory.CreateDbContext();

            var currentRound = 
                await db.Round
                .Include(x => x.Question)
                .ThenInclude(x => x.Answers)
                .Include(x => x.PvpRound)
                .ThenInclude(x => x.PvpRoundAnswers)
                .Where(x => x.GameRoundNumber == data.CurrentGameRoundNumber
                    && x.GameInstanceId == data.GameInstanceId)
                .FirstOrDefaultAsync();

            currentRound.IsQuestionVotingOpen = false;

            var playerCorrect = new Dictionary<int, bool>();

            // If attacker didn't win, we don't care what the outcome is
            var attackerAnswer = currentRound
                .PvpRound
                .PvpRoundAnswers
                .First(x => x.UserId == currentRound.PvpRound.AttackerId);


            // Attacker didn't answer, automatically loses
            if(attackerAnswer.MChoiceQAnswerId == null)
            {
                // Player answered incorrecly, release isattacked lock on objterritory
                currentRound.PvpRound.WinnerId = currentRound.PvpRound.DefenderId;
                currentRound.PvpRound.AttackedTerritory.IsAttacked = false;
            }
            else
            {

                var didAttackerAnswerCorrectly = currentRound
                    .Question
                    .Answers
                    .First(x => x.Id == attackerAnswer.MChoiceQAnswerId)
                    .Correct;

                if(!didAttackerAnswerCorrectly)
                {
                    // Player answered incorrecly, release isattacked lock on objterritory
                    currentRound.PvpRound.WinnerId = currentRound.PvpRound.DefenderId;
                    currentRound.PvpRound.AttackedTerritory.IsAttacked = false;
                }
                else
                {
                    var defenderAnswer = currentRound
                        .PvpRound
                        .PvpRoundAnswers
                        .First(x => x.UserId == currentRound.PvpRound.DefenderId);

                    // Defender didn't vote, he lost
                    if(defenderAnswer.MChoiceQAnswerId == null)
                    {
                        // Player answered incorrecly, release isattacked lock on objterritory
                        currentRound.PvpRound.WinnerId = currentRound.PvpRound.AttackerId;
                        currentRound.PvpRound.AttackedTerritory.IsAttacked = false;
                        currentRound.PvpRound.AttackedTerritory.TakenBy = currentRound.PvpRound.AttackerId;
                    }
                    else
                    {
                        // A new number question has to be shown
                        throw new NotImplementedException("A new number question has to be shown");
                    }
                }
            }

            db.Update(currentRound);
            await db.SaveChangesAsync();

            var qResult = new QuestionResultResponse()
            {
                Id = currentRound.Question.Id,
                Answers = mapper.Map<List<AnswerResultResponse>>(currentRound.Question.Answers),
                Question = currentRound.Question.Question,
                Type = currentRound.Question.Type,
                WinnerId = (int)currentRound.PvpRound.WinnerId,
            };

            await hubContext.Clients.Group(data.GameLink).PreviewResult(qResult);

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
    }
}

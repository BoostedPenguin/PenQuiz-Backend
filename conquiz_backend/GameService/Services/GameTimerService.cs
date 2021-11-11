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
    public interface IGameTimerService
    {
        void OnGameStart(GameInstance gm);
    }

    public enum ActionState
    {
        GAME_START_PREVIEW_TIME,

        // Rounding
        OPEN_PLAYER_ATTACK_VOTING,
        CLOSE_PLAYER_ATTACK_VOTING,


        // Open MC Question
        SHOW_MULTIPLE_CHOICE_NEUTRAL_TERRITORY_SCREEN,
        OPEN_MULTIPLE_CHOICE_NEUTRAL_TERRITORY_SELECTING,
        CLOSE_MULTIPLE_CHOICE_NEUTRAL_TERRITORY_SELECTING,
    }

    /// <summary>
    /// Handles all game instance timers and callbacks the appropriate GameControlService functions
    /// </summary>
    public class GameTimerService : DataService<DefaultModel>, IGameTimerService
    {
        
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        private readonly IMapper mapper;
        private readonly IMapGeneratorService mapGeneratorService;

        // GameId<Game> | CurrentTimer 
        public static List<TimerWrapper> GameTimers = new List<TimerWrapper>();
        private readonly int ServerClientTimeOffset = 1000;
        public GameTimerService(IDbContextFactory<DefaultContext> _contextFactory, IHttpContextAccessor httpContextAccessor, IHubContext<GameHub, IGameHub> hubContext, IMapper mapper, IMapGeneratorService mapGeneratorService) : base(_contextFactory)
        {
            contextFactory = _contextFactory;
            this.httpContextAccessor = httpContextAccessor;
            this.hubContext = hubContext;
            this.mapper = mapper;
            this.mapGeneratorService = mapGeneratorService;
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
                public ActionState NextAction { get; set; }
            }
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

        public enum TestStates
        {
            GAME_START_PREVIEW_TIME,

            // 1st round
            OPEN_FIRST_PLAYER_ATTACK_VOTING,
            CLOSE_FIRST_PLAYER_ATTACK_VOTING,
            // Game preview = 3s
            OPEN_SECOND_PLAYER_ATTACK_VOTING,
            CLOSE_SECOND_PLAYER_ATTACK_VOTING,
            // Game preview = 3s
            OPEN_THIRD_PLAYER_ATTACK_VOTING,
            CLOSE_THIRD_PLAYER_ATTACK_VOTING,
            // Game preview = 3s


            // Open MC Question
            SHOW_MULTIPLE_CHOICE_NEUTRAL_TERRITORY_SCREEN,
            OPEN_MULTIPLE_CHOICE_NEUTRAL_TERRITORY_SELECTING,
            CLOSE_MULTIPLE_CHOICE_NEUTRAL_TERRITORY_SELECTING,
        }


        public void OnGameStart(GameInstance gm)
        {
            if(GameTimers.FirstOrDefault(x => x.Data.GameInstanceId == gm.Id) != null)
                throw new ArgumentException("Timer already exists for this game instance");

            var actionTimer = new TimerWrapper(gm.Id, gm.InvitationLink)
            {
                AutoReset = false,
                Interval = 500,
            };
            
            // Default starter values
            actionTimer.Data.NextAction = ActionState.GAME_START_PREVIEW_TIME;
            actionTimer.Data.CurrentGameRoundNumber = 1;

            GameTimers.Add(actionTimer);

            // Start timer
            actionTimer.Elapsed += ActionTimer_Elapsed;
            actionTimer.Start();
        }

        private async void ActionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Stop the timer when it elapses.
            // Start it again after an action is complete
            var timer = (TimerWrapper)sender;
            timer.Stop();

            switch (timer.Data.NextAction)
            {
                case ActionState.GAME_START_PREVIEW_TIME:
                    
                    // Send request to clients to stay on main screen for preview
                    await Game_Preview_Time(timer);

                    // Set next action
                    timer.Data.NextAction = ActionState.OPEN_PLAYER_ATTACK_VOTING;

                    // Set time until next action *call case state*
                    timer.Interval = GameActionsTime.GetServerActionsTime(ActionState.GAME_START_PREVIEW_TIME);
                    
                    // Restart timer
                    timer.Start();
                    return;
                case ActionState.OPEN_PLAYER_ATTACK_VOTING:

                    // Send request to clients to open the multiple choice voting
                    // And show whos attacking turn it is
                    await Open_MultipleChoice_Attacker_Territory_Selecting(timer);
                    return;

                case ActionState.CLOSE_PLAYER_ATTACK_VOTING:
                    await Close_MultipleChoice_Attacker_Territory_Selecting(timer);
                    return;
            }
        }

        private async Task UnexpectedCriticalError(TimerWrapper timerWrapper, string message = "Unhandled game exception")
        {
            var db = contextFactory.CreateDbContext();
            var gm = await db.GameInstance.FirstOrDefaultAsync(x => x.Id == timerWrapper.Data.GameInstanceId);
            gm.GameState = GameState.CANCELED;

            await db.SaveChangesAsync();

            await hubContext.Clients.Group(timerWrapper.Data.GameLink).LobbyCanceled("Unexpected error occured. Game closed.");
            GameTimers.Remove(timerWrapper);
            throw new Exception(message);
        }

        private async Task Game_Preview_Time(TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;
            await hubContext.Clients.Group(data.GameLink)
                .Game_Show_Main_Screen();
        }

        private async Task Open_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();

            var currentRound = await db.Round
                .Include(x => x.NeutralRound)
                .ThenInclude(x => x.TerritoryAttackers)
                .ThenInclude(x => x.AttackedTerritory)
                .Where(x => x.GameRoundNumber == data.CurrentGameRoundNumber && x.GameInstanceId == data.GameInstanceId)
                .FirstOrDefaultAsync();

            // Open this round for territory voting
            currentRound.IsTerritoryVotingOpen = true;

            db.Update(currentRound);
            await db.SaveChangesAsync();

            var currentAttacker = currentRound.NeutralRound.TerritoryAttackers
                .First(x => x.AttackOrderNumber == currentRound.NeutralRound.AttackOrderNumber);

            await hubContext.Clients.Group(data.GameLink)
                .ShowRoundingAttacker(currentAttacker.AttackerId,
                    GameActionsTime.GetServerActionsTime(ActionState.GAME_START_PREVIEW_TIME) - ServerClientTimeOffset);

            // Set next action and interval
            timerWrapper.Data.NextAction = ActionState.CLOSE_PLAYER_ATTACK_VOTING;
            timerWrapper.Interval = GameActionsTime.GetServerActionsTime(ActionState.OPEN_PLAYER_ATTACK_VOTING);

            timerWrapper.Start();
        }

        /// <summary>
        /// When a player's timeframe for attacking neutral territory expires
        /// Close off the voting for him. If he hasn't selected anything, give him a random selected territory
        /// Start next player territory select timer
        /// </summary>
        /// <returns></returns>
        private async Task Close_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();

            var currentRound = await db.Round
                .Include(x => x.NeutralRound)
                .ThenInclude(x => x.TerritoryAttackers)
                .ThenInclude(x => x.AttackedTerritory)
                .Where(x => x.GameRoundNumber == data.CurrentGameRoundNumber && x.GameInstanceId == data.GameInstanceId)
                .FirstOrDefaultAsync();

            var currentAttacker = currentRound.NeutralRound.TerritoryAttackers
                .First(x => x.AttackOrderNumber == currentRound.NeutralRound.AttackOrderNumber);

            // Player didn't select anything, assign him a random UNSELECTED territory
            if(currentAttacker.AttackedTerritoryId == null)
            {
                var randomTerritory =
                    await mapGeneratorService.GetRandomMCTerritoryNeutral(currentAttacker.AttackerId, data.GameInstanceId);

                // Set this territory as being attacked from this person
                currentAttacker.AttackedTerritoryId = randomTerritory.Id;

                // Set the ObjectTerritory as being attacked currently
                randomTerritory.IsAttacked = true;
            }

            // This attacker voting is over, go to next attacker
            switch(currentRound.NeutralRound.AttackOrderNumber)
            {
                case 1:
                case 2:
                    var newAttackorderNumber = ++currentRound.NeutralRound.AttackOrderNumber;
                    var nextAttacker = 
                        currentRound.NeutralRound.TerritoryAttackers
                        .First(x => x.AttackOrderNumber == newAttackorderNumber);

                    db.Update(currentRound);
                    await db.SaveChangesAsync();

                    // Notify the group who is the next attacker
                    await hubContext.Clients.Group(data.GameLink).ShowRoundingAttacker(nextAttacker.AttackerId,
                        GameActionsTime.GetServerActionsTime(ActionState.GAME_START_PREVIEW_TIME) - ServerClientTimeOffset);

                    // Set the next timer event
                    timerWrapper.Data.NextAction = ActionState.CLOSE_PLAYER_ATTACK_VOTING;
                    timerWrapper.Interval = GameActionsTime.GetServerActionsTime(ActionState.OPEN_PLAYER_ATTACK_VOTING);

                    timerWrapper.Start();

                    break;

                // This was the last attacker, show a preview of all territories now, and then show the questions
                case 3:
                    db.Update(currentRound);
                    await db.SaveChangesAsync();
                    throw new NotImplementedException();

                default:
                    await UnexpectedCriticalError(timerWrapper, 
                        $"Unexpected attackordernumber value: {currentRound.NeutralRound.AttackOrderNumber}");
                    
                    return;
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

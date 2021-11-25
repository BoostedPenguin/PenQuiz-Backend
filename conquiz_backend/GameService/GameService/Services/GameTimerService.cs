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
using GameService.MessageBus;

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
        SHOW_MULTIPLE_CHOICE_QUESTION,
        END_MULTIPLE_CHOICE_QUESTION,
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
        private readonly IMessageBusClient messageBus;
        private readonly IGameTerritoryService gameTerritoryService;

        // GameId<Game> | CurrentTimer 
        public static List<TimerWrapper> GameTimers = new List<TimerWrapper>();
        public GameTimerService(IDbContextFactory<DefaultContext> _contextFactory, 
            IHttpContextAccessor httpContextAccessor, 
            IHubContext<GameHub, IGameHub> hubContext, 
            IMapper mapper,
            IMessageBusClient messageBus,
            IGameTerritoryService gameTerritoryService) : base(_contextFactory)
        {
            contextFactory = _contextFactory;
            this.httpContextAccessor = httpContextAccessor;
            this.hubContext = hubContext;
            this.mapper = mapper;
            this.messageBus = messageBus;
            this.gameTerritoryService = gameTerritoryService;
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
                public int LastNeutralMCRound { get; set; }

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

            // Set the last neutral mc round 
            using var db = contextFactory.CreateDbContext();
            actionTimer.Data.LastNeutralMCRound = db.Round
                .Where(x => x.GameInstanceId == gm.Id && x.AttackStage == AttackStage.MULTIPLE_NEUTRAL)
                .Count();

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

                case ActionState.SHOW_MULTIPLE_CHOICE_QUESTION:
                    await Show_MultipleChoice_Screen(timer, true);
                    return;

                case ActionState.END_MULTIPLE_CHOICE_QUESTION:
                    await Close_MultipleChoice_Neutral_Question_Voting(timer);
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

        public static async Task<GameInstance> GetFullGameInstance(int gameInstanceId, DefaultContext defaultContext)
        {
            var game = await defaultContext.GameInstance
                .Include(x => x.Participants)
                .ThenInclude(x => x.Player)
                .Include(x => x.Rounds)
                .ThenInclude(x => x.NeutralRound)
                .ThenInclude(x => x.TerritoryAttackers)
                .Include(x => x.ObjectTerritory)
                .ThenInclude(x => x.MapTerritory)
                .FirstOrDefaultAsync(x => x.Id == gameInstanceId);

            game.Rounds = game.Rounds.OrderBy(x => x.GameRoundNumber).ToList();

            var ss = game.Rounds.OrderBy(x => x.GameRoundNumber).ToList();
            foreach(var round in game.Rounds)
            {
                round.NeutralRound.TerritoryAttackers = 
                    round.NeutralRound.TerritoryAttackers.OrderBy(x => x.AttackOrderNumber).ToList();
            }


            return game;
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

            var availableTerritories = await gameTerritoryService
                .GetAvailableAttackTerritoriesNames(db, currentAttacker.AttackerId, currentRound.GameInstanceId, true);

            await hubContext.Clients.Group(data.GameLink)
                .ShowRoundingAttacker(currentAttacker.AttackerId,
                    GameActionsTime.GetServerActionsTime(ActionState.OPEN_PLAYER_ATTACK_VOTING), availableTerritories);

            var fullGame = await GetFullGameInstance(data.GameInstanceId, db);
            await hubContext.Clients.Group(data.GameLink)
                .GetGameInstance(fullGame);

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
                    await gameTerritoryService.GetRandomMCTerritoryNeutral(currentAttacker.AttackerId, data.GameInstanceId);

                // Set this territory as being attacked from this person
                currentAttacker.AttackedTerritoryId = randomTerritory.Id;

                // Set the ObjectTerritory as being attacked currently
                randomTerritory.AttackedBy = currentAttacker.AttackerId;
                db.Update(randomTerritory);
            }
            var fullGame = await GetFullGameInstance(data.GameInstanceId, db);

            // This attacker voting is over, go to next attacker
            switch (currentRound.NeutralRound.AttackOrderNumber)
            {
                case 1:
                case 2:
                    var newAttackorderNumber = ++currentRound.NeutralRound.AttackOrderNumber;
                    var nextAttacker = 
                        currentRound.NeutralRound.TerritoryAttackers
                        .First(x => x.AttackOrderNumber == newAttackorderNumber);

                    db.Update(currentRound);
                    await db.SaveChangesAsync();

                    var availableTerritories = await gameTerritoryService
                        .GetAvailableAttackTerritoriesNames(db, nextAttacker.AttackerId, currentRound.GameInstanceId, true);

                    // Notify the group who is the next attacker
                    await hubContext.Clients.Group(data.GameLink).ShowRoundingAttacker(nextAttacker.AttackerId,
                        GameActionsTime.GetServerActionsTime(ActionState.OPEN_PLAYER_ATTACK_VOTING), availableTerritories);

                    await hubContext.Clients.Group(data.GameLink)
                        .GetGameInstance(fullGame);

                    // Set the next timer event
                    timerWrapper.Data.NextAction = ActionState.CLOSE_PLAYER_ATTACK_VOTING;
                    timerWrapper.Interval = GameActionsTime.GetServerActionsTime(ActionState.OPEN_PLAYER_ATTACK_VOTING);

                    timerWrapper.Start();

                    break;

                // This was the last attacker, show a preview of all territories now, and then show the questions
                case 3:
                    currentRound.IsTerritoryVotingOpen = false;
                    db.Update(currentRound);
                    await db.SaveChangesAsync();

                    await hubContext.Clients.Group(data.GameLink)
                        .GetGameInstance(fullGame);


                    timerWrapper.Interval = GameActionsTime.DefaultPreviewTime;
                    timerWrapper.Data.NextAction = ActionState.SHOW_MULTIPLE_CHOICE_QUESTION;
                    timerWrapper.Start();
                    break;
                    //throw new NotImplementedException();

                default:
                    await UnexpectedCriticalError(timerWrapper, 
                        $"Unexpected attackordernumber value: {currentRound.NeutralRound.AttackOrderNumber}");
                    
                    return;
            }
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
                .ThenInclude(x => x.GameInstance)
                .ThenInclude(x => x.Participants)
                .Where(x => x.Round.GameInstanceId == data.GameInstanceId &&
                    x.Round.GameRoundNumber == x.Round.GameInstance.GameRoundNumber)
                .FirstOrDefaultAsync();


            // Open this question for voting
            question.Round.IsQuestionVotingOpen = true;
            db.Update(question.Round);
            await db.SaveChangesAsync();

            //await SendQuestionHub(data.GameLink, question);

            var response = mapper.Map<QuestionClientResponse>(question);

            // If the round is a neutral one, then everyone can attack
            if(isNeutral)
            {
                response.IsNeutral = true;
                response.Participants = question.Round.GameInstance.Participants.ToArray();

                await hubContext.Clients.Group(data.GameLink).GetRoundQuestion(response,
                    GameActionsTime.GetServerActionsTime(ActionState.SHOW_MULTIPLE_CHOICE_QUESTION));
            }
            else
            {
                response.IsNeutral = false;

                var participants = await db.PvpRounds
                    .Include(x => x.Round)
                    .ThenInclude(x => x.GameInstance)
                    .ThenInclude(x => x.Participants)
                    .Where(x => x.Round.GameRoundNumber == data.CurrentGameRoundNumber &&
                        x.Round.GameInstanceId == data.GameInstanceId)
                    .Select(x => new
                    {
                        Participants = x.Round.GameInstance.Participants
                            .Where(y => y.PlayerId == x.AttackerId || y.PlayerId == x.DefenderId)
                            .ToArray(),
                        x.AttackerId,
                        x.DefenderId,
                    })
                    .FirstOrDefaultAsync();

                response.Participants = participants.Participants;
                response.AttackerId = participants.AttackerId;
                response.DefenderId = participants.DefenderId ?? 0;


                await hubContext.Clients.Group(data.GameLink).GetRoundQuestion(response,
                    GameActionsTime.GetServerActionsTime(ActionState.SHOW_MULTIPLE_CHOICE_QUESTION));
            }

            timerWrapper.Data.NextAction = ActionState.END_MULTIPLE_CHOICE_QUESTION;
            timerWrapper.Interval = GameActionsTime.GetServerActionsTime(ActionState.SHOW_MULTIPLE_CHOICE_QUESTION);
            timerWrapper.Start();
        }

        private async Task Close_MultipleChoice_Neutral_Question_Voting(TimerWrapper timerWrapper)
        {
            // Can disable voting on start, however even 0-1s delay wouldn't be game breaking and would ease performance
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            var db = contextFactory.CreateDbContext();

            var currentRound =
                await db.Round
                .Include(x => x.GameInstance)
                .Include(x => x.Question)
                .ThenInclude(x => x.Answers)
                .Include(x => x.NeutralRound)
                .ThenInclude(x => x.TerritoryAttackers)
                .ThenInclude(x => x.AttackedTerritory)
                .Where(x => x.GameRoundNumber == data.CurrentGameRoundNumber
                    && x.GameInstanceId == data.GameInstanceId)
                .FirstOrDefaultAsync();

            

            currentRound.IsQuestionVotingOpen = false;

            var playerIdAnswerId = new Dictionary<int, int>();

            foreach (var p in currentRound.NeutralRound.TerritoryAttackers)
            {
                var isThisPlayerAnswerCorrect =
                    currentRound.Question.Answers.FirstOrDefault(x => x.Id == p.AttackerMChoiceQAnswerId);

                // Player hasn't answered anything for this question, he loses
                if (isThisPlayerAnswerCorrect == null)
                {
                    p.AttackerWon = false;
                    p.AttackedTerritory.AttackedBy = null;
                    p.AttackedTerritory.TakenBy = null;


                    playerIdAnswerId.Add(
                        p.AttackerId,
                        0
                    );
                    continue;
                }

                if (isThisPlayerAnswerCorrect.Correct)
                {
                    // Player answered correctly, he gets the territory
                    p.AttackerWon = true;
                    p.AttackedTerritory.AttackedBy = null;
                    p.AttackedTerritory.TakenBy = p.AttackerId;
                }
                else
                {
                    // Player answered incorrecly, release isattacked lock on objterritory
                    p.AttackerWon = false;
                    p.AttackedTerritory.AttackedBy = null;
                    p.AttackedTerritory.TakenBy = null;
                }

                playerIdAnswerId.Add(
                    p.AttackerId,
                    p.AttackerMChoiceQAnswerId ?? 0
                );
            }

            // Create number question rounds if gm multiple choice neutral rounds are over
            Round[] rounds = null;
            if(currentRound.GameRoundNumber == data.LastNeutralMCRound)
            {
                rounds = await Create_Neutral_Number_Rounds(db, timerWrapper);
                await db.AddRangeAsync(rounds);
            }

            // Go to next round
            timerWrapper.Data.CurrentGameRoundNumber++;
            currentRound.GameInstance.GameRoundNumber = timerWrapper.Data.CurrentGameRoundNumber;

            db.Update(currentRound);
            await db.SaveChangesAsync();

            // Request a new batch of number questions from the question service
            if(currentRound.GameRoundNumber == data.LastNeutralMCRound)
            {
                RequestQuestions(data.GameInstanceId, rounds, true);
            }

            // Response correct answer and all player answers
            var response = new PlayerQuestionAnswers()
            {
                CorrectAnswerId = currentRound.Question.Answers.FirstOrDefault(x => x.Correct).Id,
                PlayerAnswers = new List<PlayerIdAnswerId>(),
            };

            foreach(var pId in playerIdAnswerId)
            {
                response.PlayerAnswers.Add(new PlayerIdAnswerId()
                {
                    Id = pId.Key,
                    AnswerId = pId.Value,
                });
            }

            await hubContext.Clients.Groups(data.GameLink).QuestionPreviewResult(response);

            timerWrapper.Data.NextAction = ActionState.OPEN_PLAYER_ATTACK_VOTING;
            timerWrapper.Interval = GameActionsTime.DefaultPreviewTime;

            timerWrapper.Start();
        }

        private void RequestQuestions(int gameInstanceId, Round[] rounds, bool isNeutralGeneration = false)
        {
            // Request questions only for the initial multiple questions for neutral attacking order
            // After multiple choices are over, request a new batch for number questions for all untaken territories
            messageBus.RequestQuestions(new RequestQuestionsDto()
            {
                Event = "Question_Request",
                GameInstanceId = gameInstanceId,
                MultipleChoiceQuestionsRoundId = new List<int>(),
                NumberQuestionsRoundId = rounds.Select(x => x.Id).ToList(),
                IsNeutralGeneration = isNeutralGeneration,
            });
        }

        private async Task<Round[]> Create_Neutral_Number_Rounds(DefaultContext db, TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;
            var untakenTerritoriesCount = await db.ObjectTerritory
                .Where(x => x.TakenBy == null && x.GameInstanceId == data.GameInstanceId)
                .CountAsync();

            var participantsIds = await db.Participants
                .Where(x => x.GameId == data.GameInstanceId)
                .Select(x => x.PlayerId)
                .ToListAsync();

            var numberQuestionRounds = new List<Round>();
            int baseDebug = 5;
            for(var i = 0; i < untakenTerritoriesCount; i++)
            {
                var baseRound = new Round()
                {
                    // After this method executes we switch to the next round automatically thus + 1 now
                    GameRoundNumber = baseDebug + i + 1,
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

                foreach(var participId in participantsIds)
                {
                    baseRound.NeutralRound.TerritoryAttackers.Add(new AttackingNeutralTerritory()
                    {
                        AttackerId = participId,
                        AttackOrderNumber = 0,
                    });
                }

                numberQuestionRounds.Add(baseRound);
            }

            return numberQuestionRounds.ToArray();
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
                currentRound.PvpRound.AttackedTerritory.AttackedBy = null;
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
                    currentRound.PvpRound.AttackedTerritory.AttackedBy = null;
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
                        currentRound.PvpRound.AttackedTerritory.AttackedBy = null;
                        currentRound.PvpRound.AttackedTerritory.TakenBy = currentRound.PvpRound.AttackerId;
                    }
                    else
                    {
                        // A new number question has to be shown
                        throw new NotImplementedException("A new number question has to be shown");
                    }
                }
            }

            //db.Update(currentRound);
            //await db.SaveChangesAsync();

            //var qResult = new QuestionResultResponse()
            //{
            //    Id = currentRound.Question.Id,
            //    Answers = mapper.Map<List<AnswerResultResponse>>(currentRound.Question.Answers),
            //    Question = currentRound.Question.Question,
            //    Type = currentRound.Question.Type,
            //    WinnerId = (int)currentRound.PvpRound.WinnerId,
            //};

            //await hubContext.Clients.Group(data.GameLink).PreviewResult(qResult);

            //timerWrapper.Interval = 3000;
            //timerWrapper.Start();
        }
    }
}

using AutoMapper;
using GameService.Context;
using GameService.Data;
using GameService.Data.Models;
using GameService.Dtos;
using GameService.Dtos.SignalR_Responses;
using GameService.Hubs;
using GameService.MessageBus;
using GameService.Services.GameTimerServices.NeutralTimerServices;
using GameService.Services.GameTimerServices.PvpTimerServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace GameService.Services.GameTimerServices
{
    public interface IGameTimerService
    {
        List<TimerWrapper> GameTimers { get;}

        void OnGameStart(GameInstance gm);
        void CancelGameTimer(GameInstance gm);
    }

    public class GameTimerService : IGameTimerService
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly INeutralMCTimerEvents neutralMCTimerEvents;
        private readonly ICapitalStageTimerEvents capitalStageTimerEvents;
        private readonly IFinalPvpQuestionService finalPvpQuestionService;
        private readonly ILogger<GameTimerService> logger;
        private readonly IMapper mapper;

        private readonly INeutralNumberTimerEvents neutralNumberTimerEvents;
        private readonly IPvpStageTimerEvents pvpStageTimerEvents;
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        public List<TimerWrapper> GameTimers { get; } = new();
        private readonly IMessageBusClient messageBus;

        private void RequestQuestions(string gameGlobalIdentifier, Round[] rounds, bool isNeutralGeneration = false)
        {
            // Request questions only for the initial multiple questions for neutral attacking order
            // After multiple choices are over, request a new batch for number questions for all untaken territories
            messageBus.RequestQuestions(new RequestQuestionsDto()
            {
                Event = "Question_Request",
                GameGlobalIdentifier = gameGlobalIdentifier,
                MultipleChoiceQuestionsRoundId = rounds
                    .Where(x => x.AttackStage == AttackStage.MULTIPLE_NEUTRAL)
                    .Select(x => x.Id)
                    .ToList(),
                NumberQuestionsRoundId = new List<int>(),
                IsNeutralGeneration = isNeutralGeneration,
            });
        }

        public GameTimerService(IDbContextFactory<DefaultContext> _contextFactory,
            IPvpStageTimerEvents pvpStageTimerEvents,
            IHubContext<GameHub, IGameHub> hubContext,
            INeutralMCTimerEvents neutralMCTimerEvents,
            IMessageBusClient messageBus,
            ICapitalStageTimerEvents capitalStageTimerEvents,
            IFinalPvpQuestionService finalPvpQuestionService,
            ILogger<GameTimerService> logger,
            IMapper mapper,
            INeutralNumberTimerEvents neutralNumberTimerEvents)
        {
            contextFactory = _contextFactory;
            this.pvpStageTimerEvents = pvpStageTimerEvents;
            this.hubContext = hubContext;
            this.neutralMCTimerEvents = neutralMCTimerEvents;
            this.messageBus = messageBus;
            this.capitalStageTimerEvents = capitalStageTimerEvents;
            this.finalPvpQuestionService = finalPvpQuestionService;
            this.logger = logger;
            this.mapper = mapper;
            this.neutralNumberTimerEvents = neutralNumberTimerEvents;
        }

        public void OnGameStart(GameInstance gm)
        {
            if (GameTimers.FirstOrDefault(x => x.Data.GameInstanceId == gm.Id) != null)
                throw new ArgumentException("Timer already exists for this game instance");

            var actionTimer = new TimerWrapper(gm.Id, gm.InvitationLink, gm.GameGlobalIdentifier)
            {
                AutoReset = false,
                Interval = 500,
            };
            actionTimer.Data.CountDownTimer = new TimerWrapper.CountDownTimer(hubContext, actionTimer.Data.GameLink);

            // Set the last neutral mc round 
            actionTimer.Data.LastNeutralMCRound = gm.Rounds.Where(x => x.AttackStage == AttackStage.MULTIPLE_NEUTRAL).Count();

            // Default starter values
            actionTimer.Data.GameInstance = gm;

            GameTimers.Add(actionTimer);

            // Start timer
            actionTimer.Elapsed += ActionTimer_Elapsed;

            // Request initial multiple choice questions from question service
            RequestQuestions(gm.GameGlobalIdentifier, gm.Rounds.ToArray(), true);


            actionTimer.StartTimer(ActionState.GAME_START_PREVIEW_TIME, 50);
        }

        private async void ActionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Stop the timer when it elapses.
            // Start it again after an action is complete
            var timer = (TimerWrapper)sender;
            timer.Stop();



            try
            {
                switch (timer.Data.NextAction)
                {
                    case ActionState.GAME_START_PREVIEW_TIME:

                        /*
                         * Send request to clients to stay on main screen for preview
                         * 
                         * - Next event will be ActionState.OPEN_PLAYER_ATTACK_VOTING
                         */

                        //await Game_Preview_Time(timer);

                        // Debug
                        await neutralNumberTimerEvents.Debug_Assign_All_Territories_Start_Pvp(timer);
                        //await neutralMCTimerEvents.Debug_Start_Number_Neutral(timer);
                        return;

                    #region Neutral Multiple Choice events
                    case ActionState.OPEN_PLAYER_ATTACK_VOTING:

                        /*
                         * Send request to clients to open the multiple choice territory voting for a specific person
                         * Show whos attacking turn it is
                         * 
                         * - Next event will be ActionState.CLOSE_PLAYER_ATTACK_VOTING
                         */

                        await neutralMCTimerEvents
                            .Open_Neutral_MultipleChoice_Attacker_Territory_Selecting(timer);
                        return;

                    case ActionState.CLOSE_PLAYER_ATTACK_VOTING:

                        /*
                         * Send request to clients to close the multiple choice voting for a specific person
                         * Find next attacker and display him
                         * 
                         * - If this was last attacker, next event will be ActionState.SHOW_MULTIPLE_CHOICE_QUESTION
                         * - If this was not last attacker, next event will be ActionState.OPEN_PLAYER_ATTACK_VOTING
                         */

                        await neutralMCTimerEvents
                            .Close_Neutral_MultipleChoice_Attacker_Territory_Selecting(timer);
                        return;

                    case ActionState.SHOW_MULTIPLE_CHOICE_QUESTION:

                        /*
                         * Get the question and display it to the users
                         * - Next event will be ActionState.END_MULTIPLE_CHOICE_QUESTION
                         */

                        await neutralMCTimerEvents
                            .Show_Neutral_MultipleChoice_Screen(timer);
                        return;

                    case ActionState.END_MULTIPLE_CHOICE_QUESTION:

                        /*
                         * Check all player answers if they won a neutral territory or not
                         * 
                         * - If this was last fixed MC neutral territory, 
                         * next event will be ActionState.SHOW_PREVIEW_GAME_MAP
                         * 
                         * - If this wasn't last fixed MC neutral territory, 
                         * next event will be ActionState.OPEN_PLAYER_ATTACK_VOTING
                         */

                        await neutralMCTimerEvents
                            .Close_Neutral_MultipleChoice_Question_Voting(timer);
                        return;
                    #endregion

                    #region Neutral Number Events
                    case ActionState.SHOW_PREVIEW_GAME_MAP:

                        /*
                         * Prepares users for number questions (Blitz rounds)
                         * - Next event will be ActionState.SHOW_NUMBER_QUESTION
                         */

                        await neutralNumberTimerEvents.Show_Game_Map_Screen(timer);
                        return;

                    case ActionState.SHOW_NUMBER_QUESTION:

                        /*
                         * Get the blitz question and display it to the users
                         * - Next event will be ActionState.END_NUMBER_QUESTION
                         */

                        await neutralNumberTimerEvents.Show_Neutral_Number_Screen(timer);
                        return;

                    case ActionState.END_NUMBER_QUESTION:

                        /*
                         * Checks users answers for the winner
                         * 
                         * - If this was last neutral number question, 
                         * next event will be ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING
                         * 
                         * - If this wasn't last neutral number question, 
                         * next event will be ActionState.SHOW_PREVIEW_GAME_MAP
                         */

                        await neutralNumberTimerEvents.Close_Neutral_Number_Question_Voting(timer);
                        return;
                    #endregion

                    #region Pvp Events
                    case ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING:

                        /*
                         * Send request to clients to open the multiple choice pvp territory voting for a specific person
                         * And show whos attacking turn it is
                         * 
                         * - Next event will be ActionState.CLOSE_PVP_PLAYER_ATTACK_VOTING
                         */

                        await pvpStageTimerEvents.Open_Pvp_MultipleChoice_Attacker_Territory_Selecting(timer);
                        return;

                    case ActionState.CLOSE_PVP_PLAYER_ATTACK_VOTING:

                        /*
                         * Send request to clients to close the multiple choice territory pvp voting for a specific person
                         * 
                         * - Next event will be ActionState.SHOW_PVP_MULTIPLE_CHOICE_QUESTION
                         */

                        await pvpStageTimerEvents.Close_Pvp_MultipleChoice_Attacker_Territory_Selecting(timer);
                        return;

                    case ActionState.SHOW_PVP_MULTIPLE_CHOICE_QUESTION:

                        /*
                         * Get the multiple choice question and display it to the users
                         * Only 2 players will be able to answer (1v1 pvp)
                         * 
                         * - Next event will be ActionState.END_PVP_MULTIPLE_CHOICE_QUESTION:
                         */

                        await pvpStageTimerEvents.Show_Pvp_MultipleChoice_Screen(timer);
                        return;

                    case ActionState.END_PVP_MULTIPLE_CHOICE_QUESTION:

                        /*
                         * Determine the pvp mc winner based on user answers
                         * 
                         * - If both users answered correctly, next event will be ActionState.SHOW_PVP_NUMBER_QUESTION
                         * 
                         * - If attacker won, defender lost and territory attacked is 
                         * capital, next event will be ActionState.SHOW_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION
                         * 
                         * - If this is last multiple choice question, not capital and scores of at least
                         * 2 users are same, next event will be ActionState.SHOW_FINAL_PVP_NUMBER_QUESTION
                         * 
                         * - If this is last multiple choice question, not capital and scores of users are unique
                         * next event will be ActionState.END_GAME
                         */

                        await pvpStageTimerEvents.Close_Pvp_MultipleChoice_Question_Voting(timer);
                        return;

                    case ActionState.SHOW_PVP_NUMBER_QUESTION:

                        /*
                         * Get the number question and display it to the users
                         * Only 2 players will be able to answer (1v1 pvp)
                         * 
                         * - Next event will be ActionState.END_PVP_NUMBER_QUESTION
                         */

                        await pvpStageTimerEvents.Show_Pvp_Number_Screen(timer);
                        return;

                    case ActionState.END_PVP_NUMBER_QUESTION:

                        /*
                         * Determine the pvp number question winner based on user answers
                         * 
                         * - If attacker won and territory is capital, 
                         * next event will be ActionState.SHOW_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION
                         * 
                         * - If this is last question, not capital and scores of at least
                         * 2 users are same, next event will be ActionState.SHOW_FINAL_PVP_NUMBER_QUESTION
                         * 
                         * - If this is last question, not capital and scores of users are unique
                         * next event will be ActionState.END_GAME
                         */

                        await pvpStageTimerEvents.Close_Pvp_Number_Question_Voting(timer);
                        return;
                    #endregion

                    #region CapitalPvpRoundStages
                    case ActionState.SHOW_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION:

                        /*
                         * Get the MC capital question and display it to the users
                         * Only 2 players will be able to answer (1v1 pvp)
                         * 
                         * - Next event will be ActionState.END_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION
                         */

                        await capitalStageTimerEvents.Capital_Show_Pvp_MultipleChoice_Question_Voting(timer);
                        return;

                    case ActionState.END_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION:

                        /*
                         * Determine the pvp capital MC question winner based on user answers
                         * 
                         * - If either won and this isn't last question
                         * next event will be ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING
                         * 
                         * - If either won and this is last question and scores of at least 2 users are same
                         * next event will be ActionState.SHOW_FINAL_PVP_NUMBER_QUESTION
                         * 
                         * - If either won and this is last question and scores of users are unique
                         * next event will be ActionState.END_GAME
                         * 
                         * - If both users won, next event will be ActionState.SHOW_CAPITAL_PVP_NUMBER_QUESTION
                         */

                        await capitalStageTimerEvents.Capital_Close_Pvp_MultipleChoice_Question_Voting(timer);
                        return;

                    case ActionState.SHOW_CAPITAL_PVP_NUMBER_QUESTION:

                        /*
                         * Get the number capital question and display it to the users
                         * Only 2 players will be able to answer (1v1 pvp)
                         * 
                         * - Next event will be ActionState.END_CAPITAL_PVP_NUMBER_QUESTION
                         */

                        await capitalStageTimerEvents.Capital_Show_Pvp_Number_Screen(timer);
                        return;

                    case ActionState.END_CAPITAL_PVP_NUMBER_QUESTION:

                        /*
                         * Determine the pvp capital number question winner based on user answers
                         * 
                         * - If either won and this isn't last question
                         * next event will be ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING
                         * 
                         * - If either won and this is last question and scores of at least 2 users are same
                         * next event will be ActionState.SHOW_FINAL_PVP_NUMBER_QUESTION
                         * 
                         * - If either won and this is last question and scores of users are unique
                         * next event will be ActionState.END_GAME
                         */

                        await capitalStageTimerEvents.Capital_Close_Pvp_Number_Question_Voting(timer);
                        return;
                    #endregion

                    #region FinalPvpStageQuestion
                    case ActionState.SHOW_FINAL_PVP_NUMBER_QUESTION:

                        /*
                         * Get the final number capital question and display it to the users
                         * 2-3 players will answer depending on their scoring
                         * 
                         * - Next event will be ActionState.END_FINAL_PVP_NUMBER_QUESTION
                         */

                        await finalPvpQuestionService.Final_Show_Pvp_Number_Screen(timer);
                        return;

                    case ActionState.END_FINAL_PVP_NUMBER_QUESTION:

                        /*
                         * Determine the pvp number final question winner based on user answers
                         * 
                         * Next event will be ActionState.END_GAME
                         */

                        await finalPvpQuestionService.Final_Close_Pvp_Number_Question_Voting(timer);
                        return;
                    #endregion


                    /*
                     * Game ended
                     * 
                     * Next event: None
                     */
                    case ActionState.END_GAME:
                        await Game_End(timer);
                        return;

                }
            }
            catch (Exception ex)
            {
                await UnexpectedCriticalError(timer, ex.Message);

                logger.LogError($"Unexpected error occured during a game instance: {ex.Message} \n\n {ex.StackTrace}");
                Console.WriteLine(ex);
            }
        }

        private async Task Game_End(TimerWrapper timerWrapper)
        {
            timerWrapper.Stop();
            timerWrapper.Data.CountDownTimer.Stop();
            using var db = contextFactory.CreateDbContext();
            var data = timerWrapper.Data;

            var gi = timerWrapper.Data.GameInstance;

            // Game is finished
            gi.GameState = GameState.FINISHED;


            await hubContext.Clients.Group(data.GameLink)
                .ShowGameMap();

            var res2 = mapper.Map<GameInstanceResponse>(data.GameInstance);
            await hubContext.Clients.Group(data.GameLink)
                .GetGameInstance(res2);

            // Perform cleanup
            db.Update(gi);
            await db.SaveChangesAsync();

            GameTimers.Remove(timerWrapper);
            timerWrapper.Data.CountDownTimer.Dispose();
            timerWrapper.Dispose();
        }

        private async Task UnexpectedCriticalError(TimerWrapper timerWrapper, string message = "Unhandled game exception")
        {
            timerWrapper.Stop();

            using var db = contextFactory.CreateDbContext();
            var gm = timerWrapper.Data.GameInstance;
            gm.GameState = GameState.CANCELED;
            db.Update(gm);
            await db.SaveChangesAsync();

            timerWrapper.Data.CountDownTimer.Stop();
            timerWrapper.Data.CountDownTimer.Dispose();

            await hubContext.Clients.Group(timerWrapper.Data.GameLink).LobbyCanceled($"Unexpected error occured. Game closed.\n{message}");
            GameTimers.Remove(timerWrapper);
            timerWrapper.Dispose();
        }


        private async Task Game_Preview_Time(TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;
            await hubContext.Clients.Group(data.GameLink)
                .Game_Show_Main_Screen();

            // Restart timer
            timerWrapper.StartTimer(ActionState.OPEN_PLAYER_ATTACK_VOTING);
        }

        public void CancelGameTimer(GameInstance gm)
        {
            var gameTimer = GameTimers.FirstOrDefault(x => x.Data.GameLink == gm.InvitationLink);
            if (gameTimer == null) return;

            gameTimer.Stop();
            gameTimer.Data.CountDownTimer.Stop();
            gameTimer.Data.CountDownTimer.Dispose();
            GameTimers.Remove(gameTimer);
            gameTimer.Dispose();
        }
    }
}

using GameService.Context;
using GameService.Data;
using GameService.Data.Models;
using GameService.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace GameService.Services.GameTimerServices
{
    public interface IGameTimerService
    {
        void OnGameStart(GameInstance gm);
        void CancelGameTimer(GameInstance gm);
    }

    public class GameTimerService : IGameTimerService
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly INeutralMCTimerEvents neutralMCTimerEvents;
        private readonly ICapitalStageTimerEvents capitalStageTimerEvents;
        private readonly IFinalPvpQuestionService finalPvpQuestionService;
        private readonly INeutralNumberTimerEvents neutralNumberTimerEvents;
        private readonly IPvpStageTimerEvents pvpStageTimerEvents;
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        public static List<TimerWrapper> GameTimers = new List<TimerWrapper>();

        public GameTimerService(IDbContextFactory<DefaultContext> _contextFactory,
            IPvpStageTimerEvents pvpStageTimerEvents,
            IHubContext<GameHub, IGameHub> hubContext,
            INeutralMCTimerEvents neutralMCTimerEvents,
            ICapitalStageTimerEvents capitalStageTimerEvents,
            IFinalPvpQuestionService finalPvpQuestionService,
            INeutralNumberTimerEvents neutralNumberTimerEvents)
        {
            contextFactory = _contextFactory;
            this.pvpStageTimerEvents = pvpStageTimerEvents;
            this.hubContext = hubContext;
            this.neutralMCTimerEvents = neutralMCTimerEvents;
            this.capitalStageTimerEvents = capitalStageTimerEvents;
            this.finalPvpQuestionService = finalPvpQuestionService;
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
            using var db = contextFactory.CreateDbContext();
            actionTimer.Data.LastNeutralMCRound = db.Round
                .Where(x => x.GameInstanceId == gm.Id && x.AttackStage == AttackStage.MULTIPLE_NEUTRAL)
                .Count();

            // Default starter values
            actionTimer.Data.CurrentGameRoundNumber = 1;

            GameTimers.Add(actionTimer);

            // Start timer
            actionTimer.Elapsed += ActionTimer_Elapsed;

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

                        // Send request to clients to stay on main screen for preview
                        await Game_Preview_Time(timer);

                        // Debug
                        //await neutralNumberTimerEvents.Debug_Assign_All_Territories_Start_Pvp(timer);
                        //await neutralMCTimerEvents.Debug_Start_Number_Neutral(timer);
                        return;

                    #region Neutral Multiple Choice events
                    case ActionState.OPEN_PLAYER_ATTACK_VOTING:

                        // Send request to clients to open the multiple choice voting
                        // And show whos attacking turn it is

                        await neutralMCTimerEvents
                            .Open_Neutral_MultipleChoice_Attacker_Territory_Selecting(timer);
                        return;

                    case ActionState.CLOSE_PLAYER_ATTACK_VOTING:

                        await neutralMCTimerEvents
                            .Close_Neutral_MultipleChoice_Attacker_Territory_Selecting(timer);
                        return;

                    case ActionState.SHOW_MULTIPLE_CHOICE_QUESTION:

                        await neutralMCTimerEvents
                            .Show_Neutral_MultipleChoice_Screen(timer);
                        return;

                    case ActionState.END_MULTIPLE_CHOICE_QUESTION:

                        await neutralMCTimerEvents
                            .Close_Neutral_MultipleChoice_Question_Voting(timer);
                        return;
                    #endregion

                    #region Neutral Number Events
                    case ActionState.SHOW_PREVIEW_GAME_MAP:
                        await neutralNumberTimerEvents.Show_Game_Map_Screen(timer);
                        return;

                    case ActionState.SHOW_NUMBER_QUESTION:
                        await neutralNumberTimerEvents.Show_Neutral_Number_Screen(timer);
                        return;

                    case ActionState.END_NUMBER_QUESTION:
                        await neutralNumberTimerEvents.Close_Neutral_Number_Question_Voting(timer);
                        return;
                    #endregion

                    #region Pvp Events
                    case ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING:
                        await pvpStageTimerEvents.Open_Pvp_MultipleChoice_Attacker_Territory_Selecting(timer);
                        return;

                    case ActionState.CLOSE_PVP_PLAYER_ATTACK_VOTING:
                        await pvpStageTimerEvents.Close_Pvp_MultipleChoice_Attacker_Territory_Selecting(timer);
                        return;

                    case ActionState.SHOW_PVP_MULTIPLE_CHOICE_QUESTION:
                        await pvpStageTimerEvents.Show_Pvp_MultipleChoice_Screen(timer);
                        return;

                    case ActionState.END_PVP_MULTIPLE_CHOICE_QUESTION:
                        await pvpStageTimerEvents.Close_Pvp_MultipleChoice_Question_Voting(timer);
                        return;

                    case ActionState.SHOW_PVP_NUMBER_QUESTION:
                        await pvpStageTimerEvents.Show_Pvp_Number_Screen(timer);
                        return;

                    case ActionState.END_PVP_NUMBER_QUESTION:
                        await pvpStageTimerEvents.Close_Pvp_Number_Question_Voting(timer);
                        return;
                    #endregion

                    #region CapitalPvpRoundStages
                    case ActionState.SHOW_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION:
                        await capitalStageTimerEvents.Capital_Show_Pvp_MultipleChoice_Question_Voting(timer);
                        return;

                    case ActionState.END_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION:
                        await capitalStageTimerEvents.Capital_Close_Pvp_MultipleChoice_Question_Voting(timer);
                        return;

                    case ActionState.SHOW_CAPITAL_PVP_NUMBER_QUESTION:
                        await capitalStageTimerEvents.Capital_Show_Pvp_Number_Screen(timer);
                        return;

                    case ActionState.END_CAPITAL_PVP_NUMBER_QUESTION:
                        await capitalStageTimerEvents.Capital_Close_Pvp_Number_Question_Voting(timer);
                        return;
                    #endregion

                    #region FinalPvpStageQuestion
                    case ActionState.SHOW_FINAL_PVP_NUMBER_QUESTION:
                        await finalPvpQuestionService.Final_Show_Pvp_Number_Screen(timer);
                        return;

                    case ActionState.END_FINAL_PVP_NUMBER_QUESTION:
                        await finalPvpQuestionService.Final_Close_Pvp_Number_Question_Voting(timer);
                        return;
                    #endregion

                    case ActionState.END_GAME:
                        await Game_End(timer);
                        return;

                }
            }
            catch (Exception ex)
            {
                await UnexpectedCriticalError(timer, ex.Message);
                Console.WriteLine(ex);
            }
        }

        private async Task Game_End(TimerWrapper timerWrapper)
        {
            timerWrapper.Stop();
            timerWrapper.Data.CountDownTimer.Stop();
            using var db = contextFactory.CreateDbContext();
            var data = timerWrapper.Data;

            var gi = await db.GameInstance
                .Where(x => x.Id == data.GameInstanceId)
                .FirstOrDefaultAsync();

            // Game is finished
            gi.GameState = GameState.FINISHED;
            db.Update(gi);
            await db.SaveChangesAsync();

            await hubContext.Clients.Group(data.GameLink)
                .ShowGameMap();

            var gm = await CommonTimerFunc.GetFullGameInstance(data.GameInstanceId, db);
            await hubContext.Clients.Group(data.GameLink).GetGameInstance(gm);

            GameTimers.Remove(timerWrapper);
            timerWrapper.Data.CountDownTimer.Dispose();
            timerWrapper.Dispose();
        }

        private async Task UnexpectedCriticalError(TimerWrapper timerWrapper, string message = "Unhandled game exception")
        {
            using var db = contextFactory.CreateDbContext();
            timerWrapper.Stop();
            timerWrapper.Data.CountDownTimer.Stop();
            timerWrapper.Data.CountDownTimer.Dispose();
            var gm = await db.GameInstance.FirstOrDefaultAsync(x => x.Id == timerWrapper.Data.GameInstanceId);
            gm.GameState = GameState.CANCELED;

            await db.SaveChangesAsync();

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

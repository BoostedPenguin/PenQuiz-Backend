using GameService.Context;
using GameService.Hubs;
using GameService.Models;
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
    }

    public class GameTimerService : IGameTimerService
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly INeutralStageTimerEvents neutralStageTimerEvents;
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        public static List<TimerWrapper> GameTimers = new List<TimerWrapper>();

        public GameTimerService(IDbContextFactory<DefaultContext> _contextFactory,
            INeutralStageTimerEvents neutralStageTimerEvents,
            IHubContext<GameHub, IGameHub> hubContext)
        {
            contextFactory = _contextFactory;
            this.neutralStageTimerEvents = neutralStageTimerEvents;
            this.hubContext = hubContext;
        }

        public void OnGameStart(GameInstance gm)
        {
            if (GameTimers.FirstOrDefault(x => x.Data.GameInstanceId == gm.Id) != null)
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

            try
            {
                switch (timer.Data.NextAction)
                {
                    case ActionState.GAME_START_PREVIEW_TIME:

                        // Send request to clients to stay on main screen for preview
                        await Game_Preview_Time(timer);
                        return;

                    #region Neutral events
                    case ActionState.OPEN_PLAYER_ATTACK_VOTING:

                        // Send request to clients to open the multiple choice voting
                        // And show whos attacking turn it is

                        await neutralStageTimerEvents
                            .Open_Neutral_MultipleChoice_Attacker_Territory_Selecting(timer);
                        return;

                    case ActionState.CLOSE_PLAYER_ATTACK_VOTING:

                        await neutralStageTimerEvents
                            .Close_Neutral_MultipleChoice_Attacker_Territory_Selecting(timer);
                        return;

                    case ActionState.SHOW_MULTIPLE_CHOICE_QUESTION:

                        await neutralStageTimerEvents
                            .Show_Neutral_MultipleChoice_Screen(timer);
                        return;

                    case ActionState.END_MULTIPLE_CHOICE_QUESTION:

                        await neutralStageTimerEvents
                            .Close_Neutral_MultipleChoice_Question_Voting(timer);
                        return;
                    #endregion

                    case ActionState.SHOW_PREVIEW_GAME_MAP:
                        await neutralStageTimerEvents.Show_Game_Map_Screen(timer);
                        return;

                    case ActionState.SHOW_NUMBER_QUESTION:
                        await neutralStageTimerEvents.Show_Neutral_Number_Screen(timer);
                        return;

                    case ActionState.END_NUMBER_QUESTION:
                        await neutralStageTimerEvents.Close_Neutral_Number_Question_Voting(timer);
                        return;
                }
            }
            catch(Exception ex)
            {
                await UnexpectedCriticalError(timer, ex.Message);
                Console.WriteLine(ex);
            }
        }



        private async Task UnexpectedCriticalError(TimerWrapper timerWrapper, string message = "Unhandled game exception")
        {
            var db = contextFactory.CreateDbContext();
            var gm = await db.GameInstance.FirstOrDefaultAsync(x => x.Id == timerWrapper.Data.GameInstanceId);
            gm.GameState = GameState.CANCELED;

            await db.SaveChangesAsync();

            await hubContext.Clients.Group(timerWrapper.Data.GameLink).LobbyCanceled($"Unexpected error occured. Game closed.\n{message}");
            GameTimers.Remove(timerWrapper);
        }


        private async Task Game_Preview_Time(TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;
            await hubContext.Clients.Group(data.GameLink)
                .Game_Show_Main_Screen();

            // Set next action
            timerWrapper.Data.NextAction = ActionState.OPEN_PLAYER_ATTACK_VOTING;

            // Set time until next action *call case state*
            timerWrapper.Interval = GameActionsTime.GetServerActionsTime(ActionState.GAME_START_PREVIEW_TIME);

            // Restart timer
            timerWrapper.Start();
        }
    }
}

using AutoMapper;
using GameService.Context;
using GameService.Dtos.SignalR_Responses;
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
    public interface IPvpStageTimerEvents
    {
        // Number
        // MultipleChoice
        Task Open_Pvp_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper);
        Task Close_Pvp_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper);
        Task Close_Pvp_MultipleChoice_Question_Voting(TimerWrapper timerWrapper);
        Task Show_Pvp_MultipleChoice_Screen(TimerWrapper timerWrapper);
        Task Show_Pvp_Number_Screen(TimerWrapper timerWrapper);
        Task Close_Pvp_Number_Question_Voting(TimerWrapper timerWrapper);
    }

    public class PvpStageTimerEvents : IPvpStageTimerEvents
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        private readonly IGameTerritoryService gameTerritoryService;
        private readonly IMapper mapper;
        private readonly IMessageBusClient messageBus;
        private object participantIds;
        private readonly Random r;

        public PvpStageTimerEvents(IDbContextFactory<DefaultContext> _contextFactory,
            IHubContext<GameHub, IGameHub> hubContext,
            IGameTerritoryService gameTerritoryService,
            IMapper mapper,
            IMessageBusClient messageBus)
        {
            this.hubContext = hubContext;
            this.gameTerritoryService = gameTerritoryService;
            this.mapper = mapper;
            this.messageBus = messageBus;
            contextFactory = _contextFactory;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="timerWrapper"></param>
        /// <param name="isNeutral"></param>
        /// <returns></returns>
        public async Task Show_Pvp_MultipleChoice_Screen(TimerWrapper timerWrapper)
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

            var response = mapper.Map<QuestionClientResponse>(question);

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
                GameActionsTime.GetServerActionsTime(ActionState.SHOW_PVP_MULTIPLE_CHOICE_QUESTION));

            timerWrapper.Data.NextAction = ActionState.END_PVP_MULTIPLE_CHOICE_QUESTION;
            timerWrapper.Interval = GameActionsTime.GetServerActionsTime(ActionState.SHOW_PVP_MULTIPLE_CHOICE_QUESTION);
            timerWrapper.Start();
        }

        ///TODO
        public async Task Close_Pvp_MultipleChoice_Question_Voting(TimerWrapper timerWrapper)
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
                .Include(x => x.PvpRound)
                .ThenInclude(x => x.PvpRoundAnswers)
                .Include(x => x.PvpRound)
                .ThenInclude(x => x.AttackedTerritory)
                .Where(x => x.GameRoundNumber == data.CurrentGameRoundNumber
                    && x.GameInstanceId == data.GameInstanceId)
                .FirstOrDefaultAsync();

            currentRound.IsQuestionVotingOpen = false;

            // If attacker didn't win, we don't care what the outcome is
            var attackerAnswer = currentRound
                .PvpRound
                .PvpRoundAnswers
                .FirstOrDefault(x => x.UserId == currentRound.PvpRound.AttackerId);

            var defenderAnswer = currentRound
                .PvpRound
                .PvpRoundAnswers
                .FirstOrDefault(x => x.UserId == currentRound.PvpRound.DefenderId);

            bool bothPlayersAnsweredCorrectly = false;
            // Attacker didn't answer, automatically loses
            if (attackerAnswer == null || attackerAnswer.MChoiceQAnswerId == null)
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

                if (!didAttackerAnswerCorrectly)
                {
                    // Player answered incorrecly, release isattacked lock on objterritory
                    currentRound.PvpRound.WinnerId = currentRound.PvpRound.DefenderId;
                    currentRound.PvpRound.AttackedTerritory.AttackedBy = null;
                }
                else
                {


                    // Defender didn't vote, he lost
                    if (defenderAnswer == null || defenderAnswer.MChoiceQAnswerId == null)
                    {
                        // Player answered incorrecly, release isattacked lock on objterritory
                        currentRound.PvpRound.WinnerId = currentRound.PvpRound.AttackerId;
                        currentRound.PvpRound.AttackedTerritory.AttackedBy = null;
                        currentRound.PvpRound.AttackedTerritory.TakenBy = currentRound.PvpRound.AttackerId;
                    }
                    // Both people answered correctly, show a blitz number question
                    else
                    {
                        var didDefenderAnswerCorrectly = currentRound
                            .Question
                            .Answers
                            .First(x => x.Id == defenderAnswer.MChoiceQAnswerId)
                            .Correct;

                        // If defender answered incorrectly attacker wins
                        if (!didDefenderAnswerCorrectly)
                        {
                            // Player answered incorrecly, release isattacked lock on objterritory
                            currentRound.PvpRound.WinnerId = currentRound.PvpRound.AttackerId;
                            currentRound.PvpRound.AttackedTerritory.AttackedBy = null;
                            currentRound.PvpRound.AttackedTerritory.TakenBy = currentRound.PvpRound.AttackerId;
                        }
                        else
                        {
                            bothPlayersAnsweredCorrectly = true;
                        }
                    }
                }
            }

            if(!bothPlayersAnsweredCorrectly)
            {
                timerWrapper.Data.CurrentGameRoundNumber++;
                currentRound.GameInstance.GameRoundNumber = timerWrapper.Data.CurrentGameRoundNumber;
            }

            db.Update(currentRound);
            await db.SaveChangesAsync();

            // Client response
            var response = new MCPlayerQuestionAnswers()
            {
                CorrectAnswerId = currentRound.Question.Answers.FirstOrDefault(x => x.Correct).Id,
                PlayerAnswers = new List<PlayerIdAnswerId>()
                {
                    new PlayerIdAnswerId()
                    {
                        Id = currentRound.PvpRound.DefenderId ?? 0,
                        AnswerId = defenderAnswer?.MChoiceQAnswerId ?? 0,
                    },
                    new PlayerIdAnswerId()
                    {
                        Id = currentRound.PvpRound.AttackerId,
                        AnswerId = attackerAnswer?.MChoiceQAnswerId ?? 0,
                    }
                }
            };

            await hubContext.Clients.Groups(data.GameLink).MCQuestionPreviewResult(response);

            if(bothPlayersAnsweredCorrectly)
            {
                timerWrapper.Data.NextAction = ActionState.SHOW_PVP_NUMBER_QUESTION;
            }
            else
            {
                timerWrapper.Data.NextAction = ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING;
            }

            timerWrapper.Interval = GameActionsTime.DefaultPreviewTime;

            timerWrapper.Start();
        }

        public async Task Show_Pvp_Number_Screen(TimerWrapper timerWrapper)
        {
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            var db = contextFactory.CreateDbContext();

            var question = await db.Questions
                .Include(x => x.Answers)
                .Include(x => x.PvpRoundNum)
                .ThenInclude(x => x.Round)
                .ThenInclude(x => x.GameInstance)
                .ThenInclude(x => x.Participants)
                .Where(x => x.PvpRoundNum.Round.GameInstanceId == data.GameInstanceId &&
                    x.PvpRoundNum.Round.GameRoundNumber == data.CurrentGameRoundNumber)
                .FirstOrDefaultAsync();

            question.PvpRoundNum.Round.IsQuestionVotingOpen = true;
            question.PvpRoundNum.Round.QuestionOpenedAt = DateTime.Now;

            question.PvpRoundNum.Round.AttackStage = AttackStage.NUMBER_PVP;
            db.Update(question);
            await db.SaveChangesAsync();

            var response = mapper.Map<QuestionClientResponse>(question);

            response.IsNeutral = false;
            response.Participants = question
                .PvpRoundNum
                .Round
                .GameInstance
                .Participants
                .Where(x => x.PlayerId == question.PvpRoundNum.AttackerId || x.PlayerId == question.PvpRoundNum.DefenderId)
                .ToArray();

            response.AttackerId = question.PvpRoundNum.AttackerId;
            response.DefenderId = question.PvpRoundNum.DefenderId ?? 0;

            await hubContext.Clients.Group(data.GameLink).GetRoundQuestion(response,
                GameActionsTime.GetServerActionsTime(ActionState.SHOW_NUMBER_QUESTION));


            timerWrapper.Data.NextAction = ActionState.END_PVP_NUMBER_QUESTION;
            timerWrapper.Interval = GameActionsTime.GetServerActionsTime(ActionState.SHOW_NUMBER_QUESTION);
            timerWrapper.Start();
        }

        public async Task Close_Pvp_Number_Question_Voting(TimerWrapper timerWrapper)
        {
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();

            var currentRound =
                await db.Round
                .Include(x => x.GameInstance)
                .Include(x => x.PvpRound)
                .ThenInclude(x => x.NumberQuestion)
                .ThenInclude(x => x.Answers)
                .Include(x => x.PvpRound)
                .ThenInclude(x => x.PvpRoundAnswers)
                .Include(x => x.PvpRound)
                .ThenInclude(x => x.AttackedTerritory)
                .Where(x => x.GameRoundNumber == data.CurrentGameRoundNumber
                    && x.GameInstanceId == data.GameInstanceId)
                .FirstOrDefaultAsync();

            currentRound.IsQuestionVotingOpen = false;

            var correctNumberQuestionAnswer = long.Parse(currentRound.PvpRound.NumberQuestion.Answers.First().Answer);

            var playerAnswers = currentRound.PvpRound.PvpRoundAnswers.Select(x => new
            {
                x.NumberQAnswer,
                x.UserId,
                x.NumberQAnsweredAt,
            });

            var clientResponse = new NumberPlayerQuestionAnswers()
            {
                CorrectAnswer = correctNumberQuestionAnswer.ToString(),
                PlayerAnswers = new List<NumberPlayerIdAnswer>(),
            };

            foreach(var player in playerAnswers)
            {
                if (player.NumberQAnswer == null)
                {
                    clientResponse.PlayerAnswers.Add(new NumberPlayerIdAnswer()
                    {
                        Answer = null,
                        TimeElapsed = "",
                        PlayerId = player.UserId,
                    });
                    continue;
                }

                var difference = Math.Abs(correctNumberQuestionAnswer) - Math.Abs((long)player.NumberQAnswer);
                var absoluteDifference = Math.Abs(difference);

                var timeElapsed = Math.Abs((currentRound.QuestionOpenedAt - player.NumberQAnsweredAt).Value.TotalSeconds);

                clientResponse.PlayerAnswers.Add(new NumberPlayerIdAnswer()
                {
                    Answer = player.NumberQAnswer.ToString(),
                    TimeElapsedNumber = timeElapsed,
                    TimeElapsed = timeElapsed.ToString("0.00"),
                    DifferenceWithCorrectNumber = absoluteDifference,
                    PlayerId = player.UserId,
                    DifferenceWithCorrect = absoluteDifference.ToString(),
                });
            }

            int winnerId;

            // If no player answered
            // Defender won
            if (clientResponse.PlayerAnswers.All(x => x.Answer == null))
            {
                winnerId = currentRound.PvpRound.DefenderId ?? 0;
            }
            else
            {
                // Order by answer first
                // Then orderby answeredat
                winnerId = clientResponse.PlayerAnswers
                    .Where(x => x.Answer != null && x.TimeElapsed != null)
                    .OrderBy(x => x.DifferenceWithCorrectNumber)
                    .ThenBy(x => x.TimeElapsedNumber)
                    .Select(x => x.PlayerId)
                    .First();
            }

            // Winner gets territory (if defender it stays his)
            currentRound.PvpRound.AttackedTerritory.TakenBy = winnerId;
            currentRound.PvpRound.AttackedTerritory.AttackedBy = null;
            currentRound.PvpRound.WinnerId = winnerId;


            clientResponse.PlayerAnswers.ForEach(x => x.Winner = x.PlayerId == winnerId);


            // Go to next round
            timerWrapper.Data.CurrentGameRoundNumber++;
            currentRound.GameInstance.GameRoundNumber = timerWrapper.Data.CurrentGameRoundNumber;

            db.Update(currentRound);
            await db.SaveChangesAsync();

            await hubContext.Clients.Groups(data.GameLink).NumberQuestionPreviewResult(clientResponse);

            // Set next action
            timerWrapper.Data.NextAction = ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING;
            timerWrapper.Interval = GameActionsTime.DefaultPreviewTime;
            timerWrapper.Start();
        }

        public async Task Open_Pvp_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();

            var currentRound = await db.Round
                .Include(x => x.PvpRound)
                .ThenInclude(x => x.PvpRoundAnswers)
                .Where(x => x.GameRoundNumber == data.CurrentGameRoundNumber 
                    && x.GameInstanceId == data.GameInstanceId)
                .FirstOrDefaultAsync();

            currentRound.IsTerritoryVotingOpen = true;

            db.Update(currentRound);
            await db.SaveChangesAsync();

            var currentAttacker = currentRound.PvpRound.AttackerId;

            var availableTerritories = await gameTerritoryService
                .GetAvailableAttackTerritoriesNames(db, currentAttacker, data.GameInstanceId, false);

            await hubContext.Clients.Group(data.GameLink)
                .ShowRoundingAttacker(currentAttacker,
                    GameActionsTime.GetServerActionsTime(ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING), availableTerritories);

            var fullGame = await CommonTimerFunc.GetFullGameInstance(data.GameInstanceId, db);
            await hubContext.Clients.Group(data.GameLink)
                .GetGameInstance(fullGame);

            // Set next action and interval
            timerWrapper.Data.NextAction = ActionState.CLOSE_PVP_PLAYER_ATTACK_VOTING;
            timerWrapper.Interval = GameActionsTime.GetServerActionsTime(ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING);

            timerWrapper.Start();
        }

        public async Task Close_Pvp_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();

            var currentRound = await db.Round
                .Include(x => x.PvpRound)
                .ThenInclude(x => x.PvpRoundAnswers)
                .Where(x => x.GameRoundNumber == data.CurrentGameRoundNumber && x.GameInstanceId == data.GameInstanceId)
                .FirstOrDefaultAsync();

            // Player didn't select anything, assign him a random UNSELECTED territory
            if (currentRound.PvpRound.AttackedTerritoryId == null)
            {
                var randomTerritory = 
                    await gameTerritoryService.GetRandomTerritory(currentRound.PvpRound.AttackerId, data.GameInstanceId, false);

                currentRound.PvpRound.AttackedTerritoryId = randomTerritory.Id;

                currentRound.PvpRound.DefenderId = randomTerritory.TakenBy;
                randomTerritory.AttackedBy = currentRound.PvpRound.AttackerId;

                db.Update(randomTerritory);
            }

            var fullGame = await CommonTimerFunc.GetFullGameInstance(data.GameInstanceId, db);

            currentRound.IsTerritoryVotingOpen = false;
            db.Update(currentRound);
            await db.SaveChangesAsync();

            await hubContext.Clients.Group(data.GameLink)
                .GetGameInstance(fullGame);

            timerWrapper.Interval = GameActionsTime.DefaultPreviewTime;
            timerWrapper.Data.NextAction = ActionState.SHOW_PVP_MULTIPLE_CHOICE_QUESTION;
            timerWrapper.Start();
        }
    }
}

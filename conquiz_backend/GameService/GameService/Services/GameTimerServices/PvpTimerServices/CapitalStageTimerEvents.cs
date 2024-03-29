﻿using AutoMapper;
using GameService.Context;
using GameService.Data;
using GameService.Data.Models;
using GameService.Dtos.SignalR_Responses;
using GameService.Hubs;
using GameService.MessageBus;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.GameTimerServices.PvpTimerServices
{
    public interface ICapitalStageTimerEvents
    {
        Task Capital_Show_Pvp_MultipleChoice_Question_Voting(TimerWrapper timerWrapper);
        Task Capital_Close_Pvp_MultipleChoice_Question_Voting(TimerWrapper timerWrapper);
        Task Capital_Show_Pvp_Number_Screen(TimerWrapper timerWrapper);
        Task Capital_Close_Pvp_Number_Question_Voting(TimerWrapper timerWrapper);
    }

    public class CapitalStageTimerEvents : ICapitalStageTimerEvents
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        private readonly ICurrentStageQuestionService gM_DataExtractionService;
        private readonly IMapper mapper;
        private readonly IMessageBusClient messageBus;
        public CapitalStageTimerEvents(IDbContextFactory<DefaultContext> _contextFactory,
            IHubContext<GameHub, IGameHub> hubContext,
            ICurrentStageQuestionService gM_DataExtractionService,
            IMapper mapper,
            IMessageBusClient messageBus)
        {
            this.hubContext = hubContext;
            this.gM_DataExtractionService = gM_DataExtractionService;
            this.mapper = mapper;
            this.messageBus = messageBus;
            contextFactory = _contextFactory;
        }

        public async Task Capital_Show_Pvp_MultipleChoice_Question_Voting(TimerWrapper timerWrapper)
        {
            // Stop timer until we calculate the next action and client event
            timerWrapper.Stop();

            // Get the question and show it to the clients
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();
            var gm = timerWrapper.Data.GameInstance;

            var question = data.GetBaseRound
                .PvpRound.CapitalRounds.First(e => !e.IsCompleted && e.CapitalRoundAttackStage == CapitalRoundAttackStage.MULTIPLE_CHOICE_QUESTION).CapitalRoundMultipleQuestion;
            
            // Get mapped response for clients
            var response = gM_DataExtractionService.GetCurrentStageQuestionResponse(gm);

            // Open for voting the capital question
            question.CapitalRoundMultiple.IsQuestionVotingOpen = true;
            db.Update(gm);
            await db.SaveChangesAsync();

            // Send data to client
            await hubContext.Clients.Group(data.GameLink).GetRoundQuestion(response);

            // If both bots, change the timer
            var isDefenderBot = gm.Participants.FirstOrDefault(e => e.PlayerId == response.DefenderId).Player.IsBot;
            var isAttackerBot = gm.Participants.FirstOrDefault(e => e.PlayerId == response.AttackerId).Player.IsBot;

            if (isDefenderBot && isAttackerBot)
            {
                timerWrapper.StartTimer(ActionState.END_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION, GameActionsTime.BotVsBotQuestionTime);
                return;
            }

            timerWrapper.StartTimer(ActionState.END_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION);
        }

        public async Task Capital_Close_Pvp_MultipleChoice_Question_Voting(TimerWrapper timerWrapper)
        {
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();
            var gm = data.GameInstance;
            var baseRound = data.GetBaseRound;


            var capitalRound = baseRound.PvpRound.CapitalRounds.FirstOrDefault(x => !x.IsCompleted &&
                x.CapitalRoundAttackStage == CapitalRoundAttackStage.MULTIPLE_CHOICE_QUESTION &&
                x.IsQuestionVotingOpen);

            if (capitalRound == null)
            {
                // Should never be null
                throw new ArgumentException("Capital round is null, when it shouldn't be. Fatal error.");
            }

            capitalRound.IsQuestionVotingOpen = false;


            // Check if either participant is a bot
            // If the attacker is a bot
            if (gm.Participants.First(e => e.PlayerId == baseRound.PvpRound.AttackerId).Player.IsBot)
            {
                capitalRound.CapitalRoundUserAnswers.Add(new CapitalRoundAnswers()
                {
                    MChoiceQAnswerId = BotService.GenerateBotMCAnswerId(capitalRound
                        .CapitalRoundMultipleQuestion
                        .Answers.ToArray()),

                    UserId = baseRound.PvpRound.AttackerId
                });
            }

            // If the defender is a bot
            if (gm.Participants.First(e => e.PlayerId == baseRound.PvpRound.DefenderId).Player.IsBot)
            {
                capitalRound.CapitalRoundUserAnswers.Add(new CapitalRoundAnswers()
                {
                    MChoiceQAnswerId = BotService.GenerateBotMCAnswerId(capitalRound
                        .CapitalRoundMultipleQuestion
                        .Answers.ToArray()),

                    UserId = (int)baseRound.PvpRound.DefenderId
                });
            }


            var attackerAnswer = capitalRound
                .CapitalRoundUserAnswers.FirstOrDefault(x => x.UserId == baseRound.PvpRound.AttackerId);
            var defenderAnswer = capitalRound
                .CapitalRoundUserAnswers.FirstOrDefault(x => x.UserId == baseRound.PvpRound.DefenderId);


            bool bothPlayersAnsweredCorrectly = false;
            bool pvpRoundFinished = false;
            var nextAction = ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING;

            // Attacker didn't answer, automatically loses
            if (attackerAnswer == null || attackerAnswer.MChoiceQAnswerId == null)
            {
                // Player answered incorrecly, release isattacked lock on objterritory
                baseRound.PvpRound.WinnerId = baseRound.PvpRound.DefenderId;
                baseRound.PvpRound.AttackedTerritory.AttackedBy = null;

                pvpRoundFinished = true;
            }
            else
            {
                var didAttackerAnswerCorrectly = capitalRound
                    .CapitalRoundMultipleQuestion
                    .Answers
                    .First(x => x.Id == attackerAnswer.MChoiceQAnswerId)
                    .Correct;

                if (!didAttackerAnswerCorrectly)
                {
                    // Player answered incorrecly, release isattacked lock on objterritory
                    baseRound.PvpRound.WinnerId = baseRound.PvpRound.DefenderId;
                    baseRound.PvpRound.AttackedTerritory.AttackedBy = null;

                    pvpRoundFinished = true;
                }
                else
                {
                    var remainingCapitalRoundsCount = baseRound
                        .PvpRound
                        .CapitalRounds
                        .Count(x => !x.IsCompleted && x.Id != capitalRound.Id
                            && x.CapitalRoundAttackStage == CapitalRoundAttackStage.MULTIPLE_CHOICE_QUESTION);

                    // Defender didn't vote
                    if (defenderAnswer == null || defenderAnswer.MChoiceQAnswerId == null)
                    {
                        // No more remaining capital rounds
                        if (remainingCapitalRoundsCount == 0)
                        {
                            // All defender territories are now the attackers
                            

                            var allDefenderTerritories = gm.ObjectTerritory
                                .Where(e => e.TakenBy == baseRound.PvpRound.DefenderId)
                                .ToList();

                            foreach (var terr in allDefenderTerritories)
                            {
                                terr.IsCapital = false;
                                terr.TakenBy = baseRound.PvpRound.AttackerId;
                                terr.AttackedBy = null;
                                db.Update(gm);
                            }


                            // Player answered incorrecly, release isattacked lock on objterritory
                            baseRound.PvpRound.WinnerId = baseRound.PvpRound.AttackerId;

                            pvpRoundFinished = true;
                        }
                        else
                        {
                            nextAction = ActionState.SHOW_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION;
                        }
                    }
                    // Defender answered
                    else
                    {
                        var didDefenderAnswerCorrectly = capitalRound
                            .CapitalRoundMultipleQuestion
                            .Answers
                            .First(x => x.Id == defenderAnswer.MChoiceQAnswerId)
                            .Correct;

                        // If defender answered incorrectly check if there are more capital rounds
                        if (!didDefenderAnswerCorrectly)
                        {

                            // If it's capital and there are remaining non-finished capital rounds go to next one
                            if (remainingCapitalRoundsCount == 0)
                            {
                                // All defender territories are now the attackers
                                var allDefenderTerritories = gm.ObjectTerritory
                                    .Where(e => e.TakenBy == baseRound.PvpRound.DefenderId)
                                    .ToList();

                                foreach (var terr in allDefenderTerritories)
                                {
                                    terr.IsCapital = false;
                                    terr.TakenBy = baseRound.PvpRound.AttackerId;
                                    terr.AttackedBy = null;
                                    db.Update(gm);
                                }


                                // Player answered incorrecly, release isattacked lock on objterritory
                                baseRound.PvpRound.WinnerId = baseRound.PvpRound.AttackerId;

                                pvpRoundFinished = true;
                            }
                            else
                            {
                                nextAction = ActionState.SHOW_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION;
                            }
                        }
                        // Both players answered correctly
                        else
                        {
                            nextAction = ActionState.SHOW_CAPITAL_PVP_NUMBER_QUESTION;
                            capitalRound.CapitalRoundAttackStage = CapitalRoundAttackStage.NUMBER_QUESTION;
                            bothPlayersAnsweredCorrectly = true;
                        }
                    }
                }
            }

            if(!bothPlayersAnsweredCorrectly)
            {
                capitalRound.IsCompleted = true;
            }

            if (pvpRoundFinished && !bothPlayersAnsweredCorrectly)
            {
                baseRound.GameInstance.GameRoundNumber++;
            }

            db.Update(gm);
            await db.SaveChangesAsync();
            CommonTimerFunc.CalculateUserScore(gm);

            // Client response
            var response = new MCPlayerQuestionAnswers()
            {
                CorrectAnswerId = capitalRound.CapitalRoundMultipleQuestion.Answers.FirstOrDefault(x => x.Correct).Id,
                PlayerAnswers = new List<PlayerIdAnswerId>()
                {
                    new PlayerIdAnswerId()
                    {
                        Id = baseRound.PvpRound.DefenderId ?? 0,
                        AnswerId = defenderAnswer?.MChoiceQAnswerId ?? 0,
                    },
                    new PlayerIdAnswerId()
                    {
                        Id = baseRound.PvpRound.AttackerId,
                        AnswerId = attackerAnswer?.MChoiceQAnswerId ?? 0,
                    }
                }
            };

            await hubContext.Clients.Groups(data.GameLink).MCQuestionPreviewResult(response);

            var isGameOver = await CommonTimerFunc
                .PvpStage_IsGameOver(timerWrapper, db, messageBus);

            switch (isGameOver)
            {
                case CommonTimerFunc.PvpStageIsGameOver.GAME_OVER:
                    nextAction = ActionState.END_GAME;
                    break;
                case CommonTimerFunc.PvpStageIsGameOver.GAME_CONTINUING:
                    // Do nothing, game continues
                    break;
                case CommonTimerFunc.PvpStageIsGameOver.REQUEST_FINAL_QUESTION:
                    nextAction = ActionState.SHOW_FINAL_PVP_NUMBER_QUESTION;
                    break;
            }

            timerWrapper.StartTimer(nextAction);
        }



        public async Task Capital_Show_Pvp_Number_Screen(TimerWrapper timerWrapper)
        {
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();
            var gm = data.GameInstance;

            var question = data.GetBaseRound
                .PvpRound.CapitalRounds.First(e => !e.IsCompleted && e.CapitalRoundAttackStage == CapitalRoundAttackStage.NUMBER_QUESTION).CapitalRoundNumberQuestion;


            var response = gM_DataExtractionService.GetCurrentStageQuestionResponse(gm);

            question.CapitalRoundNumber.IsQuestionVotingOpen = true;
            question.CapitalRoundNumber.QuestionOpenedAt = DateTime.Now;

            db.Update(gm);
            await db.SaveChangesAsync();

            await hubContext.Clients.Group(data.GameLink).GetRoundQuestion(response);

            // If both bots, change the timer
            var isDefenderBot = gm.Participants.FirstOrDefault(e => e.PlayerId == response.DefenderId).Player.IsBot;
            var isAttackerBot = gm.Participants.FirstOrDefault(e => e.PlayerId == response.AttackerId).Player.IsBot;

            if (isDefenderBot && isAttackerBot)
            {
                timerWrapper.StartTimer(ActionState.END_CAPITAL_PVP_NUMBER_QUESTION, GameActionsTime.BotVsBotQuestionTime);
                return;
            }

            timerWrapper.StartTimer(ActionState.END_CAPITAL_PVP_NUMBER_QUESTION);
        }

        public async Task Capital_Close_Pvp_Number_Question_Voting(TimerWrapper timerWrapper)
        {
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();
            var gm = data.GameInstance;
            var baseRound = data.GetBaseRound;


            var capitalRound = baseRound.PvpRound.CapitalRounds.FirstOrDefault(x => !x.IsCompleted &&
                x.CapitalRoundAttackStage == CapitalRoundAttackStage.NUMBER_QUESTION &&
                x.IsQuestionVotingOpen);

            if (capitalRound == null)
            {
                // Should never be null
                throw new ArgumentException("Capital round is null, when it shouldn't be. Fatal error.");
            }

            capitalRound.IsQuestionVotingOpen = false;

            var correctNumberQuestionAnswer = long.Parse(capitalRound.CapitalRoundNumberQuestion.Answers.First().Answer);

            var playerAnswers = capitalRound.CapitalRoundUserAnswers;

            var clientResponse = new NumberPlayerQuestionAnswers()
            {
                CorrectAnswer = correctNumberQuestionAnswer.ToString(),
                PlayerAnswers = new List<NumberPlayerIdAnswer>(),
            };

            foreach (var player in playerAnswers)
            {
                // Check if the current attacker is a bot
                // Handle bot answer
                var isThisPlayerBot = gm.Participants.First(e => e.PlayerId == player.UserId).Player.IsBot;
                if (isThisPlayerBot)
                {
                    player.NumberQAnsweredAt = DateTime.Now;
                    player.NumberQAnswer = BotService.GenerateBotNumberAnswer(correctNumberQuestionAnswer);
                }


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

                var timeElapsed = Math.Abs((capitalRound.QuestionOpenedAt - player.NumberQAnsweredAt).Value.TotalSeconds);

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
                winnerId = baseRound.PvpRound.DefenderId ?? 0;
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

            bool pvpRoundFinished = false;
            var nextAction = ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING;
            // Defender won
            if (winnerId == baseRound.PvpRound.DefenderId)
            {
                baseRound.PvpRound.AttackedTerritory.TakenBy = baseRound.PvpRound.DefenderId;
                baseRound.PvpRound.AttackedTerritory.AttackedBy = null;
                baseRound.PvpRound.WinnerId = winnerId;

                pvpRoundFinished = true;
            }
            // Attacker won
            else
            {
                var remainingCapitalRoundsCount = baseRound
                    .PvpRound
                    .CapitalRounds
                    .Count(x => !x.IsCompleted && x.Id != capitalRound.Id
                        && x.CapitalRoundAttackStage == CapitalRoundAttackStage.MULTIPLE_CHOICE_QUESTION);

                // If it's capital and there are remaining non-finished capital rounds go to next one
                if (remainingCapitalRoundsCount == 0)
                {
                    // All defender territories are now the attackers
                    var allDefenderTerritories = gm.ObjectTerritory
                        .Where(e => e.TakenBy == baseRound.PvpRound.DefenderId)
                        .ToList();

                    foreach (var terr in allDefenderTerritories)
                    {
                        terr.IsCapital = false;
                        terr.TakenBy = baseRound.PvpRound.AttackerId;
                        terr.AttackedBy = null;
                        db.Update(gm);
                    }

                    // Player answered incorrecly, release isattacked lock on objterritory
                    baseRound.PvpRound.WinnerId = baseRound.PvpRound.AttackerId;

                    pvpRoundFinished = true;
                }
                else
                {
                    nextAction = ActionState.SHOW_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION;
                }
            }

            clientResponse.PlayerAnswers.ForEach(x => x.Winner = x.PlayerId == winnerId);
            capitalRound.IsCompleted = true;

            if(pvpRoundFinished)
            {
                // Go to next round
                baseRound.GameInstance.GameRoundNumber++;
            }


            db.Update(gm);
            await db.SaveChangesAsync();
            CommonTimerFunc.CalculateUserScore(gm);

            await hubContext.Clients.Groups(data.GameLink).NumberQuestionPreviewResult(clientResponse);

            // Check if there are any non-attacker territories left
            var isGameOver = await CommonTimerFunc
                .PvpStage_IsGameOver(timerWrapper, db, messageBus);

            switch (isGameOver)
            {
                case CommonTimerFunc.PvpStageIsGameOver.GAME_OVER:
                    nextAction = ActionState.END_GAME;
                    break;
                case CommonTimerFunc.PvpStageIsGameOver.GAME_CONTINUING:
                    // Do nothing, game continues
                    break;
                case CommonTimerFunc.PvpStageIsGameOver.REQUEST_FINAL_QUESTION:
                    nextAction = ActionState.SHOW_FINAL_PVP_NUMBER_QUESTION;
                    break;
            }

            // Set next action
            timerWrapper.StartTimer(nextAction);
        }
    }
}

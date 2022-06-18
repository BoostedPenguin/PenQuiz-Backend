using AutoMapper;
using GameService.Context;
using GameService.Data;
using GameService.Data.Models;
using GameService.Dtos.SignalR_Responses;
using GameService.Hubs;
using GameService.MessageBus;
using GameService.Services.CharacterActions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.GameTimerServices.PvpTimerServices
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
        private readonly ICurrentStageQuestionService dataExtractionService;
        private readonly IMessageBusClient messageBus;

        public PvpStageTimerEvents(IDbContextFactory<DefaultContext> _contextFactory,
            IHubContext<GameHub, IGameHub> hubContext,
            IGameTerritoryService gameTerritoryService,
            IMapper mapper,
            ICurrentStageQuestionService dataExtractionService,
            IMessageBusClient messageBus)
        {
            this.hubContext = hubContext;
            this.gameTerritoryService = gameTerritoryService;
            this.mapper = mapper;
            this.dataExtractionService = dataExtractionService;
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
            using var db = contextFactory.CreateDbContext();

            var gm = data.GameInstance;

            var question = gm.Rounds.First(e => e.GameRoundNumber == e.GameInstance.GameRoundNumber).Question;

            var response = dataExtractionService.GetCurrentStageQuestionResponse(gm);

            // Open this question for voting
            question.Round.IsQuestionVotingOpen = true;
            db.Update(gm);
            await db.SaveChangesAsync();

            await hubContext.Clients.Group(data.GameLink).GetRoundQuestion(response);

            // If both bots, change the timer
            var isDefenderBot = gm.Participants.FirstOrDefault(e => e.PlayerId == response.DefenderId).Player.IsBot;
            var isAttackerBot = gm.Participants.FirstOrDefault(e => e.PlayerId == response.AttackerId).Player.IsBot;

            if(isDefenderBot && isAttackerBot)
            {
                timerWrapper.StartTimer(ActionState.END_PVP_MULTIPLE_CHOICE_QUESTION, GameActionsTime.BotVsBotQuestionTime);
                return;
            }

            timerWrapper.StartTimer(ActionState.END_PVP_MULTIPLE_CHOICE_QUESTION);
        }

        public async Task Close_Pvp_MultipleChoice_Question_Voting(TimerWrapper timerWrapper)
        {
            // Can disable voting on start, however even 0-1s delay wouldn't be game breaking and would ease performance
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();

            var gm = data.GameInstance;

            var currentRound = data.GetBaseRound;

            currentRound.IsQuestionVotingOpen = false;


            // Check if either participant is a bot
            // If the attacker is a bot
            if(gm.Participants.First(e => e.PlayerId == currentRound.PvpRound.AttackerId).Player.IsBot)
            {
                currentRound.PvpRound.PvpRoundAnswers.Add(new PvpRoundAnswers()
                {
                    MChoiceQAnswerId = BotService.GenerateBotMCAnswerId(currentRound.Question.Answers.ToArray()),
                    UserId = currentRound.PvpRound.AttackerId
                });
            }

            // If the defender is a bot
            if (gm.Participants.First(e => e.PlayerId == currentRound.PvpRound.DefenderId).Player.IsBot)
            {
                currentRound.PvpRound.PvpRoundAnswers.Add(new PvpRoundAnswers()
                {
                    MChoiceQAnswerId = BotService.GenerateBotMCAnswerId(currentRound.Question.Answers.ToArray()),
                    UserId = (int)currentRound.PvpRound.DefenderId
                });
            }

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
            var nextAction = ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING;

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
                // Attacker answered correctly
                {
                    // If capital go to next capital question, defender didn't lose capital yet
                    var isTerritoryCapital = currentRound.PvpRound.AttackedTerritory.IsCapital;

                    // Defender didn't vote, he lost
                    if (defenderAnswer == null || defenderAnswer.MChoiceQAnswerId == null)
                    {
                        if(isTerritoryCapital)
                        {
                            nextAction = ActionState.SHOW_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION;
                        }
                        else
                        {
                            // Player answered incorrecly, release isattacked lock on objterritory
                            currentRound.PvpRound.WinnerId = currentRound.PvpRound.AttackerId;
                            currentRound.PvpRound.AttackedTerritory.AttackedBy = null;
                            currentRound.PvpRound.AttackedTerritory.TakenBy = currentRound.PvpRound.AttackerId;
                        }
                    }
                    // Defender had an answer
                    else
                    {
                        var didDefenderAnswerCorrectly = currentRound
                            .Question
                            .Answers
                            .First(x => x.Id == defenderAnswer.MChoiceQAnswerId)
                            .Correct;

                        // If defender answered incorrectly 
                        if (!didDefenderAnswerCorrectly)
                        {

                            // If it's capital and there are remaining non-finished capital rounds go to next one
                            if (currentRound.PvpRound.AttackedTerritory.IsCapital)
                            {
                                nextAction = ActionState.SHOW_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION;
                            }
                            else
                            {
                                // Defender answered incorrecly, release isattacked lock on objterritory
                                currentRound.PvpRound.WinnerId = currentRound.PvpRound.AttackerId;
                                currentRound.PvpRound.AttackedTerritory.AttackedBy = null;
                                currentRound.PvpRound.AttackedTerritory.TakenBy = currentRound.PvpRound.AttackerId;
                            }
                        }
                        else
                        {
                            nextAction = ActionState.SHOW_PVP_NUMBER_QUESTION;
                            bothPlayersAnsweredCorrectly = true;
                        }
                    }
                }
            }

            // Go to next round
            // If attacker did not answer correctly (he lost)
            // If attacker AND defender answered CORRECTLY - do NOT go to next round
            // If attacker DID answer correctly AND the attacked territory isn't capital
            // (because there will be a followup question)
            if(currentRound.PvpRound.WinnerId is not null && 
                !bothPlayersAnsweredCorrectly && nextAction != ActionState.SHOW_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION)
            {
                currentRound.GameInstance.GameRoundNumber++;
            }
            

            // Indicate that this pvpround is already on capital questions
            if(nextAction == ActionState.SHOW_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION)
            {
                currentRound.PvpRound.IsCurrentlyCapitalStage = true;
            }

            db.Update(gm);
            await db.SaveChangesAsync();
            CommonTimerFunc.CalculateUserScore(gm);


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

        public async Task Show_Pvp_Number_Screen(TimerWrapper timerWrapper)
        {
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();

            var gm = data.GameInstance;
            var question = data.GetBaseRound.PvpRound.NumberQuestion;

            
            question.PvpRoundNum.Round.IsQuestionVotingOpen = true;
            question.PvpRoundNum.Round.QuestionOpenedAt = DateTime.Now;

            question.PvpRoundNum.Round.AttackStage = AttackStage.NUMBER_PVP;
            db.Update(gm);
            await db.SaveChangesAsync();

            var response = dataExtractionService.GetCurrentStageQuestionResponse(gm);

            await hubContext.Clients.Group(data.GameLink).GetRoundQuestion(response);


            // If both bots, change the timer
            var isDefenderBot = gm.Participants.FirstOrDefault(e => e.PlayerId == response.DefenderId).Player.IsBot;
            var isAttackerBot = gm.Participants.FirstOrDefault(e => e.PlayerId == response.AttackerId).Player.IsBot;

            if (isDefenderBot && isAttackerBot)
            {
                timerWrapper.StartTimer(ActionState.END_PVP_NUMBER_QUESTION, GameActionsTime.BotVsBotQuestionTime);
                return;
            }

            timerWrapper.StartTimer(ActionState.END_PVP_NUMBER_QUESTION);
        }

        public async Task Close_Pvp_Number_Question_Voting(TimerWrapper timerWrapper)
        {
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();

            var gm = data.GameInstance;

            var currentRound = data.GetBaseRound;

            currentRound.IsQuestionVotingOpen = false;

            var correctNumberQuestionAnswer = long.Parse(currentRound.PvpRound.NumberQuestion.Answers.First().Answer);

            var playerAnswers = currentRound.PvpRound.PvpRoundAnswers;

            var clientResponse = new NumberPlayerQuestionAnswers()
            {
                CorrectAnswer = correctNumberQuestionAnswer.ToString(),
                PlayerAnswers = new List<NumberPlayerIdAnswer>(),
            };

            foreach(var player in playerAnswers)
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
            bool pvpRoundFinished = false;
            var nextAction = ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING;

            // Defender won
            if (winnerId == currentRound.PvpRound.DefenderId)
            {
                currentRound.PvpRound.AttackedTerritory.TakenBy = currentRound.PvpRound.DefenderId;
                currentRound.PvpRound.AttackedTerritory.AttackedBy = null;
                currentRound.PvpRound.WinnerId = winnerId;

                pvpRoundFinished = true;
            }
            // Attacker won
            else
            {
                if (currentRound.PvpRound.AttackedTerritory.IsCapital)
                {
                    nextAction = ActionState.SHOW_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION;
                }
                else
                {
                    currentRound.PvpRound.AttackedTerritory.TakenBy = currentRound.PvpRound.AttackerId;
                    currentRound.PvpRound.AttackedTerritory.AttackedBy = null;
                    currentRound.PvpRound.WinnerId = winnerId;

                    pvpRoundFinished = true;
                }
            }

            clientResponse.PlayerAnswers.ForEach(x => x.Winner = x.PlayerId == winnerId);

            if (pvpRoundFinished)
            {
                // Go to next round
                currentRound.GameInstance.GameRoundNumber++;
            }


            // Indicate that this pvpround is already on capital questions
            if (nextAction == ActionState.SHOW_CAPITAL_PVP_MULTIPLE_CHOICE_QUESTION)
            {
                currentRound.PvpRound.IsCurrentlyCapitalStage = true;
            }

            db.Update(gm);
            await db.SaveChangesAsync();

            CommonTimerFunc.CalculateUserScore(gm);

            await hubContext.Clients.Groups(data.GameLink).NumberQuestionPreviewResult(clientResponse);

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

        public async Task Open_Pvp_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();

            var gm = data.GameInstance;
            var currentRound = gm.Rounds.Where(x => x.GameRoundNumber == x.GameInstance.GameRoundNumber).FirstOrDefault();
            
          
            switch (await CommonTimerFunc.PvpStage_IsGameOver(timerWrapper, db, messageBus))
            {
                case CommonTimerFunc.PvpStageIsGameOver.GAME_OVER:
                    timerWrapper.StartTimer(ActionState.END_GAME);
                    return;
                case CommonTimerFunc.PvpStageIsGameOver.GAME_CONTINUING:
                    // Do nothing, game continues
                    break;
                case CommonTimerFunc.PvpStageIsGameOver.REQUEST_FINAL_QUESTION:
                    timerWrapper.StartTimer(ActionState.SHOW_FINAL_PVP_NUMBER_QUESTION);
                    return;
            }

            currentRound.IsTerritoryVotingOpen = true;

            db.Update(gm);
            await db.SaveChangesAsync();

            var currentAttacker = currentRound.PvpRound.AttackerId;

            var userTerritoriesCount = gm.ObjectTerritory.Where(x => x.TakenBy == currentAttacker).Count();

            // User already lost, move to next attacker
            if(userTerritoriesCount == 0)
            {
                // Go to next round
                currentRound.GameInstance.GameRoundNumber++;

                db.Update(gm);
                await db.SaveChangesAsync();

                timerWrapper.StartTimer(ActionState.OPEN_PVP_PLAYER_ATTACK_VOTING, 50);
                return;
            }

            var availableTerritories = gameTerritoryService
                .GetAvailableAttackTerritoriesNames(gm, currentAttacker, data.GameInstanceId, false);

            await hubContext.Clients.Group(data.GameLink)
                .ShowRoundingAttacker(new RoundingAttackerRes( currentAttacker, availableTerritories));

            var res1 = mapper.Map<GameInstanceResponse>(data.GameInstance);
            await hubContext.Clients.Group(data.GameLink)
                .GetGameInstance(res1);


            // Bots
            var isAttackerBot = gm.Participants.First(e => e.PlayerId == currentAttacker).Player.IsBot;

            if (isAttackerBot)
            {
                timerWrapper.StartTimer(ActionState.CLOSE_PVP_PLAYER_ATTACK_VOTING, GameActionsTime.BotTerritorySelectTime);
                return;
            }


            timerWrapper.StartTimer(ActionState.CLOSE_PVP_PLAYER_ATTACK_VOTING);
        }

        public async Task Close_Pvp_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();

            var gm = data.GameInstance;

            var currentRound = gm.Rounds
                .Where(x => x.GameRoundNumber == x.GameInstance.GameRoundNumber).FirstOrDefault();


            // Player didn't select anything, assign him a random UNSELECTED territory
            if (currentRound.PvpRound.AttackedTerritoryId == null)
            {
                var readonlyRandomTerritory = 
                    gameTerritoryService.GetRandomTerritory(gm, currentRound.PvpRound.AttackerId, data.GameInstanceId, false);

                var randomTerritory = data.GameInstance.ObjectTerritory.First(x => x.Id == readonlyRandomTerritory.Id);

                currentRound.PvpRound.AttackedTerritoryId = randomTerritory.Id;

                currentRound.PvpRound.DefenderId = randomTerritory.TakenBy;

                randomTerritory.AttackedBy = currentRound.PvpRound.AttackerId;

                db.Update(gm);
            }



            currentRound.IsTerritoryVotingOpen = false;
            db.Update(gm);
            await db.SaveChangesAsync();

            // If the chosen territory is a capital add additional rounds
            if (currentRound.PvpRound.AttackedTerritory.IsCapital)
            {
                const int AdditionalCapitalRounds = 1;

                for (var i = 0; i < AdditionalCapitalRounds; i++)
                {
                    currentRound.PvpRound.CapitalRounds.Add(new CapitalRound());
                }

                db.Update(gm);
                await db.SaveChangesAsync();

                // Request questions for these rounds
                CommonTimerFunc.RequestCapitalQuestions(messageBus,
                    data.GameGlobalIdentifier,
                    currentRound.PvpRound.CapitalRounds.Select(x => x.Id).ToList());
            }

            var res1 = mapper.Map<GameInstanceResponse>(data.GameInstance);
            await hubContext.Clients.Group(data.GameLink)
                .GetGameInstance(res1);

            timerWrapper.StartTimer(ActionState.SHOW_PVP_MULTIPLE_CHOICE_QUESTION);
        }
    }
}

﻿using AutoMapper;
using GameService.Context;
using GameService.Data;
using GameService.Data.Models;
using GameService.Dtos.SignalR_Responses;
using GameService.Hubs;
using GameService.MessageBus;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.GameTimerServices.NeutralTimerServices
{
    public interface INeutralMCTimerEvents
    {
        Task Close_Neutral_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper);
        Task Close_Neutral_MultipleChoice_Question_Voting(TimerWrapper timerWrapper);
        Task Debug_Start_Number_Neutral(TimerWrapper timerWrapper);
        Task Open_Neutral_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper);
        Task Show_Neutral_MultipleChoice_Screen(TimerWrapper timerWrapper);
    }

    public partial class NeutralMCTimerEvents : INeutralMCTimerEvents
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        private readonly IGameTerritoryService gameTerritoryService;
        private readonly IMapper mapper;
        private readonly ICurrentStageQuestionService gM_DataExtractionService;
        private readonly IMessageBusClient messageBus;
        public NeutralMCTimerEvents(IDbContextFactory<DefaultContext> _contextFactory,
            IHubContext<GameHub, IGameHub> hubContext,
            IGameTerritoryService gameTerritoryService,
            IMapper mapper,
            ICurrentStageQuestionService gM_DataExtractionService,
            IMessageBusClient messageBus)
        {
            contextFactory = _contextFactory;
            this.hubContext = hubContext;
            this.gameTerritoryService = gameTerritoryService;
            this.mapper = mapper;
            this.gM_DataExtractionService = gM_DataExtractionService;
            this.messageBus = messageBus;
        }

        public async Task Close_Neutral_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;

            var gm = data.GameInstance;
            var currentRound = data.GetBaseRound;

            var currentAttacker = currentRound.NeutralRound.TerritoryAttackers
                .First(x => x.AttackOrderNumber == currentRound.NeutralRound.AttackOrderNumber);

            // Player didn't select anything, assign him a random UNSELECTED territory
            if (currentAttacker.AttackedTerritoryId == null)
            {
                var randomTerritory =
                    gameTerritoryService.GetRandomTerritory(gm, currentAttacker.AttackerId, data.GameInstanceId);

                // Set this territory as being attacked from this person
                currentAttacker.AttackedTerritoryId = randomTerritory.Id;

                // Set the ObjectTerritory as being attacked currently
                data.GameInstance.ObjectTerritory.First(x => x.Id == randomTerritory.Id).AttackedBy = currentAttacker.AttackerId;

            }

            // This attacker voting is over, go to next attacker
            switch (currentRound.NeutralRound.AttackOrderNumber)
            {
                case 1:
                case 2:
                    var newAttackorderNumber = ++currentRound.NeutralRound.AttackOrderNumber;
                    var nextAttacker =
                        currentRound.NeutralRound.TerritoryAttackers
                        .First(x => x.AttackOrderNumber == newAttackorderNumber);
                    using (var db = contextFactory.CreateDbContext())
                    {
                        db.Update(gm);
                        await db.SaveChangesAsync();
                    }


                    var availableTerritories = gameTerritoryService
                        .GetAvailableAttackTerritoriesNames(gm, nextAttacker.AttackerId, currentRound.GameInstanceId, true);

                    
                    // Notify the group who is the next attacker
                    await hubContext.Clients.Group(data.GameLink)
                        .ShowRoundingAttacker(new RoundingAttackerRes(nextAttacker.AttackerId, availableTerritories));


                    var res1 = mapper.Map<GameInstanceResponse>(data.GameInstance);
                    await hubContext.Clients.Group(data.GameLink)
                        .GetGameInstance(res1);


                    var isAttackerBot = gm.Participants.First(e => e.PlayerId == nextAttacker.AttackerId).Player.IsBot;

                    if (isAttackerBot)
                    {
                        timerWrapper.StartTimer(ActionState.CLOSE_PLAYER_ATTACK_VOTING, GameActionsTime.BotTerritorySelectTime);
                        return;
                    }

                    timerWrapper.StartTimer(ActionState.CLOSE_PLAYER_ATTACK_VOTING);

                    break;

                // This was the last attacker, show a preview of all territories now, and then show the questions
                case 3:
                    currentRound.IsTerritoryVotingOpen = false;
                    using (var db = contextFactory.CreateDbContext())
                    {
                        db.Update(gm);
                        await db.SaveChangesAsync();
                    }

                    var res2 = mapper.Map<GameInstanceResponse>(data.GameInstance);
                    await hubContext.Clients.Group(data.GameLink)
                        .GetGameInstance(res2);


                    timerWrapper.StartTimer(ActionState.SHOW_MULTIPLE_CHOICE_QUESTION);
                    break;
                //throw new NotImplementedException();

                default:
                    throw new ArgumentException($"Unexpected attackordernumber value: {currentRound.NeutralRound.AttackOrderNumber}");
            }
        }

        public async Task Close_Neutral_MultipleChoice_Question_Voting(TimerWrapper timerWrapper)
        {
            // Can disable voting on start, however even 0-1s delay wouldn't be game breaking and would ease performance
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            var gm = data.GameInstance;

            var currentRound = data.GetBaseRound;



            currentRound.IsQuestionVotingOpen = false;

            var playerIdAnswerId = new Dictionary<int, int>();

            foreach (var p in currentRound.NeutralRound.TerritoryAttackers)
            {
                // Check if the current attacker is a bot
                // Handle bot answer
                var isThisPlayerBot = gm.Participants.First(e => e.PlayerId == p.AttackerId).Player.IsBot;
                if(isThisPlayerBot)
                {
                    p.AttackerMChoiceQAnswerId = BotService.GenerateBotMCAnswerId(currentRound.Question.Answers.ToArray());
                }


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

            // Debug
            //if (currentRound.GameRoundNumber == 1)
            //{
            //    currentRound.GameRoundNumber = 5;
            //    timerWrapper.Data.CurrentGameRoundNumber = 5;
            //}


            // Go to next round
            currentRound.GameInstance.GameRoundNumber++;

            using (var db = contextFactory.CreateDbContext())
            {
                db.Update(gm);
                await db.SaveChangesAsync();
            }
            CommonTimerFunc.CalculateUserScore(gm);

            // Request a new batch of number questions from the question service
            if (gm.GameRoundNumber > data.LastNeutralMCRound)
            {
                // Create number question rounds if gm multiple choice neutral rounds are over
                var rounds = Create_Neutral_Number_Rounds(gm, timerWrapper);

                rounds.ForEach(e => gm.Rounds.Add(e));

                using (var db = contextFactory.CreateDbContext())
                {
                    db.Update(gm);
                    await db.SaveChangesAsync();

                    data.LastNeutralNumberRound = gm.Rounds.Where(e => e.AttackStage == AttackStage.NUMBER_NEUTRAL)
                        .OrderByDescending(e => e.GameRoundNumber)
                        .Select(e => e.GameRoundNumber).First();
                }


                CommonTimerFunc.RequestQuestions(messageBus, data.GameGlobalIdentifier, rounds, true);
            }

            // Response correct answer and all player answers
            var response = new MCPlayerQuestionAnswers()
            {
                CorrectAnswerId = currentRound.Question.Answers.FirstOrDefault(x => x.Correct).Id,
                PlayerAnswers = new List<PlayerIdAnswerId>(),
            };

            foreach (var pId in playerIdAnswerId)
            {
                response.PlayerAnswers.Add(new PlayerIdAnswerId()
                {
                    Id = pId.Key,
                    AnswerId = pId.Value,
                });
            }

            await hubContext.Clients.Groups(data.GameLink).MCQuestionPreviewResult(response);

            if (gm.GameRoundNumber > data.LastNeutralMCRound)
            {
                // Next action should be a number question related one
                timerWrapper.StartTimer(ActionState.SHOW_PREVIEW_GAME_MAP);
            }
            else
            {
                timerWrapper.StartTimer(ActionState.OPEN_PLAYER_ATTACK_VOTING);
            }
        }




        public async Task Open_Neutral_MultipleChoice_Attacker_Territory_Selecting(TimerWrapper timerWrapper)
        {
            var data = timerWrapper.Data;

            var gm = data.GameInstance;
            var currentRound = data.GetBaseRound;


            // Open this round for territory voting
            currentRound.IsTerritoryVotingOpen = true;

            using (var db = contextFactory.CreateDbContext())
            {
                db.Update(gm);
                await db.SaveChangesAsync();
            }

            var currentAttacker = currentRound.NeutralRound.TerritoryAttackers
                .First(x => x.AttackOrderNumber == currentRound.NeutralRound.AttackOrderNumber);

            var availableTerritories = gameTerritoryService
                .GetAvailableAttackTerritoriesNames(gm, currentAttacker.AttackerId, currentRound.GameInstanceId, true);

            await hubContext.Clients.Group(data.GameLink)
                .ShowRoundingAttacker(new RoundingAttackerRes(currentAttacker.AttackerId, availableTerritories));

            var res2 = mapper.Map<GameInstanceResponse>(data.GameInstance);
            await hubContext.Clients.Group(data.GameLink)
                .GetGameInstance(res2);


            var isAttackerBot = gm.Participants.First(e => e.PlayerId == currentAttacker.AttackerId).Player.IsBot;

            if(isAttackerBot)
            {
                timerWrapper.StartTimer(ActionState.CLOSE_PLAYER_ATTACK_VOTING, GameActionsTime.BotTerritorySelectTime);
                return;
            }

            timerWrapper.StartTimer(ActionState.CLOSE_PLAYER_ATTACK_VOTING);
        }

        public async Task Show_Neutral_MultipleChoice_Screen(TimerWrapper timerWrapper)
        {
            // Stop timer until we calculate the next action and client event
            timerWrapper.Stop();

            // Get the question and show it to the clients
            var data = timerWrapper.Data;

            var gm = timerWrapper.Data.GameInstance;
            var currentRound = data.GetBaseRound;

            var response = gM_DataExtractionService.GetCurrentStageQuestionResponse(gm);

            // Open this question for voting
            currentRound.Question.Round.IsQuestionVotingOpen = true;
            using (var db = contextFactory.CreateDbContext())
            {
                db.Update(gm);
                await db.SaveChangesAsync();
            }


            await hubContext.Clients.Group(data.GameLink).GetRoundQuestion(response);

            timerWrapper.StartTimer(ActionState.END_MULTIPLE_CHOICE_QUESTION);
        }
    }
}

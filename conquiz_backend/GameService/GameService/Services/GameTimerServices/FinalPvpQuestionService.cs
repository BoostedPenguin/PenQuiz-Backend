using AutoMapper;
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

namespace GameService.Services.GameTimerServices
{
    public interface IFinalPvpQuestionService
    {
        Task Final_Show_Pvp_Number_Screen(TimerWrapper timerWrapper);
        Task Final_Close_Pvp_Number_Question_Voting(TimerWrapper timerWrapper);
    }

    public class FinalPvpQuestionService : IFinalPvpQuestionService
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IHubContext<GameHub, IGameHub> hubContext;
        private readonly IGameTerritoryService gameTerritoryService;
        private readonly IMapper mapper;
        private readonly IMessageBusClient messageBus;

        public FinalPvpQuestionService(IDbContextFactory<DefaultContext> _contextFactory,
            IHubContext<GameHub, IGameHub> hubContext,
            IGameTerritoryService gameTerritoryService,
            IMapper mapper,
            IMessageBusClient messageBus)
        {
            contextFactory = _contextFactory;
            this.hubContext = hubContext;
            this.gameTerritoryService = gameTerritoryService;
            this.mapper = mapper;
            this.messageBus = messageBus;
        }

        public async Task Final_Show_Pvp_Number_Screen(TimerWrapper timerWrapper)
        {
            // Stop timer until we calculate the next action and client event
            timerWrapper.Stop();

            // Get the question and show it to the clients
            var data = timerWrapper.Data;
            var gm = data.GameInstance;
            using var db = contextFactory.CreateDbContext();

            var question = gm.Rounds
                .First(e => e.GameRoundNumber == gm.GameRoundNumber && e.AttackStage == AttackStage.FINAL_NUMBER_PVP)
                .Question;

            if (question == null)
                throw new ArgumentException($"There was no question generated for gameinstanceid: {data.GameInstanceId}, gameroundnumber: {data.CurrentGameRoundNumber}.");


            // Open this question for voting
            question.Round.IsQuestionVotingOpen = true;
            question.Round.QuestionOpenedAt = DateTime.Now;

            db.Update(question.Round);
            await db.SaveChangesAsync();

            var response = mapper.Map<QuestionClientResponse>(question);

            // If the round is a neutral one, then everyone can attack
            var terAttackers = question.Round.NeutralRound.TerritoryAttackers.ToList();
            if (terAttackers.Count() == 2)
            {
                response.IsNeutral = false;
                response.AttackerId = terAttackers[0].AttackerId;
                response.DefenderId = terAttackers[1].AttackerId;

                var participantsMapping = mapper.Map<ParticipantsResponse[]>(question.Round.GameInstance.Participants
                    .Where(x => terAttackers.Any(y => y.AttackerId == x.PlayerId))
                    .ToArray());
                response.Participants = participantsMapping;
            }
            else
            {
                response.IsNeutral = true;
                var participantsMapping = mapper.Map<ParticipantsResponse[]>(question.Round.GameInstance.Participants.ToArray());
                response.Participants = participantsMapping;
            }

            await hubContext.Clients.Group(data.GameLink).GetRoundQuestion(response);

            timerWrapper.StartTimer(ActionState.END_FINAL_PVP_NUMBER_QUESTION);
        }
        private readonly Random r = new Random();
        public async Task Final_Close_Pvp_Number_Question_Voting(TimerWrapper timerWrapper)
        {
            timerWrapper.Stop();
            var data = timerWrapper.Data;
            using var db = contextFactory.CreateDbContext();
            var gm = data.GameInstance;
            var currentRound = gm.Rounds
                .First(e => e.GameRoundNumber == data.CurrentGameRoundNumber && e.AttackStage == AttackStage.FINAL_NUMBER_PVP);


            currentRound.IsQuestionVotingOpen = false;

            var correctNumberQuestionAnswer = long.Parse(currentRound.Question.Answers.First().Answer);

            var attackerAnswers = currentRound
                .NeutralRound
                .TerritoryAttackers
                .Select(x => new
                {
                    x.AnsweredAt,
                    x.AttackerId,
                    x.AttackerNumberQAnswer,
                });

            // 2 things need to happen:
            // First check for closest answer to the correct answer
            // The closest answer wins the round
            // If 2 or more answers are same check answeredAt and the person who answered
            // The quickest wins the round

            // Use case: If no person answered, then reward the a random territory to a random person
            // (to prevent empty rounds)

            var clientResponse = new NumberPlayerQuestionAnswers()
            {
                CorrectAnswer = correctNumberQuestionAnswer.ToString(),
                PlayerAnswers = new List<NumberPlayerIdAnswer>(),
            };


            foreach (var at in attackerAnswers)
            {
                if (at.AttackerNumberQAnswer == null)
                {
                    clientResponse.PlayerAnswers.Add(new NumberPlayerIdAnswer()
                    {
                        Answer = null,
                        TimeElapsed = "",
                        PlayerId = at.AttackerId,
                    });
                    continue;
                }
                // Solution is to absolute value both of them
                // Then subtract from each other
                // Also have to make an absolute value the result

                var difference = Math.Abs(correctNumberQuestionAnswer) - Math.Abs((long)at.AttackerNumberQAnswer);
                var absoluteDifference = Math.Abs(difference);

                var timeElapsed = Math.Abs((currentRound.QuestionOpenedAt - at.AnsweredAt).Value.TotalSeconds);

                clientResponse.PlayerAnswers.Add(new NumberPlayerIdAnswer()
                {
                    Answer = at.AttackerNumberQAnswer.ToString(),
                    TimeElapsedNumber = timeElapsed,
                    TimeElapsed = timeElapsed.ToString("0.00"),
                    DifferenceWithCorrectNumber = absoluteDifference,
                    PlayerId = at.AttackerId,
                    DifferenceWithCorrect = absoluteDifference.ToString(),
                });
            }

            // After calculating all answer differences, see who is the winner

            // If all player answers are null (no one answered) give a random person a random territory
            // Will prioritize matching borders instead of totally random territories
            // Will make the map look more connected

            int winnerId;
            if(clientResponse.PlayerAnswers.Count() == 3)
            {
                int secondaryWinnerId = 0;
                // If no player answered
                // Give out a random territory to one of them
                if (clientResponse.PlayerAnswers.All(x => x.Answer == null))
                {
                    var randomWinnerIndex = r.Next(0, clientResponse.PlayerAnswers.Count());
                    winnerId = clientResponse.PlayerAnswers[randomWinnerIndex].PlayerId;

                    while (secondaryWinnerId == 0)
                    {
                        var randomWinnerId = clientResponse.PlayerAnswers[r.Next(0, clientResponse.PlayerAnswers.Count())].PlayerId;
                        if (randomWinnerId == winnerId)
                            continue;

                        secondaryWinnerId = randomWinnerId;
                    }
                }
                else
                {
                    // Order by answer first
                    // Then orderby answeredat
                    var orderedAnswers = clientResponse.PlayerAnswers
                        .Where(x => x.Answer != null && x.TimeElapsed != null)
                        .OrderBy(x => x.DifferenceWithCorrectNumber)
                        .ThenBy(x => x.TimeElapsedNumber)
                        .Select(x => x.PlayerId)
                        .ToList();

                    winnerId = orderedAnswers[0];
                    secondaryWinnerId = orderedAnswers[1];
                }

                currentRound.GameInstance.Participants.First(x => x.PlayerId == winnerId).FinalQuestionScore = 400;
                currentRound.GameInstance.Participants.First(x => x.PlayerId == secondaryWinnerId).FinalQuestionScore = 300;

            }
            else
            {

                // If no player answered
                // Give out a random territory to one of them
                if (clientResponse.PlayerAnswers.All(x => x.Answer == null))
                {
                    var randomWinnerIndex = r.Next(0, clientResponse.PlayerAnswers.Count());
                    winnerId = clientResponse.PlayerAnswers[randomWinnerIndex].PlayerId;
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

                currentRound.GameInstance.Participants.First(x => x.PlayerId == winnerId).FinalQuestionScore = 400;
                clientResponse.PlayerAnswers.ForEach(x => x.Winner = x.PlayerId == winnerId);

            }
            db.Update(currentRound);
            await db.SaveChangesAsync();

            // Tell clients
            await hubContext.Clients.Groups(data.GameLink).NumberQuestionPreviewResult(clientResponse);

            timerWrapper.StartTimer(ActionState.END_GAME);
        }
    }
}

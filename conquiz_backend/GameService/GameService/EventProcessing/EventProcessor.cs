using AutoMapper;
using GameService.Context;
using GameService.Dtos;
using GameService.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;

namespace GameService.EventProcessing
{
    public interface IEventProcessor
    {
        void ProcessEvent(string message);
    }

    public class EventProcessor : IEventProcessor
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IMapper mapper;

        public EventProcessor(IDbContextFactory<DefaultContext> contextFactory, IMapper mapper)
        {
            this.contextFactory = contextFactory;
            this.mapper = mapper;
        }
        public void ProcessEvent(string message)
        {
            var eventType = DetermineEvent(message);

            switch (eventType)
            {
                case EventType.UserPublished:
                    AddUser(message);
                    break;
                case EventType.QuestionsReceived:
                    var result = JsonSerializer.Deserialize<QResponse>(message);
                    AddGameQuestions(result);
                    break;
                case EventType.CapitalQuestionResponse:
                    AddCapitalQuestions(message);
                    break;
                default:
                    break;
            }
        }

        private void AddCapitalQuestions(string message)
        {
            var result = JsonSerializer.Deserialize<QResponse>(message);

            using var db = contextFactory.CreateDbContext();

            var capitalRounds = db.CapitalRound
                .Include(x => x.PvpRound)
                .ThenInclude(x => x.Round)
                .Where(x => x.PvpRound.Round.GameInstanceId == result.GameInstanceId)
                .ToList();

            var mapped = mapper.Map<Questions[]>(result.QuestionResponses);

            foreach (var receivedQuestion in mapped)
            {
                var capitalRound = capitalRounds.Where(x => x.Id == receivedQuestion.RoundId).FirstOrDefault();

                if (capitalRound == null)
                {
                    Console.WriteLine($"--> Capital Round with ID: {receivedQuestion.RoundId}. Doesn't exist.");
                    continue;
                }

                // Ensure these id's are null
                receivedQuestion.RoundId = null;
                receivedQuestion.PvpRoundId = null;

                if (receivedQuestion.Type == "number")
                {
                    receivedQuestion.CapitalRoundNumberId = capitalRound.Id;
                    receivedQuestion.CapitalRoundMCId = null;
                }
                else
                {
                    receivedQuestion.CapitalRoundMCId = capitalRound.Id;
                    receivedQuestion.CapitalRoundNumberId = null;

                }
                db.AddAsync(receivedQuestion);
            }

            db.SaveChanges();
        }

        private void AddGameQuestions(QResponse questionsResponse)
        {
            using var db = contextFactory.CreateDbContext();

            var gm = db.GameInstance
                .Include(x => x.Rounds)
                .ThenInclude(x => x.PvpRound)
                .Include(x => x.Rounds)
                .ThenInclude(x => x.Question)
                .Where(x => x.Id == questionsResponse.GameInstanceId)
                .FirstOrDefault();


            if (gm == null)
            {
                Console.WriteLine("--> Game instance doesn't exist");
                return;
            }

            var mapped = mapper.Map<Questions[]>(questionsResponse.QuestionResponses);
            foreach (var receivedQuestion in mapped)
            {
                var gameRound = gm.Rounds.Where(x => x.Id == receivedQuestion.RoundId).FirstOrDefault();



                if (gameRound == null)
                {
                    Console.WriteLine($"--> Round with ID: {receivedQuestion.RoundId}. Doesn't exist.");
                    continue;
                }

                // If secondary number question, switch to pvproundid instead of main roundid
                if ((gameRound.AttackStage == AttackStage.NUMBER_PVP || gameRound.AttackStage == AttackStage.MULTIPLE_PVP)
                    && receivedQuestion.Type == "number")
                {
                    receivedQuestion.PvpRoundId = gameRound.PvpRound.Id;
                    receivedQuestion.RoundId = null;
                }
                db.AddAsync(receivedQuestion);
            }

            db.SaveChanges();
        }

        private void AddUser(string userPublishedMessage)
        {
            using var db = contextFactory.CreateDbContext();

            var userPublishedDto = JsonSerializer.Deserialize<UserPublishedDto>(userPublishedMessage);

            try
            {
                var user = mapper.Map<Users>(userPublishedDto);
                if (db.Users.FirstOrDefault(x => x.ExternalId == user.ExternalId) == null)
                {
                    db.Users.Add(user);
                    db.SaveChanges();
                    Console.WriteLine("--> User added to GameService database.");

                }
                else
                {
                    Console.WriteLine("--> User already exists. Not adding to db.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Could not add User to DB: {ex.Message}");
            }
        }

        private EventType DetermineEvent(string notificationMessage)
        {
            Console.WriteLine("--> Determining Event");

            var eventType = JsonSerializer.Deserialize<GenericEventDto>(notificationMessage);

            switch (eventType.Event)
            {
                case "Questions_MultipleChoice_Neutral_Response":
                    Console.WriteLine("Question Response Event Detected");
                    return EventType.QuestionsReceived;
                case "User_Published":
                    Console.WriteLine("User Published Event Detected");
                    return EventType.UserPublished;
                case "Capital_Question_Response":
                    Console.WriteLine("Capital Question Response Detected");
                    return EventType.CapitalQuestionResponse;
                default:
                    Console.WriteLine("--> Could not determine the event type");
                    return EventType.Undetermined;
            }
        }
    }

    enum EventType
    {
        CapitalQuestionResponse,
        QuestionsReceived,
        UserPublished,
        Undetermined
    }
}
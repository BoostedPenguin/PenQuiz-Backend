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
                default:
                    break;
            }
        }

        private void AddGameQuestions(QResponse questionsResponse)
        {
            using var db = contextFactory.CreateDbContext();

            var gm = db.GameInstance
                .Include(x => x.Rounds)
                .ThenInclude(x => x.Question)
                .Where(x => x.Id == questionsResponse.GameInstanceId)
                .FirstOrDefault();


            if(gm == null)
            {
                Console.WriteLine("--> Game instance doesn't exist");
                return;
            }

            var mapped = mapper.Map<Questions[]>(questionsResponse.QuestionResponses);
            foreach(var receivedQuestion in mapped)
            {
                var gameRound = gm.Rounds.Where(x => x.Id == receivedQuestion.RoundsId).FirstOrDefault();

                if(gameRound == null)
                {
                    Console.WriteLine($"--> Round with ID: {receivedQuestion.RoundsId}. Doesn't exist.");
                    continue;
                }
                gameRound.Question = receivedQuestion;
                db.Update(gameRound);
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
                default:
                    Console.WriteLine("--> Could not determine the event type");
                    return EventType.Undetermined;
            }
        }
    }

    enum EventType
    {
        QuestionsReceived,
        UserPublished,
        Undetermined
    }
}
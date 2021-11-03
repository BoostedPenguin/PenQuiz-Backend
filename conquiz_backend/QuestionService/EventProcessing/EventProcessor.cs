using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QuestionService.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.EventProcessing
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
                default:
                    break;
            }
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
        QuestionResponse,
        Undetermined
    }
}

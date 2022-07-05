using AccountService.Data;
using AccountService.Data.Models;
using AccountService.Dtos;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.Json;

namespace AccountService.EventProcessing
{
    public interface IEventProcessor
    {
        void ProcessEvent(string message);
    }

    public class EventProcessor : IEventProcessor
    {
        private readonly IDbContextFactory<AppDbContext> contextFactory;
        private readonly IMapper mapper;
        private readonly ILogger<EventProcessor> logger;

        public EventProcessor(IDbContextFactory<AppDbContext> contextFactory, IMapper mapper, ILogger<EventProcessor> logger)
        {
            this.contextFactory = contextFactory;
            this.mapper = mapper;
            this.logger = logger;
        }

        public void ProcessEvent(string message)
        {
            var eventType = DetermineEvent(message);

            switch (eventType)
            {
                case EventType.CharacterPublished:
                    AddCharacter(message);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Adds or replaces existing character
        /// </summary>
        /// <param name="message"></param>
        private void AddCharacter(string message)
        {
            var receivedObj = JsonSerializer.Deserialize<CharacterResponse>(message);

            using var db = contextFactory.CreateDbContext();

            var existing = db.Characters.FirstOrDefault(e => e.CharacterGlobalIdentifier == receivedObj.CharacterGlobalIdentifier);

            if (existing != null)
            {
                existing.Name = receivedObj.Name;
                existing.Price = receivedObj.Price;
                existing.PricingType = receivedObj.PricingType;

                db.Update(existing);
                db.SaveChanges();
                return;
            }

            var character = new Character()
            {
                CharacterGlobalIdentifier = receivedObj.CharacterGlobalIdentifier,
                CharacterType = receivedObj.CharacterType,
                Name = receivedObj.Name,
                Price = receivedObj.Price,
                PricingType = receivedObj.PricingType,
            };

            db.Add(character);
            db.SaveChanges();
        }

        private EventType DetermineEvent(string notificationMessage)
        {
            var eventType = JsonSerializer.Deserialize<GenericEventDto>(notificationMessage);

            logger.LogDebug($"Determining Event: {eventType.Event}");

            switch (eventType.Event)
            {
                case "Character_Published":
                    logger.LogInformation($"Characted response event detected!");
                    return EventType.CharacterPublished;
                default:
                    logger.LogDebug($"Could not determine the event type");
                    return EventType.Undetermined;
            }
        }

        enum EventType
        {
            CharacterPublished,
            Undetermined
        }
    }
}

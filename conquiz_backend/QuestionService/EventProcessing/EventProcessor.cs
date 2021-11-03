using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QuestionService.Context;
using QuestionService.Dtos;
using QuestionService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
        private readonly IOpenDBService openDBService;

        public EventProcessor(IDbContextFactory<DefaultContext> contextFactory, IMapper mapper, IOpenDBService openDBService)
        {
            this.contextFactory = contextFactory;
            this.mapper = mapper;
            this.openDBService = openDBService;
        }

        public void ProcessEvent(string message)
        {
            var eventType = DetermineEvent(message);

            switch (eventType)
            {
                case EventType.QuestionRequest:
                    var questionRequest = JsonSerializer.Deserialize<QuestionRequest>(message);
                    openDBService.PublishMultipleChoiceQuestion(questionRequest.GameInstanceId);
                    break;
                default:
                    break;
            }
        }

        private EventType DetermineEvent(string notificationMessage)
        {
            Console.WriteLine("--> Determining Event");

            var eventType = JsonSerializer.Deserialize<GenericEventDto>(notificationMessage);

            switch (eventType.Event)
            {
                case "Question_Request":
                    Console.WriteLine("Question Request Event Detected");
                    return EventType.QuestionRequest;
                default:
                    Console.WriteLine("--> Could not determine the event type");
                    return EventType.Undetermined;
            }
        }
    }

    enum EventType
    {
        QuestionRequest,
        Undetermined
    }
}

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QuestionService.Context;
using QuestionService.Dtos;
using QuestionService.MessageBus;
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
        Task ProcessEvent(string message);
    }

    public class EventProcessor : IEventProcessor
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;
        private readonly IMapper mapper;
        private readonly IOpenDBService openDBService;
        private readonly INumberQuestionsService numberQuestionsService;
        private readonly IMessageBusClient messageBus;

        public EventProcessor(IDbContextFactory<DefaultContext> contextFactory, IMapper mapper, IOpenDBService openDBService, INumberQuestionsService numberQuestionsService, IMessageBusClient messageBus)
        {
            this.contextFactory = contextFactory;
            this.mapper = mapper;
            this.openDBService = openDBService;
            this.numberQuestionsService = numberQuestionsService;
            this.messageBus = messageBus;
        }

        public async Task ProcessEvent(string message)
        {
            var eventType = DetermineEvent(message);

            switch (eventType)
            {
                case EventType.QuestionRequest:
                    var questionRequest = JsonSerializer.Deserialize<QuestionRequest>(message);

                    var sessionToken = 
                        await openDBService.GenerateSessionToken(questionRequest.GameInstanceId);
                    
                    var mulChoiceQuestions = await 
                        openDBService.GetMultipleChoiceQuestion(sessionToken.Token, questionRequest.MultipleChoiceQuestionsRoundId);

                    var numberQuestions = 
                        await numberQuestionsService.GetNumberQuestions(questionRequest.NumberQuestionsRoundId, sessionToken.Token, sessionToken.InternalGameInstanceId);

                    // Add both questions
                    mulChoiceQuestions.AddRange(numberQuestions);

                    // Generate backup number question for every number question in case both people answer correctly
                    // Only for PVP, not needed for neutral territories
                    if (!questionRequest.IsNeutralGeneration)
                    {
                        var secondaryNumQuestions = await numberQuestionsService.GetNumberQuestions(questionRequest.MultipleChoiceQuestionsRoundId, sessionToken.Token, sessionToken.InternalGameInstanceId);
                        mulChoiceQuestions.AddRange(secondaryNumQuestions);
                    }

                    var mappedQuestions = mapper.Map<QuestionResponse[]>(mulChoiceQuestions);

                    var response = new QResponse()
                    {
                        GameInstanceId = questionRequest.GameInstanceId,
                        QuestionResponses = mappedQuestions,
                        Event = "Questions_MultipleChoice_Neutral_Response",
                    };

                    messageBus.PublishRequestedQuestions(response);

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

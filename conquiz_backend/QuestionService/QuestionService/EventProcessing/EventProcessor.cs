using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestionService.Context;
using QuestionService.Data;
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
        private readonly IMapper mapper;
        private readonly IMCQuestionsService mcQuestionService;
        private readonly INumberQuestionsService numberQuestionsService;
        private readonly IMessageBusClient messageBus;

        public EventProcessor(IMapper mapper, IMCQuestionsService mcQuestionService, INumberQuestionsService numberQuestionsService, IMessageBusClient messageBus)
        {
            this.mapper = mapper;
            this.mcQuestionService = mcQuestionService;
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
                        await mcQuestionService.GenerateSessionToken(questionRequest.GameGlobalIdentifier);
                    
                    var mulChoiceQuestions = await 
                        mcQuestionService.GetMultipleChoiceQuestion(sessionToken.Token, questionRequest.MultipleChoiceQuestionsRoundId);

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
                        GameGlobalIdentifier = questionRequest.GameGlobalIdentifier,
                        QuestionResponses = mappedQuestions,
                        Event = "Questions_MultipleChoice_Neutral_Response",
                    };

                    messageBus.PublishRequestedQuestions(response);

                    break;
                case EventType.CapitalQuestionRequest:
                    await CapitalRequest(message);
                    break;
                case EventType.FinalQuestionRequest:
                    await FinalQuestionRequest(message);
                    break;
                default:
                    break;
            }
        }

        private async Task FinalQuestionRequest(string message)
        {
            var questionRequest = JsonSerializer.Deserialize<RequestFinalNumberQuestionDto>(message);

            var sessionToken =
                await mcQuestionService.GenerateSessionToken(questionRequest.GameGlobalIdentifier);
            
            var numberQuestions =
                await numberQuestionsService.GetNumberQuestions(new List<int> { questionRequest.QuestionFinalRoundId }, sessionToken.Token, sessionToken.InternalGameInstanceId);
            
            var mappedQuestions = mapper.Map<QuestionResponse[]>(numberQuestions);

            var response = new QResponse()
            {
                GameGlobalIdentifier = questionRequest.GameGlobalIdentifier,
                QuestionResponses = mappedQuestions,
                Event = "FinalNumber_Question_Response",
            };

            messageBus.PublishRequestedQuestions(response);
        }

        private async Task CapitalRequest(string message)
        {
            var capitalRequest = JsonSerializer.Deserialize<CapitalQuestionRequest>(message);

            var sessionToken =
                await mcQuestionService.GenerateSessionToken(capitalRequest.GameGlobalIdentifier);

            var mulChoiceQuestions = await
                mcQuestionService.GetMultipleChoiceQuestion(sessionToken.Token, capitalRequest.QuestionsCapitalRoundId);

            var numberQuestions =
                await numberQuestionsService.GetNumberQuestions(capitalRequest.QuestionsCapitalRoundId, sessionToken.Token, sessionToken.InternalGameInstanceId);

            // Add both questions
            mulChoiceQuestions.AddRange(numberQuestions);

            var mappedQuestions = mapper.Map<QuestionResponse[]>(mulChoiceQuestions);

            var response = new QResponse()
            {
                GameGlobalIdentifier = capitalRequest.GameGlobalIdentifier,
                QuestionResponses = mappedQuestions,
                Event = "Capital_Question_Response",
            };

            messageBus.PublishRequestedQuestions(response);
        }

        private EventType DetermineEvent(string notificationMessage)
        {
            Console.WriteLine("--> Determining Event");

            var eventType = JsonSerializer.Deserialize<GenericEventDto>(notificationMessage);

            switch (eventType.Event)
            {
                case "Capital_Question_Request":
                    Console.WriteLine("Capital Question Request Event Detected");
                    return EventType.CapitalQuestionRequest;
                case "Question_Request":
                    Console.WriteLine("Question Request Event Detected");
                    return EventType.QuestionRequest;
                case "FinalNumber_Question_Request":
                    return EventType.FinalQuestionRequest;
                default:
                    Console.WriteLine("--> Could not determine the event type");
                    return EventType.Undetermined;
            }
        }
    }

    enum EventType
    {
        FinalQuestionRequest,
        CapitalQuestionRequest,
        QuestionRequest,
        Undetermined
    }
}

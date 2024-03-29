using AutoMapper;
using GameService.Context;
using GameService.Data;
using GameService.Data.Models;
using GameService.Dtos;
using GameService.Services.GameTimerServices;
using Google.Apis.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<EventProcessor> logger;
        private readonly IGameTimerService gameTimerService;

        public EventProcessor(IDbContextFactory<DefaultContext> contextFactory, IMapper mapper, ILogger<EventProcessor> logger, IGameTimerService gameTimerService)
        {
            this.contextFactory = contextFactory;
            this.mapper = mapper;
            this.logger = logger;
            this.gameTimerService = gameTimerService;
        }
        public void ProcessEvent(string message)
        {
            try
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
                    case EventType.FinalQuestionResponse:
                        AddFinalQuestionResponse(message);
                        break;
                    case EventType.UserCharacterResponse:
                        UserCharacterResponse(message);
                        break;
                    default:
                        break;
                }
            }
            catch(Exception ex)
            {
                logger.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Adds a reference from a user to a character as "Ownership"
        /// </summary>
        /// <param name="message"></param>
        private void UserCharacterResponse(string message)
        {
            var result = JsonSerializer.Deserialize<UserCharacterEventResponse>(message);

            using var db = contextFactory.CreateDbContext();

            var character = db.Characters.FirstOrDefault(e => e.CharacterGlobalIdentifier == result.CharacterGlobalId);

            if(character == null)
            {
                logger.LogWarning($"Character {character.CharacterGlobalIdentifier} does not exist!");
                return;
            }

            if(character.PricingType == CharacterPricingType.FREE)
            {
                logger.LogWarning($"Character {character.CharacterGlobalIdentifier} is free! Not added to user!");
                return;
            }

            var user = db.Users.Include(e => e.OwnedCharacters).FirstOrDefault(e => e.UserGlobalIdentifier == result.UserGlobalId);

            if(user == null)
            {
                logger.LogWarning($"User {result.UserGlobalId} does not exist!");
                return;
            }

            var existingCharacter = user.OwnedCharacters.FirstOrDefault(e => e.CharacterGlobalIdentifier == character.CharacterGlobalIdentifier);

            if(existingCharacter != null)
            {
                logger.LogWarning($"User {user.UserGlobalIdentifier}, already has {character.CharacterGlobalIdentifier} character in his inventory");
                return;
            }
            user.OwnedCharacters.Add(character);

            db.Update(user);
            db.SaveChanges();
        }

        private void AddFinalQuestionResponse(string message)
        {
            var result = JsonSerializer.Deserialize<QResponse>(message);
            
            var gm = gameTimerService
                .GameTimers
                .FirstOrDefault(e => e.Data.GameGlobalIdentifier == result.GameGlobalIdentifier)
                .Data.GameInstance;

            var finalRound = gm.Rounds
                .Where(e => e.Id == result.QuestionResponses
                .First().RoundId && e.AttackStage == AttackStage.FINAL_NUMBER_PVP).FirstOrDefault();


            // Get the current timer


            if (finalRound == null)
            {
                logger.LogWarning($"Capital Round with ID: {result.QuestionResponses.First().RoundId}. Doesn't exist.");
                return;
            }

            var mapped = mapper.Map<Questions>(result.QuestionResponses.First());

            finalRound.Question = mapped;
        }

        private void AddCapitalQuestions(string message)
        {
            var result = JsonSerializer.Deserialize<QResponse>(message);

            var gm = gameTimerService
                .GameTimers
                .FirstOrDefault(e => e.Data.GameGlobalIdentifier == result.GameGlobalIdentifier)
                .Data.GameInstance;

            var capitalRounds = gm.Rounds
                .Where(e => e.PvpRound != null && e.PvpRound.CapitalRounds != null)
                .SelectMany(e => e.PvpRound.CapitalRounds).ToList();



            var mapped = mapper.Map<Questions[]>(result.QuestionResponses);

            foreach (var receivedQuestion in mapped)
            {
                var capitalRound = capitalRounds.Where(x => x.Id == receivedQuestion.RoundId).FirstOrDefault();

                if (capitalRound == null)
                {
                    logger.LogWarning($"Capital Round with ID: {receivedQuestion.RoundId}. Doesn't exist.");
                    continue;
                }

                // Ensure these id's are null
                receivedQuestion.RoundId = null;
                receivedQuestion.PvpRoundId = null;

                if (receivedQuestion.Type == "number")
                {
                    receivedQuestion.CapitalRoundNumberId = capitalRound.Id;
                    receivedQuestion.CapitalRoundMCId = null;

                    capitalRound.CapitalRoundNumberQuestion = receivedQuestion;
                }
                else
                {
                    receivedQuestion.CapitalRoundMCId = capitalRound.Id;
                    receivedQuestion.CapitalRoundNumberId = null;

                    capitalRound.CapitalRoundMultipleQuestion = receivedQuestion;
                }
            }
        }


        /// <summary>
        /// Attaches the questions to the current game instance located in gametimerservice
        /// On next event the questions will get saved to the database
        /// </summary>
        /// <param name="questionsResponse"></param>
        private void AddGameQuestions(QResponse questionsResponse)
        {
            // Get the current timer
            var gm = gameTimerService
                .GameTimers
                .FirstOrDefault(e => e.Data.GameGlobalIdentifier == questionsResponse.GameGlobalIdentifier)
                .Data.GameInstance;


            if (gm == null)
            {
                logger.LogWarning($"Game instance doesnt exist. Global ID: {questionsResponse.GameGlobalIdentifier}");
                return;
            }

            if(gm.GameState != GameState.IN_PROGRESS)
            {
                logger.LogWarning($"Game instance is no longer in progress. Global ID: {questionsResponse.GameGlobalIdentifier}");
                return;
            }

            var mapped = mapper.Map<Questions[]>(questionsResponse.QuestionResponses);
            foreach (var receivedQuestion in mapped)
            {
                var gameRound = gm.Rounds.Where(x => x.Id == receivedQuestion.RoundId).FirstOrDefault();



                if (gameRound == null)
                {
                    logger.LogWarning($"Round with ID: {receivedQuestion.RoundId}. Doesn't exist.");
                    continue;
                }

                // If secondary number question, switch to pvproundid instead of main roundid
                if ((gameRound.AttackStage == AttackStage.NUMBER_PVP || gameRound.AttackStage == AttackStage.MULTIPLE_PVP)
                    && receivedQuestion.Type == "number")
                {
                    receivedQuestion.PvpRoundId = gameRound.PvpRound.Id;
                    receivedQuestion.RoundId = null;

                    gameRound.PvpRound.NumberQuestion = receivedQuestion;
                    continue;
                }
                gameRound.Question = receivedQuestion;
            }
        }

        private void AddUser(string userPublishedMessage)
        {
            using var db = contextFactory.CreateDbContext();

            var userPublishedDto = JsonSerializer.Deserialize<UserPublishedDto>(userPublishedMessage);

            try
            {
                var user = mapper.Map<Users>(userPublishedDto);
                
                var existing = db.Users.FirstOrDefault(x => x.UserGlobalIdentifier == user.UserGlobalIdentifier);

                if(existing != null)
                {
                    logger.LogInformation($"User already exists in game service database. Skip.");
                    return;
                }

                // Migrate existing people if they have a username but don't have a global identifier
                var old = db.Users.FirstOrDefault(x => x.Username == user.Username && string.IsNullOrEmpty(x.UserGlobalIdentifier));
                


                if (old != null)
                {
                    old.UserGlobalIdentifier = userPublishedDto.UserGlobalIdentifier;
                    db.Update(old);
                    db.SaveChanges();
                    logger.LogInformation($"Old user global identifier added to GameService database.");
                    return;
                }

                if (db.Users.FirstOrDefault(x => x.UserGlobalIdentifier == user.UserGlobalIdentifier) == null)
                {
                    db.Users.Add(user);
                    db.SaveChanges();

                    logger.LogInformation($"User added to GameService database.");
                }
                else
                {
                    logger.LogInformation($"User already exists. Not adding to db.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Could not add User to DB: {ex.Message}");
            }
        }

        private EventType DetermineEvent(string notificationMessage)
        {


            var eventType = JsonSerializer.Deserialize<GenericEventDto>(notificationMessage);

            logger.LogDebug($"Determining Event: {eventType.Event}");

            switch (eventType.Event)
            {
                case "FinalNumber_Question_Response":
                    logger.LogDebug($"Final question Response Event Detected");
                    return EventType.FinalQuestionResponse;
                case "Questions_MultipleChoice_Neutral_Response":
                    logger.LogDebug($"Question Response Event Detected");
                    return EventType.QuestionsReceived;
                case "User_Published":
                    logger.LogDebug($"User Published Event Detected");
                    return EventType.UserPublished;
                case "Capital_Question_Response":
                    logger.LogDebug($"Capital Question Response Detected");
                    return EventType.CapitalQuestionResponse;
                case "User_Character_Response":
                    logger.LogDebug($"User Character Response Detected");
                    return EventType.UserCharacterResponse;
                default:
                    logger.LogDebug($"Could not determine the event type");
                    return EventType.Undetermined;
            }
        }
    }

    enum EventType
    {
        FinalQuestionResponse,
        CapitalQuestionResponse,
        QuestionsReceived,
        UserPublished,
        UserCharacterResponse,
        Undetermined
    }
}
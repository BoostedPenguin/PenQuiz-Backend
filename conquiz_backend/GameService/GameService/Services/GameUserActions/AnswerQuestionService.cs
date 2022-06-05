using GameService.Data.Models;
using GameService.Services.Extensions;
using GameService.Services.GameTimerServices;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameService.Services.GameUserActions
{
    public interface IAnswerQuestionService
    {
        void AnswerQuestion(string answerIdString);
    }

    public class AnswerQuestionService : IAnswerQuestionService
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IGameTimerService gameTimerService;

        public AnswerQuestionService(IHttpContextAccessor httpContextAccessor, IGameTimerService gameTimerService)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.gameTimerService = gameTimerService;
        }

        private static AnsweredResponse AnswerCapitalMultipleQuestion(string answerIdString, Round currentRound, CapitalRound capitalRound, int userId)
        {
            bool success = int.TryParse(answerIdString, out int answerIdMPvp);
            if (!success)
                throw new AnswerSubmittedGameException("You didn't provide a valid number");

            // Requesting user is the attacker
            if (!capitalRound.CapitalRoundMultipleQuestion.Answers.Any(x => x.Id == answerIdMPvp))
                throw new AnswerSubmittedGameException("The provided answerID isn't valid for this question.");

            if (userId != currentRound.PvpRound.AttackerId && userId != currentRound.PvpRound.DefenderId)
                throw new AnswerSubmittedGameException("You can't vote for this question");

            var userAttacking = capitalRound.CapitalRoundUserAnswers
                .FirstOrDefault(x => x.UserId == userId);

            if (userAttacking != null && userAttacking.MChoiceQAnswerId != null)
                throw new ArgumentException("This user already voted for this question");

            var result = new CapitalRoundAnswers()
            {
                MChoiceQAnswerId = answerIdMPvp,
                UserId = userId
            };

            capitalRound.CapitalRoundUserAnswers.Add(result);


            var response = new AnsweredResponse(new List<AnsweredResponse.UserAnswer>()
            {
                new AnsweredResponse.UserAnswer(currentRound.PvpRound.AttackerId,
                    capitalRound.CapitalRoundUserAnswers.Any(e => e.UserId == currentRound.PvpRound.AttackerId)),

                new AnsweredResponse.UserAnswer(currentRound.PvpRound.DefenderId ?? 0,
                    capitalRound.CapitalRoundUserAnswers.Any(e => e.UserId == currentRound.PvpRound.DefenderId)),
            });

            return response;
        }


        private static AnsweredResponse AnswerCapitalNumberQuestion(string answerIdString, Round currentRound, DateTime answeredAt, CapitalRound capitalRound, int userId)
        {
            bool successNPvp = long.TryParse(answerIdString, out long answerIdNPvp);
            if (!successNPvp)
                throw new AnswerSubmittedGameException("You didn't provide a valid number");

            var pvpAttacker = capitalRound.CapitalRoundUserAnswers
                .FirstOrDefault(x => x.UserId == userId);

            if (pvpAttacker == null)
                throw new AnswerSubmittedGameException("User doesn't have an existing multiple choice answer. Fatal error.");

            if (pvpAttacker.NumberQAnswer != null)
                throw new AnswerSubmittedGameException("You already voted for this question");

            pvpAttacker.NumberQAnsweredAt = answeredAt;
            pvpAttacker.NumberQAnswer = answerIdNPvp;



            var response = new AnsweredResponse(new List<AnsweredResponse.UserAnswer>()
            {
                new AnsweredResponse.UserAnswer(currentRound.PvpRound.AttackerId,
                    capitalRound.CapitalRoundUserAnswers.Any(e => e.UserId == currentRound.PvpRound.AttackerId && e.NumberQAnswer is not null)),
                new AnsweredResponse.UserAnswer(currentRound.PvpRound.DefenderId ?? 0,
                    capitalRound.CapitalRoundUserAnswers.Any(e => e.UserId == currentRound.PvpRound.DefenderId && e.NumberQAnswer is not null)),
            });

            return response;
        }


        private static AnsweredResponse CapitalStageAnswer(string answerIdString, ref Round currentRound, DateTime answeredAt, int userId)
        {
            var capitalRound =
                currentRound
                .PvpRound
                .CapitalRounds
                .FirstOrDefault(x => !x.IsCompleted && x.IsQuestionVotingOpen);

            if (capitalRound == null)
                throw new AnswerSubmittedGameException("This capital round is null. Fatal error");

            if (!capitalRound.IsQuestionVotingOpen)
                throw new AnswerSubmittedGameException("The voting stage for this question is either over or not started.");

            var response = capitalRound.CapitalRoundAttackStage switch
            {
                CapitalRoundAttackStage.MULTIPLE_CHOICE_QUESTION => AnswerCapitalMultipleQuestion(answerIdString, currentRound, capitalRound, userId),
                CapitalRoundAttackStage.NUMBER_QUESTION => AnswerCapitalNumberQuestion(answerIdString, currentRound, answeredAt, capitalRound, userId),
                _ => throw new ArgumentException("Unhandled capital attack stage")
            };

            return response;
        }

        private AnsweredResponse AnswerFinalQuestion(string answerIdString, ref Round currentRound, int userId)
        {

            bool successNNeutral = long.TryParse(answerIdString, out long answerIdNNeutral);
            if (!successNNeutral)
                throw new AnswerSubmittedGameException("You didn't provide a valid number");

            var pAttacker = currentRound
                .NeutralRound
                .TerritoryAttackers
                .First(x => x.AttackerId == userId);

            if (pAttacker.AttackerNumberQAnswer != null)
                throw new AnswerSubmittedGameException("You already voted for this question");

            pAttacker.AnsweredAt = DateTime.Now;
            pAttacker.AttackerNumberQAnswer = answerIdNNeutral;


            return new AnsweredResponse(currentRound
                .NeutralRound
                .TerritoryAttackers
                    .Select(e => new AnsweredResponse.UserAnswer(e.AttackerId, e.AttackerNumberQAnswer is not null))
                .ToList());
        }

        private class AnsweredResponse
        {
            public List<UserAnswer> AllRoundUserAnswers { get; }
            public AnsweredResponse(List<UserAnswer> userAnswers)
            {
                AllRoundUserAnswers = userAnswers;
            }

            internal class UserAnswer
            {
                public UserAnswer(int userId, bool hasAnswer)
                {
                    UserId = userId;
                    HasAnswer = hasAnswer;
                }
                public int UserId { get; }

                public bool HasAnswer { get; }
            }
        }

        private static AnsweredResponse AnswerMultipleNeutralQuestion(string answerIdString, Round currentRound, int userId)
        {
            bool successMNeutral = int.TryParse(answerIdString, out int answerIdMNeutral);
            if (!successMNeutral)
                throw new AnswerSubmittedGameException("You didn't provide a valid number");

            if (!currentRound.Question.Answers.Any(x => x.Id == answerIdMNeutral))
                throw new AnswerSubmittedGameException("The provided answerID isn't valid for this question.");

            var playerAttacking = currentRound
                .NeutralRound
                .TerritoryAttackers
                .First(x => x.AttackerId == userId);


            if (playerAttacking.AttackerMChoiceQAnswerId != null)
                throw new AnswerSubmittedGameException("You already voted for this question");

            playerAttacking.AttackerMChoiceQAnswerId = answerIdMNeutral;

            return new AnsweredResponse(currentRound
                .NeutralRound
                .TerritoryAttackers
                    .Select(e => new AnsweredResponse.UserAnswer(e.AttackerId, e.AttackerMChoiceQAnswerId is not null))
                .ToList());

        }

        private static AnsweredResponse AnswerMultiplePvpQuestion(string answerIdString, Round currentRound, int userId)
        {
            bool success = int.TryParse(answerIdString, out int answerIdMPvp);
            if (!success)
                throw new AnswerSubmittedGameException("You didn't provide a valid number");

            // Requesting user is the attacker
            if (!currentRound.Question.Answers.Any(x => x.Id == answerIdMPvp))
                throw new AnswerSubmittedGameException("The provided answerID isn't valid for this question.");

            if (userId != currentRound.PvpRound.AttackerId && userId != currentRound.PvpRound.DefenderId)
                throw new AnswerSubmittedGameException("You can't vote for this question");

            var userAttacking = currentRound
                .PvpRound
                .PvpRoundAnswers
                .FirstOrDefault(x => x.UserId == userId);

            if (userAttacking != null && userAttacking.MChoiceQAnswerId != null)
                throw new ArgumentException("This user already voted for this question");

            var result = new PvpRoundAnswers()
            {
                MChoiceQAnswerId = answerIdMPvp,
                UserId = userId
            };
            currentRound.PvpRound.PvpRoundAnswers.Add(result);

            var response = new AnsweredResponse(new List<AnsweredResponse.UserAnswer>()
            {
                new AnsweredResponse.UserAnswer(currentRound.PvpRound.AttackerId, currentRound.PvpRound.PvpRoundAnswers.Any(e => e.UserId == currentRound.PvpRound.AttackerId)),
                new AnsweredResponse.UserAnswer(currentRound.PvpRound.DefenderId ?? 0, currentRound.PvpRound.PvpRoundAnswers.Any(e => e.UserId == currentRound.PvpRound.DefenderId)),
            });

            return response;
        }

        private static AnsweredResponse AnswerNumberPvpQuestion(string answerIdString, Round currentRound, int userId)
        {
            bool successNPvp = long.TryParse(answerIdString, out long answerIdNPvp);
            if (!successNPvp)
                throw new AnswerSubmittedGameException("You didn't provide a valid number");

            var pvpAttacker = currentRound
                .PvpRound
                .PvpRoundAnswers
                .First(x => x.UserId == userId);

            if (pvpAttacker.NumberQAnswer != null)
                throw new AnswerSubmittedGameException("You already voted for this question");

            pvpAttacker.NumberQAnsweredAt = DateTime.Now;
            pvpAttacker.NumberQAnswer = answerIdNPvp;

            var response = new AnsweredResponse(new List<AnsweredResponse.UserAnswer>()
            {
                new AnsweredResponse.UserAnswer(currentRound.PvpRound.AttackerId, 
                    currentRound.PvpRound.PvpRoundAnswers.Any(e => e.UserId == currentRound.PvpRound.AttackerId && e.NumberQAnswer is not null)),
                new AnsweredResponse.UserAnswer(currentRound.PvpRound.DefenderId ?? 0, 
                    currentRound.PvpRound.PvpRoundAnswers.Any(e => e.UserId == currentRound.PvpRound.DefenderId && e.NumberQAnswer is not null)),
            });

            return response;
        }



        private static AnsweredResponse AnswerNumberNeutralQuestion(string answerIdString, Round currentRound, int userId)
        {

            bool successNNeutral = long.TryParse(answerIdString, out long answerIdNNeutral);
            if (!successNNeutral)
                throw new AnswerSubmittedGameException("You didn't provide a valid number");

            var pAttacker = currentRound
                .NeutralRound
                .TerritoryAttackers
                .First(x => x.AttackerId == userId);

            if (pAttacker.AttackerNumberQAnswer != null)
                throw new AnswerSubmittedGameException("You already voted for this question");

            pAttacker.AnsweredAt = DateTime.Now;
            pAttacker.AttackerNumberQAnswer = answerIdNNeutral;


            return new AnsweredResponse(currentRound
                .NeutralRound
                .TerritoryAttackers
                    .Select(e => new AnsweredResponse.UserAnswer(e.AttackerId, e.AttackerNumberQAnswer is not null))
                .ToList());
        }

        /// <summary>
        /// Happens after the person answered a question
        /// </summary>
        /// <param name="timerWrapper"></param>
        private static void OnAnswerQuestion(TimerWrapper timerWrapper, AnsweredResponse response)
        {
            CloseQuestionVoting(timerWrapper, response);
        }

        /// <summary>
        /// When all players have answered, skip the wait time
        /// </summary>
        private static void CloseQuestionVoting(TimerWrapper timerWrapper, AnsweredResponse response)
        {
            var gm = timerWrapper.Data.GameInstance;

            var allGood = true;

            foreach (var user in response.AllRoundUserAnswers)
            {
                var particip = gm.Participants.FirstOrDefault(e => e.PlayerId == user.UserId);

                // Given participant does not exist
                if (particip is null)
                    return;

                if (particip.Player.IsBot)
                    continue;

                if (user.HasAnswer)
                    continue;

                allGood = false;
            }

            if (!allGood)
                return;

            // All players who aren't a bot answered
            const int ModifyTimeMilis = 1000;


            if (timerWrapper.TimeUntilNextEvent < ModifyTimeMilis)
                return;

            timerWrapper.ChangeReminingInterval(ModifyTimeMilis);
        }



        public void AnswerQuestion(string answerIdString)
        {
            var answeredAt = DateTime.Now;

            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();

            var playerGameTimer = gameTimerService.GameTimers.FirstOrDefault(e =>
                e.Data.GameInstance.Participants.FirstOrDefault(e => e.Player.UserGlobalIdentifier == globalUserId) is not null);

            if (playerGameTimer == null)
                throw new GameException("There is no open game where this player participates");

            var gm = playerGameTimer.Data.GameInstance;

            var currentRound = gm.Rounds
                .Where(x =>
                    x.GameRoundNumber == x.GameInstance.GameRoundNumber)
                .FirstOrDefault();

            var userId = gm.Participants.First(e => e.Player.UserGlobalIdentifier == globalUserId).PlayerId;



            if (currentRound == null)
                throw new AnswerSubmittedGameException("User isn't participating in any in progress games.");


            AnsweredResponse stageAnswerResponse = null;

            // Capital stage
            // Skip every check here and check externally
            if (currentRound.PvpRound?.IsCurrentlyCapitalStage == true)
            {

                // In-game-instance-uncertain
                stageAnswerResponse = CapitalStageAnswer(answerIdString, ref currentRound, answeredAt, userId);


                // On question successfully answered, trigger extra calcs
                OnAnswerQuestion(playerGameTimer, stageAnswerResponse);


                return;
            }

            if (!currentRound.IsQuestionVotingOpen)
                throw new AnswerSubmittedGameException("The voting stage for this question is either over or not started.");

            stageAnswerResponse = currentRound.AttackStage switch
            {
                AttackStage.MULTIPLE_NEUTRAL =>
                    AnswerMultipleNeutralQuestion(answerIdString, currentRound, userId),

                AttackStage.NUMBER_NEUTRAL =>
                    AnswerNumberNeutralQuestion(answerIdString, currentRound, userId),

                AttackStage.MULTIPLE_PVP =>
                    AnswerMultiplePvpQuestion(answerIdString, currentRound, userId),

                AttackStage.NUMBER_PVP =>
                    AnswerNumberPvpQuestion(answerIdString, currentRound, userId),

                AttackStage.FINAL_NUMBER_PVP =>
                    AnswerFinalQuestion(answerIdString, ref currentRound, userId),

                _ => throw new ArgumentException("Unhandled answer round stage"),
            };


            // On question successfully answered, trigger extra calcs
            OnAnswerQuestion(playerGameTimer, stageAnswerResponse);
        }
    }
}

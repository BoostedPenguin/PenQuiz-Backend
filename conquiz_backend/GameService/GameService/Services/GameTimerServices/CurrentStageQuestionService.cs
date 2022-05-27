using AutoMapper;
using GameService.Data.Models;
using GameService.Dtos.SignalR_Responses;
using System;
using System.Linq;

namespace GameService.Services.GameTimerServices
{
    public interface ICurrentStageQuestionService
    {
        Questions GetCurrentStageQuestion(GameInstance gm);
        QuestionClientResponse GetCurrentStageQuestionResponse(GameInstance gm);
    }

    public class CurrentStageQuestionService : ICurrentStageQuestionService
    {
        private readonly IMapper mapper;

        public CurrentStageQuestionService(IMapper mapper)
        {
            this.mapper = mapper;
        }
        public Questions GetCurrentStageQuestion(GameInstance gm)
        {
            var currentRound = gm.Rounds
                .Where(e => e.GameRoundNumber == gm.GameRoundNumber)
                .FirstOrDefault();

            // Handles neutral and final rounds
            switch (currentRound.AttackStage)
            {
                case AttackStage.NUMBER_NEUTRAL:
                    return gm.Rounds
                           .First(e => e.GameRoundNumber == e.GameInstance.GameRoundNumber)
                           .Question;

                case AttackStage.MULTIPLE_NEUTRAL:
                    return gm.Rounds
                                .Where(x => x.GameRoundNumber == gm.GameRoundNumber)
                                .FirstOrDefault()
                                .Question;

                case AttackStage.FINAL_NUMBER_PVP:
                    return gm.Rounds
                        .First(e => e.GameRoundNumber == gm.GameRoundNumber && e.AttackStage == AttackStage.FINAL_NUMBER_PVP)
                        .Question;
            }

            // Capital stage questions
            if (currentRound.PvpRound.IsCurrentlyCapitalStage)
            {
                switch (currentRound.PvpRound.CapitalRounds.First(e => !e.IsCompleted).CapitalRoundAttackStage)
                {
                    case CapitalRoundAttackStage.MULTIPLE_CHOICE_QUESTION:
                        return gm.Rounds
                            .Where(e => e.GameRoundNumber == gm.GameRoundNumber)
                            .First()
                            .PvpRound.CapitalRounds
                            .First(e => !e.IsCompleted && e.CapitalRoundAttackStage == CapitalRoundAttackStage.MULTIPLE_CHOICE_QUESTION)
                            .CapitalRoundMultipleQuestion;

                    case CapitalRoundAttackStage.NUMBER_QUESTION:
                        return gm.Rounds
                            .Where(e => e.GameRoundNumber == gm.GameRoundNumber)
                            .First()
                            .PvpRound
                            .CapitalRounds
                            .First(e => !e.IsCompleted && e.CapitalRoundAttackStage == CapitalRoundAttackStage.NUMBER_QUESTION)
                            .CapitalRoundNumberQuestion;
                }
            }


            switch (currentRound.AttackStage)
            {

                case AttackStage.MULTIPLE_PVP:
                    return gm.Rounds
                        .First(e => e.GameRoundNumber == e.GameInstance.GameRoundNumber)
                        .Question;

                case AttackStage.NUMBER_PVP:
                    return gm.Rounds
                        .Where(e => e.GameRoundNumber == gm.GameRoundNumber)
                        .FirstOrDefault()
                        .PvpRound
                        .NumberQuestion;
            }

            throw new ArgumentException("The current GetCurrentStageRequest was not handled correctly. Contact an administrator!");

        }

        public QuestionClientResponse GetCurrentStageQuestionResponse(GameInstance gm)
        {
            var currentRound = gm.Rounds
                .Where(e => e.GameRoundNumber == gm.GameRoundNumber)
                .FirstOrDefault();

            // Handles neutral and final rounds
            switch (currentRound.AttackStage)
            {
                case AttackStage.NUMBER_NEUTRAL:
                    return GetCurrentNeutralNumberQuestion(gm);

                case AttackStage.MULTIPLE_NEUTRAL:
                    return GetCurrentNeutralMCQuestion(gm);

                case AttackStage.FINAL_NUMBER_PVP:
                    return GetCurrentFinalPvpQuestion(gm);
            }

            // Capital stage questions
            if (currentRound.PvpRound.IsCurrentlyCapitalStage)
            {
                switch (currentRound.PvpRound.CapitalRounds.First(e => !e.IsCompleted).CapitalRoundAttackStage)
                {
                    case CapitalRoundAttackStage.MULTIPLE_CHOICE_QUESTION:
                        return GetCurrentCapitalMCQuestion(gm);

                    case CapitalRoundAttackStage.NUMBER_QUESTION:
                        return GetCurrentCapitalNumberQuestion(gm);
                }
            }


            switch (currentRound.AttackStage)
            {

                case AttackStage.MULTIPLE_PVP:
                    return GetCurrentPvpMCQuestion(gm);

                case AttackStage.NUMBER_PVP:
                    return GetCurrentPvpNumberQuestion(gm);
            }

            throw new ArgumentException("The current GetCurrentStageRequest was not handled correctly. Contact an administrator!");

        }


        public QuestionClientResponse GetCurrentCapitalMCQuestion(GameInstance gm, Questions question = null)
        {
            // Capital MC question
            if (question is null)
            {
                question = gm.Rounds
                    .Where(e => e.GameRoundNumber == gm.GameRoundNumber)
                    .First()
                    .PvpRound.CapitalRounds
                    .First(e => !e.IsCompleted && e.CapitalRoundAttackStage == CapitalRoundAttackStage.MULTIPLE_CHOICE_QUESTION)
                    .CapitalRoundMultipleQuestion;
            }


            if (question == null)
                throw new ArgumentException($"There was no question generated for gameinstanceid: {gm.Id}, gameroundnumber: {gm.GameRoundNumber}.");

            // Map pvp round
            var pvpRound = gm.Rounds.Where(e => e.GameRoundNumber == gm.GameRoundNumber).First().PvpRound;

            // Map participants
            var participantsMapping = mapper.Map<ParticipantsResponse[]>(gm.Participants
                .Where(e => e.PlayerId == pvpRound.AttackerId || e.PlayerId == pvpRound.DefenderId)
                .ToArray());


            var response = mapper.Map<QuestionClientResponse>(question);

            response.IsNeutral = false;

            response.Participants = participantsMapping;

            response.AttackerId = pvpRound.AttackerId;
            response.DefenderId = pvpRound.DefenderId ?? 0;


            // If a user got to this stage, we can gurantee that there is exactly 1 capital round including this, left
            response.CapitalRoundsRemaining = 1;


            return response;
        }

        public QuestionClientResponse GetCurrentCapitalNumberQuestion(GameInstance gm, Questions question = null)
        {
            // Capital MC question
            if (question is null)
            {
                question = gm.Rounds
                    .Where(e => e.GameRoundNumber == gm.GameRoundNumber)
                    .First()
                    .PvpRound
                    .CapitalRounds
                    .First(e => !e.IsCompleted && e.CapitalRoundAttackStage == CapitalRoundAttackStage.NUMBER_QUESTION)
                    .CapitalRoundNumberQuestion;

            }

            if (question == null)
                throw new ArgumentException($"There was no question generated for gameinstanceid: {gm.Id}, gameroundnumber: {gm.GameRoundNumber}.");


            var response = mapper.Map<QuestionClientResponse>(question);

            response.IsNeutral = false;

            var participantsMapping = mapper.Map<ParticipantsResponse[]>(question
                .CapitalRoundNumber
                .PvpRound
                .Round
                .GameInstance
                .Participants
                .Where(x => x.PlayerId == question.CapitalRoundNumber.PvpRound.AttackerId || x.PlayerId == question.CapitalRoundNumber.PvpRound.DefenderId)
                .ToArray());

            response.Participants = participantsMapping;

            response.AttackerId = question.CapitalRoundNumber.PvpRound.AttackerId;
            response.DefenderId = question.CapitalRoundNumber.PvpRound.DefenderId ?? 0;

            // If a user got to this stage, we can gurantee that there is exactly 1 capital round including this, left
            response.CapitalRoundsRemaining = 1;

            return response;
        }

        public QuestionClientResponse GetCurrentFinalPvpQuestion(GameInstance gm, Round currentRound = null)
        {
            if(currentRound is null)
            {
                currentRound = gm.Rounds
                    .First(e => e.GameRoundNumber == gm.GameRoundNumber && e.AttackStage == AttackStage.FINAL_NUMBER_PVP);
            }


            if (currentRound.Question == null)
                throw new ArgumentException($"There was no question generated for gameinstanceid: {gm.Id}, gameroundnumber: {gm.GameRoundNumber}.");



            var response = mapper.Map<QuestionClientResponse>(currentRound.Question);

            // If the round is a neutral one, then everyone can attack
            var terAttackers = currentRound.NeutralRound.TerritoryAttackers.ToList();
            if (terAttackers.Count == 2)
            {
                response.IsLastQuestion = true;
                response.IsNeutral = false;
                response.AttackerId = terAttackers[0].AttackerId;
                response.DefenderId = terAttackers[1].AttackerId;

                var participantsMapping = mapper.Map<ParticipantsResponse[]>(gm.Participants
                    .Where(x => terAttackers.Any(y => y.AttackerId == x.PlayerId))
                    .ToArray());
                response.Participants = participantsMapping;
            }
            else
            {
                response.IsLastQuestion = true;
                response.IsNeutral = true;
                var participantsMapping = mapper.Map<ParticipantsResponse[]>(gm.Participants.ToArray());
                response.Participants = participantsMapping;
            }

            return response;
        }

        public QuestionClientResponse GetCurrentNeutralMCQuestion(GameInstance gm, Round currentRound = null)
        {
            if(currentRound is null)
            {
                currentRound = gm.Rounds
                                .Where(x => x.GameRoundNumber == gm.GameRoundNumber)
                                .FirstOrDefault();
            }

            var question = currentRound.Question;
            // Show the question to the user

            if (question == null)
                throw new ArgumentException($"There was no question generated for gameinstanceid: {gm.Id}, gameroundnumber: {gm.GameRoundNumber}.");


            var response = mapper.Map<QuestionClientResponse>(question);

            var participantsMapping = mapper.Map<ParticipantsResponse[]>(question.Round.GameInstance.Participants.ToArray());

            response.Participants = participantsMapping;
            // If the round is a neutral one, then everyone can attack
            response.IsNeutral = true;

            return response;
        }

        public QuestionClientResponse GetCurrentNeutralNumberQuestion(GameInstance gm, Round currentRound = null)
        {
            if(currentRound is null)
            {
                // Show the question to the user
                currentRound = gm.Rounds
                    .First(e => e.GameRoundNumber == e.GameInstance.GameRoundNumber);
            }


            if (currentRound.Question == null)
                throw new ArgumentException($"There was no question generated for gameinstanceid: {gm.Id}, gameroundnumber: {gm.GameRoundNumber}.");



            var response = mapper.Map<QuestionClientResponse>(currentRound.Question);

            // If the round is a neutral one, then everyone can attack
            response.IsNeutral = true;
            var participantsMapping = mapper.Map<ParticipantsResponse[]>(currentRound.GameInstance.Participants.ToArray());

            response.Participants = participantsMapping;

            return response;
        }

        public QuestionClientResponse GetCurrentPvpMCQuestion(GameInstance gm, Questions question = null)
        {
            if (question is null)
            {
                question = 
                    gm.Rounds.First(e => e.GameRoundNumber == e.GameInstance.GameRoundNumber).Question;
            }

            if (question == null)
                throw new ArgumentException($"There was no question generated for gameinstanceid: {gm.Id}, gameroundnumber: {gm.GameRoundNumber}.");


            var response = mapper.Map<QuestionClientResponse>(question);

            response.IsNeutral = false;

            var participants = gm.Rounds.First(e => e.GameRoundNumber == e.GameInstance.GameRoundNumber).PvpRound;

            var participantsMapping = mapper.Map<ParticipantsResponse[]>(participants.Round.GameInstance.Participants
                        .Where(y => y.PlayerId == participants.AttackerId || y.PlayerId == participants.DefenderId)
                        .ToArray());

            response.Participants = participantsMapping;

            response.AttackerId = participants.AttackerId;
            response.DefenderId = participants.DefenderId ?? 0;


            // If the current attacked territory is capital, then we can presume this and next question are capital questions
            if (participants.AttackedTerritory.IsCapital)
                response.CapitalRoundsRemaining = 2;

            return response;
        }

        public QuestionClientResponse GetCurrentPvpNumberQuestion(GameInstance gm, Questions question = null)
        {
            if (question is null)
            {
                question =
                    gm.Rounds.Where(e => e.GameRoundNumber == gm.GameRoundNumber).FirstOrDefault().PvpRound.NumberQuestion;
            }

            if (question == null)
                throw new ArgumentException($"There was no question generated for gameinstanceid: {gm.Id}, gameroundnumber: {gm.GameRoundNumber}.");


            var response = mapper.Map<QuestionClientResponse>(question);

            response.IsNeutral = false;

            var participantsMapping = mapper.Map<ParticipantsResponse[]>(question
                .PvpRoundNum
                .Round
                .GameInstance
                .Participants
                .Where(x => x.PlayerId == question.PvpRoundNum.AttackerId || x.PlayerId == question.PvpRoundNum.DefenderId)
                .ToArray());

            response.Participants = participantsMapping;

            response.AttackerId = question.PvpRoundNum.AttackerId;
            response.DefenderId = question.PvpRoundNum.DefenderId ?? 0;


            // If the current attacked territory is capital,
            // And we got to number question (attacker and defender had same answer)
            // Then we can presume this and next question are capital questions
            if (question.PvpRoundNum.AttackedTerritory.IsCapital)
                response.CapitalRoundsRemaining = 2;

            return response;
        }
    }
}

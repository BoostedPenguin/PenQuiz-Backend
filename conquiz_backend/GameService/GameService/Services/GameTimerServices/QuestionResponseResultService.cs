using GameService.Data.Models;
using GameService.Dtos.SignalR_Responses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameService.Services.GameTimerServices
{
    public static class QuestionResponseResultService
    {

        public static MCPlayerQuestionAnswers GenerateMCQuestionPreviewResult(GameInstance gm)
        {
            var currentRound = gm.Rounds.Where(x => x.GameRoundNumber == gm.GameRoundNumber).FirstOrDefault();

            switch (currentRound.AttackStage)
            {
                case AttackStage.MULTIPLE_NEUTRAL:
                    return GenerateNeutralMCQuestionPreviewResult(gm);

                case AttackStage.MULTIPLE_PVP:
                    return GeneratePvpMCQuestionPreviewResult(gm);
            }

            if(currentRound.PvpRound.IsCurrentlyCapitalStage)
            {
                return GenerateCapitalMCQuestionPreviewResult(gm);
            }

            return null;
        }

        private static NumberPlayerQuestionAnswers GenerateNeutralNumberQuestionPreviewResult(GameInstance gm)
        {

            var currentRound = gm.Rounds
                .Where(x => x.GameRoundNumber == gm.GameRoundNumber)
                .FirstOrDefault();

            var correctNumberQuestionAnswer = long.Parse(currentRound.Question.Answers.First().Answer);

            var clientResponse = new NumberPlayerQuestionAnswers()
            {
                CorrectAnswer = correctNumberQuestionAnswer.ToString(),
                PlayerAnswers = new List<NumberPlayerIdAnswer>(),
            };
            return null;
        }

        private static MCPlayerQuestionAnswers GenerateCapitalMCQuestionPreviewResult(GameInstance gm)
        {
            var baseRound = gm.Rounds.Where(e => e.GameRoundNumber == gm.GameRoundNumber).First();

            var capitalRound = baseRound.PvpRound.CapitalRounds.FirstOrDefault(x => !x.IsCompleted &&
                x.CapitalRoundAttackStage == CapitalRoundAttackStage.MULTIPLE_CHOICE_QUESTION &&
                x.IsQuestionVotingOpen);

            if (capitalRound == null)
                throw new ArgumentException("The current capital mc question is empty!");

            var attackerAnswer = capitalRound
                .CapitalRoundUserAnswers.FirstOrDefault(x => x.UserId == baseRound.PvpRound.AttackerId);
            var defenderAnswer = capitalRound
                .CapitalRoundUserAnswers.FirstOrDefault(x => x.UserId == baseRound.PvpRound.DefenderId);

            // Client response
            var response = new MCPlayerQuestionAnswers()
            {
                CorrectAnswerId = capitalRound.CapitalRoundMultipleQuestion.Answers.FirstOrDefault(x => x.Correct).Id,
                PlayerAnswers = new List<PlayerIdAnswerId>()
                {
                    new PlayerIdAnswerId()
                    {
                        Id = baseRound.PvpRound.DefenderId ?? 0,
                        AnswerId = defenderAnswer?.MChoiceQAnswerId ?? 0,
                    },
                    new PlayerIdAnswerId()
                    {
                        Id = baseRound.PvpRound.AttackerId,
                        AnswerId = attackerAnswer?.MChoiceQAnswerId ?? 0,
                    }
                }
            };

            return response;
        }

        private static MCPlayerQuestionAnswers GeneratePvpMCQuestionPreviewResult(GameInstance gm)
        {
            var currentRound = gm.Rounds.Where(x => x.GameRoundNumber == gm.GameRoundNumber).FirstOrDefault();


            // If attacker didn't win, we don't care what the outcome is
            var attackerAnswer = currentRound
                .PvpRound
                .PvpRoundAnswers
                .FirstOrDefault(x => x.UserId == currentRound.PvpRound.AttackerId);

            var defenderAnswer = currentRound
                .PvpRound
                .PvpRoundAnswers
                .FirstOrDefault(x => x.UserId == currentRound.PvpRound.DefenderId);

            // Client response
            var response = new MCPlayerQuestionAnswers()
            {
                CorrectAnswerId = currentRound.Question.Answers.FirstOrDefault(x => x.Correct).Id,
                PlayerAnswers = new List<PlayerIdAnswerId>()
                {
                    new PlayerIdAnswerId()
                    {
                        Id = currentRound.PvpRound.DefenderId ?? 0,
                        AnswerId = defenderAnswer?.MChoiceQAnswerId ?? 0,
                    },
                    new PlayerIdAnswerId()
                    {
                        Id = currentRound.PvpRound.AttackerId,
                        AnswerId = attackerAnswer?.MChoiceQAnswerId ?? 0,
                    }
                }
            };

            return response;
        }

        private static MCPlayerQuestionAnswers GenerateNeutralMCQuestionPreviewResult(GameInstance gm)
        {
            var currentRound = gm.Rounds
                .Where(x => x.GameRoundNumber == gm.GameRoundNumber)
                .FirstOrDefault();

            var playerIdAnswerId = new Dictionary<int, int>();


            foreach (var p in currentRound.NeutralRound.TerritoryAttackers)
            {
                playerIdAnswerId.Add(
                    p.AttackerId,
                    p.AttackerMChoiceQAnswerId ?? 0
                );
            }


            var response = new MCPlayerQuestionAnswers()
            {
                CorrectAnswerId = currentRound.Question.Answers.FirstOrDefault(x => x.Correct).Id,
                PlayerAnswers = new List<PlayerIdAnswerId>(),
            };



            foreach (var pId in playerIdAnswerId)
            {
                response.PlayerAnswers.Add(new PlayerIdAnswerId()
                {
                    Id = pId.Key,
                    AnswerId = pId.Value,
                });
            }

            return response;
        }
    }
}

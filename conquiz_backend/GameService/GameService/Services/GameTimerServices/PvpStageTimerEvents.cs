using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.GameTimerServices
{
    public interface IPvpStageTimerEvents
    {

    }

    public class PvpStageTimerEvents : IPvpStageTimerEvents
    {

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="timerWrapper"></param>
        /// <param name="isNeutral"></param>
        /// <returns></returns>
        //public async Task Show_MultipleChoice_Screen(TimerWrapper timerWrapper, bool isNeutral)
        //{
        //    // Stop timer until we calculate the next action and client event
        //    timerWrapper.Stop();

        //    // Get the question and show it to the clients
        //    var data = timerWrapper.Data;
        //    var db = contextFactory.CreateDbContext();

        //    // Show the question to the user
        //    var question = await db.Questions
        //        .Include(x => x.Answers)
        //        .Include(x => x.Round)
        //        .ThenInclude(x => x.GameInstance)
        //        .ThenInclude(x => x.Participants)
        //        .Where(x => x.Round.GameInstanceId == data.GameInstanceId &&
        //            x.Round.GameRoundNumber == x.Round.GameInstance.GameRoundNumber)
        //        .FirstOrDefaultAsync();


        //    // Open this question for voting
        //    question.Round.IsQuestionVotingOpen = true;
        //    db.Update(question.Round);
        //    await db.SaveChangesAsync();

        //    //await SendQuestionHub(data.GameLink, question);

        //    var response = mapper.Map<QuestionClientResponse>(question);

        //    // If the round is a neutral one, then everyone can attack
        //    if (isNeutral)
        //    {
        //        response.IsNeutral = true;
        //        response.Participants = question.Round.GameInstance.Participants.ToArray();

        //        await hubContext.Clients.Group(data.GameLink).GetRoundQuestion(response,
        //            GameActionsTime.GetServerActionsTime(ActionState.SHOW_MULTIPLE_CHOICE_QUESTION));
        //    }
        //    else
        //    {
        //        response.IsNeutral = false;

        //        var participants = await db.PvpRounds
        //            .Include(x => x.Round)
        //            .ThenInclude(x => x.GameInstance)
        //            .ThenInclude(x => x.Participants)
        //            .Where(x => x.Round.GameRoundNumber == data.CurrentGameRoundNumber &&
        //                x.Round.GameInstanceId == data.GameInstanceId)
        //            .Select(x => new
        //            {
        //                Participants = x.Round.GameInstance.Participants
        //                    .Where(y => y.PlayerId == x.AttackerId || y.PlayerId == x.DefenderId)
        //                    .ToArray(),
        //                x.AttackerId,
        //                x.DefenderId,
        //            })
        //            .FirstOrDefaultAsync();

        //        response.Participants = participants.Participants;
        //        response.AttackerId = participants.AttackerId;
        //        response.DefenderId = participants.DefenderId ?? 0;


        //        await hubContext.Clients.Group(data.GameLink).GetRoundQuestion(response,
        //            GameActionsTime.GetServerActionsTime(ActionState.SHOW_MULTIPLE_CHOICE_QUESTION));
        //    }

        //    timerWrapper.Data.NextAction = ActionState.END_MULTIPLE_CHOICE_QUESTION;
        //    timerWrapper.Interval = GameActionsTime.GetServerActionsTime(ActionState.SHOW_MULTIPLE_CHOICE_QUESTION);
        //    timerWrapper.Start();
        //}

        ///TODO
        //private async Task Close_MultipleChoice_Pvp_Voting(TimerWrapper timerWrapper)
        //{
        //    // Can disable voting on start, however even 0-1s delay wouldn't be game breaking and would ease performance
        //    timerWrapper.Stop();
        //    var data = timerWrapper.Data;
        //    var db = contextFactory.CreateDbContext();

        //    var currentRound =
        //        await db.Round
        //        .Include(x => x.Question)
        //        .ThenInclude(x => x.Answers)
        //        .Include(x => x.PvpRound)
        //        .ThenInclude(x => x.PvpRoundAnswers)
        //        .Where(x => x.GameRoundNumber == data.CurrentGameRoundNumber
        //            && x.GameInstanceId == data.GameInstanceId)
        //        .FirstOrDefaultAsync();

        //    currentRound.IsQuestionVotingOpen = false;

        //    var playerCorrect = new Dictionary<int, bool>();

        //    // If attacker didn't win, we don't care what the outcome is
        //    var attackerAnswer = currentRound
        //        .PvpRound
        //        .PvpRoundAnswers
        //        .First(x => x.UserId == currentRound.PvpRound.AttackerId);


        //    // Attacker didn't answer, automatically loses
        //    if (attackerAnswer.MChoiceQAnswerId == null)
        //    {
        //        // Player answered incorrecly, release isattacked lock on objterritory
        //        currentRound.PvpRound.WinnerId = currentRound.PvpRound.DefenderId;
        //        currentRound.PvpRound.AttackedTerritory.AttackedBy = null;
        //    }
        //    else
        //    {

        //        var didAttackerAnswerCorrectly = currentRound
        //            .Question
        //            .Answers
        //            .First(x => x.Id == attackerAnswer.MChoiceQAnswerId)
        //            .Correct;

        //        if (!didAttackerAnswerCorrectly)
        //        {
        //            // Player answered incorrecly, release isattacked lock on objterritory
        //            currentRound.PvpRound.WinnerId = currentRound.PvpRound.DefenderId;
        //            currentRound.PvpRound.AttackedTerritory.AttackedBy = null;
        //        }
        //        else
        //        {
        //            var defenderAnswer = currentRound
        //                .PvpRound
        //                .PvpRoundAnswers
        //                .First(x => x.UserId == currentRound.PvpRound.DefenderId);

        //            // Defender didn't vote, he lost
        //            if (defenderAnswer.MChoiceQAnswerId == null)
        //            {
        //                // Player answered incorrecly, release isattacked lock on objterritory
        //                currentRound.PvpRound.WinnerId = currentRound.PvpRound.AttackerId;
        //                currentRound.PvpRound.AttackedTerritory.AttackedBy = null;
        //                currentRound.PvpRound.AttackedTerritory.TakenBy = currentRound.PvpRound.AttackerId;
        //            }
        //            else
        //            {
        //                // A new number question has to be shown
        //                throw new NotImplementedException("A new number question has to be shown");
        //            }
        //        }
        //    }

        //    //db.Update(currentRound);
        //    //await db.SaveChangesAsync();

        //    //var qResult = new QuestionResultResponse()
        //    //{
        //    //    Id = currentRound.Question.Id,
        //    //    Answers = mapper.Map<List<AnswerResultResponse>>(currentRound.Question.Answers),
        //    //    Question = currentRound.Question.Question,
        //    //    Type = currentRound.Question.Type,
        //    //    WinnerId = (int)currentRound.PvpRound.WinnerId,
        //    //};

        //    //await hubContext.Clients.Group(data.GameLink).PreviewResult(qResult);

        //    //timerWrapper.Interval = 3000;
        //    //timerWrapper.Start();
        //}
    }
}

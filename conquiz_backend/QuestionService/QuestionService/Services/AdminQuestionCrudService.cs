using Microsoft.EntityFrameworkCore;
using QuestionService.Data;
using QuestionService.Data.Models;
using QuestionService.Data.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.Services
{
    public interface IAdminQuestionCrudService
    {
        Task<PaginatedQuestionsResponse> GetUnverifiedQuestions(int pageNumber, int pageEntries);
        Task RejectQuestion(VerifyQuestionRequest request);
        Task VerifyChangedQuestion(VerifyChangedQuestionRequest request);
        Task VerifyQuestion(VerifyQuestionRequest request);
    }

    public class AdminQuestionCrudService : IAdminQuestionCrudService
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;

        public AdminQuestionCrudService(IDbContextFactory<DefaultContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public async Task<PaginatedQuestionsResponse> GetUnverifiedQuestions(int pageNumber, int pageEntries)
        {
            using var db = contextFactory.CreateDbContext();
            var baseQuery = db.Questions
                .Where(x => x.VerificationStatus == VerificationStatus.UNVERIFIED)
                .OrderBy(x => x.SubmittedAt)
                .AsNoTracking();

            var questions = await baseQuery
                .Include(x => x.Answers)
                .Skip((pageNumber - 1) * pageEntries)
                .Take(pageEntries)
                .ToListAsync();

            var count = baseQuery.Count();

            return new PaginatedQuestionsResponse()
            {
                Questions = questions,
                PageIndex = pageNumber,
                TotalPages = (int)Math.Ceiling(count / (double)pageEntries),
            };
        }

        public async Task RejectQuestion(VerifyQuestionRequest request)
        {
            using var db = contextFactory.CreateDbContext();
            var question = await db.Questions.FirstOrDefaultAsync(x => x.Id == request.QuestionId);

            if(question == null)
                throw new ArgumentException($"Question with ID: {request.QuestionId} not found.");

            if (question.VerificationStatus == VerificationStatus.REJECTED)
                throw new ArgumentException("Question was already rejected by an administrator.");

            question.VerificationStatus = VerificationStatus.REJECTED;

            db.Update(question);
            await db.SaveChangesAsync();
        }

        public async Task VerifyChangedQuestion(VerifyChangedQuestionRequest request)
        {
            using var db = contextFactory.CreateDbContext();

            var question = await db.Questions.Include(x => x.Answers).FirstOrDefaultAsync(x => x.Id == request.QuestionId);
            if (question == null)
                throw new ArgumentException($"Question with ID: {request.QuestionId} not found.");

            switch (question.VerificationStatus)
            {
                case VerificationStatus.VERIFIED:
                    throw new ArgumentException($"Question with ID: {request.QuestionId} was already verified.");
                case VerificationStatus.REJECTED:
                    throw new ArgumentException($"Question with ID: {request.QuestionId} was already rejected by administrator.");
            }

            // Uppercase first char
            request.Question = Extensions.FirstCharToUpper(request.Question);

            // Add question mark in the end if missing
            if (!request.Question.EndsWith("?"))
                request.Question = request.Question += "?";

            // Correct answer
            question.Question = request.Question;
            question.Answers.First(x => x.Correct).Answer = Extensions.FirstCharToUpper(request.Answer);

            if(question.Type == "multiple")
            {
                if (request.WrongAnswers == null || request.WrongAnswers.Count != 3)
                    throw new ArgumentException("You need to provide exactly 3 wrong answers to modify a multiple choice question.");

                if (question.Answers.Count != 4)
                    throw new ArgumentException($"The question was marked as multiple, but didn't have 4 correct answers. ID: {question.Id}");

                var count = 0;
                foreach(var answer in question.Answers.Where(x => !x.Correct))
                {
                    answer.Answer = Extensions.FirstCharToUpper(request.WrongAnswers[count++]);
                }
            }

            question.VerifiedAt = DateTime.Now;
            question.VerificationStatus = VerificationStatus.VERIFIED;

            db.Update(question);
            await db.SaveChangesAsync();
        }

        public async Task VerifyQuestion(VerifyQuestionRequest request)
        {
            using var db = contextFactory.CreateDbContext();

            var question = await db.Questions.FirstOrDefaultAsync(x => x.Id == request.QuestionId);

            if (question == null)
                throw new ArgumentException($"Question with ID: {request.QuestionId} not found.");

            switch (question.VerificationStatus)
            {
                case VerificationStatus.VERIFIED:
                    throw new ArgumentException($"Question with ID: {request.QuestionId} was already verified.");
                case VerificationStatus.REJECTED:
                    throw new ArgumentException($"Question with ID: {request.QuestionId} was already rejected by administrator.");
            }

            question.VerifiedAt = DateTime.Now;
            question.VerificationStatus = VerificationStatus.VERIFIED;

            db.Update(question);
            await db.SaveChangesAsync();
        }
    }
}

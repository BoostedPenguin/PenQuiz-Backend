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

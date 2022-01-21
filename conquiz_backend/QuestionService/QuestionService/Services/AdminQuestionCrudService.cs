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
        Task<PaginatedQuestionsResponse> GetUnverifiedQuestions(int pageNumber);
        Task VerifyQuestion(VerifyQuestionRequest request);
    }

    public class AdminQuestionCrudService : IAdminQuestionCrudService
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;

        public AdminQuestionCrudService(IDbContextFactory<DefaultContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public async Task<PaginatedQuestionsResponse> GetUnverifiedQuestions(int pageNumber)
        {
            var pageSize = 10;


            using var db = contextFactory.CreateDbContext();
            var baseQuery = db.Questions
                .Where(x => !x.IsVerified)
                .OrderBy(x => x.SubmittedAt)
                .AsNoTracking();

            var questions = await baseQuery
                .Include(x => x.Answers)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var count = baseQuery.Count();

            return new PaginatedQuestionsResponse()
            {
                Questions = questions,
                PageIndex = pageNumber,
                TotalPages = (int)Math.Ceiling(count / (double)pageSize),
            };
        }

        public async Task VerifyQuestion(VerifyQuestionRequest request)
        {
            using var db = contextFactory.CreateDbContext();

            var question = await db.Questions.FirstOrDefaultAsync(x => x.Id == request.QuestionId);

            if (question == null)
                throw new ArgumentException($"Question with ID: {request.QuestionId} not found.");

            if (question.IsVerified == true)
                throw new ArgumentException($"Question with ID: {request.QuestionId} was already verified.");

            question.VerifiedAt = DateTime.Now;
            question.IsVerified = true;

            db.Update(question);
            await db.SaveChangesAsync();
        }
    }
}

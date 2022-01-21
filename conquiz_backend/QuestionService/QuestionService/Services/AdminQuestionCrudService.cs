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
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            this.AddRange(items);
        }

        public bool HasPreviousPage
        {
            get
            {
                return (PageIndex > 1);
            }
        }

        public bool HasNextPage
        {
            get
            {
                return (PageIndex < TotalPages);
            }
        }

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }

    public interface IAdminQuestionCrudService
    {
        Task VerifyQuestion(VerifyQuestionRequest request);
    }

    public class AdminQuestionCrudService : IAdminQuestionCrudService
    {
        private readonly IDbContextFactory<DefaultContext> contextFactory;

        public AdminQuestionCrudService(IDbContextFactory<DefaultContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public async Task GetUnverifiedQuestions(int pageNumber)
        {
            using var db = contextFactory.CreateDbContext();
            //var questions = db.Questions.OrderBy(x => x.)

            //await PaginatedList<Questions>.CreateAsync(qu)
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

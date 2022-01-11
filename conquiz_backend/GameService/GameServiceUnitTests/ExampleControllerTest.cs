using GameService.Context;
using GameService.Data;
using GameService.Data.Models;
using GameService.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace GameServiceUnitTests
{
    public class ExampleControllerTest : IDisposable
    {
        DefaultContext context;
        Mock<IDbContextFactory<DefaultContext>> mockContextFactory;
        public ExampleControllerTest()
        {
            mockContextFactory = new Mock<IDbContextFactory<DefaultContext>>();

            context = CreateDbContext();
            mockContextFactory.Setup(x => x.CreateDbContext()).Returns(context);
        }


        private DefaultContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            var dbContext = new DefaultContext(options);

            dbContext.Add(new Questions() { Question = "Wtf" });
            dbContext.SaveChanges();
            return dbContext;
        }



        [Fact]
        public void ExampleTest()
        {

            var service = new ExampleService(mockContextFactory.Object);

            var result = service.DoSomething();

            Assert.True(result);
        }

        public void Dispose()
        {
            context.Dispose();
        }
    }
}

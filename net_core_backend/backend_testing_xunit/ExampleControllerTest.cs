using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using net_core_backend.Context;
using net_core_backend.Controllers;
using net_core_backend.Models;
using net_core_backend.Services;
using net_core_backend.Services.Interfaces;
using net_core_backend.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace backend_testing_xunit
{
    public class ExampleControllerTest : IDisposable
    {
        DefaultContext context;
        public ExampleControllerTest()
        {
            context = CreateDbContext();
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
        public async Task ExampleTest()
        {
            var b = new Mock<IExampleService>();
            b.Setup(x => x.DoSomething()).ReturnsAsync(true);

            var g = context.Questions.ToList();

            Assert.Equal(true, true);
        }

        public void Dispose()
        {
            context.Dispose();
        }
    }
}

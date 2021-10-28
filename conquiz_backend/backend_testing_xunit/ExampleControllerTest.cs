﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using GameService.Context;
using GameService.Controllers;
using GameService.Models;
using GameService.Services;
using GameService.Services.Interfaces;
using GameService.ViewModel;
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
        Mock<IContextFactory> mockContextFactory;
        public ExampleControllerTest()
        {
            mockContextFactory = new Mock<IContextFactory>();

            context = CreateDbContext();
            mockContextFactory.Setup(x => x.CreateDbContext(null)).Returns(context);
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

            var service = new ExampleService(mockContextFactory.Object);

            var result = await service.DoSomething();

            Assert.True(result);
        }

        public void Dispose()
        {
            context.Dispose();
        }
    }
}
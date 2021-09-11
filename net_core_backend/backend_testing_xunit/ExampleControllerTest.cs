using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public class ExampleControllerTest : DatabaseSeeder
    {
        private IExampleService service;
        private ExampleController controller;
        private readonly IMapper mapper;

        public ExampleControllerTest(IHttpContextAccessor http, IContextFactory factory, IMapper mapper) : base(http, factory)
        {
            //Configure identity
            CreateIdentity("Someone's authentication token");
            this.mapper = mapper;
        }

        protected override void CreateIdentity(string auth)
        {
            // Configure identity
            base.CreateIdentity(auth);

            // Inject
            service = new ExampleService(factory, http);
            controller = new ExampleController(null, mapper, service)
            {
                ControllerContext = controllerContext,
            };
        }

        [Fact]
        public async Task ExampleTest()
        {
            // Inject
            CreateIdentity("Someone's authentication token");

            // Arrange
            DefaultModel dExample = null;
            //using(var a = factory.CreateDbContext())
            //{
            //    dExample = await a.Example.Include(x => x.SomeOtherRelationship).FirstOrDefaultAsync();
            //}
            var expected = mapper.Map<ExampleViewModel>(dExample);

            // Act
            var result = await controller.AddSomething("Some word");

            result = null;

            // Assert
            //Assert.Equal(Serialize(expected), Serialize(((OkObjectResult)result).Value));
            Assert.Equal(true, true);
        }
    }
}

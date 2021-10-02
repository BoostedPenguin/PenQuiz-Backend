using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Moq;
using net_core_backend.Context;
using net_core_backend.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace backend_testing_xunit
{
    public class TestingContextFactory : IDesignTimeDbContextFactory<DefaultContext>, IContextFactory
    {
        public DefaultContext CreateDbContext(string[] args = null)
        {
            var options = new DbContextOptionsBuilder<DefaultContext>();
            options.UseInMemoryDatabase("TestingDb");

            return new DefaultContext(options.Options);
        }
    }
    public class GameServiceTest
    {
        TestingContextFactory mockContextFactory;
        Mock<IHttpContextAccessor> mockHttpContextAccessor;
        GameService gameService;
        public GameServiceTest()
        {
            var context = new ContextFactory();

            mockContextFactory = new TestingContextFactory();

            //Mock IHttpContextAccessor
            mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Host"] = "localhost:5000";

            var identity = new GenericIdentity("name", "test");
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "123"));
            var contextUser = new ClaimsPrincipal(identity);

            httpContext.User = contextUser;

            mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(httpContext);
            var b = new MapGeneratorService(mockContextFactory, mockHttpContextAccessor.Object);


            gameService = new GameService(mockContextFactory, mockHttpContextAccessor.Object, b);
        }


        [Fact]
        public async Task TestInvitationLinkFormat()
        {
            var result = gameService.CreateInvitiationUrl();

            var wellFormatted = Uri.IsWellFormedUriString(result, UriKind.Absolute);

            Assert.True(wellFormatted);
        }

        [Fact]
        public async Task TestCreateGameLobby()
        {
            var result = await gameService.CreateGameLobby();

            Assert.NotNull(result);
        }
    }
}

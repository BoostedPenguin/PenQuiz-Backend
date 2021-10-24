using Microsoft.AspNetCore.Http;
using Moq;
using Moq.Protected;
using net_core_backend.Context;
using net_core_backend.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace backend_testing_xunit
{
    public class OpenDbServiceTest
    {
        TestingContextFactory mockContextFactory;
        Mock<IHttpContextAccessor> mockHttpContextAccessor;
        OpenDBService openDbService;

        public OpenDbServiceTest()
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
            var mockFactory = new Mock<IHttpClientFactory>();


            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(@"{""response_code"":0,""results"":[{""category"":""Entertainment: Television"",""type"":""multiple"",""difficulty"":""hard"",""question"":""What was the callsign of Commander William Adama in Battlestar Galactica(2004) ? "",""correct_answer"":""Husker"",""incorrect_answers"":[""Starbuck"",""Apollo"",""Crashdown""]}]}"),
                });
            var client = new HttpClient(mockHttpMessageHandler.Object);
            mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            openDbService = new OpenDBService(mockContextFactory, mockHttpContextAccessor.Object, mockFactory.Object);
        }
    }
}

using AccountService;
using AutoMapper;
using GameService.Data.Models;
using GameService.Helpers;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Grpc
{
    public interface IAccountDataClient
    {
        IEnumerable<Users> ReturnAllAccounts();
    }
    public class AccountDataClient : IAccountDataClient
    {
        private readonly IOptions<AppSettings> appSettings;
        private readonly IMapper mapper;
        private readonly ILogger<AccountDataClient> logger;

        public AccountDataClient(IOptions<AppSettings> appSettings, IMapper mapper, ILogger<AccountDataClient> logger)
        {
            this.appSettings = appSettings;
            this.mapper = mapper;
            this.logger = logger;
        }

        public IEnumerable<Users> ReturnAllAccounts()
        {
            logger.LogInformation($"Calling GRPC {appSettings.Value.GrpcAccount}");

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var channel = GrpcChannel.ForAddress(appSettings.Value.GrpcAccount);
            var client = new GrpcAccount.GrpcAccountClient(channel);
            var request = new GetAllRequest();

            try
            {
                var response = client.GetAllAccounts(request);
                return mapper.Map<IEnumerable<Users>>(response.Accounts);
            }
            catch(Exception ex)
            {
                logger.LogError($"Couldn't call GRPC Server {ex.Message}");
                return null;
            }
        }
    }
}

using AccountService;
using AutoMapper;
using GameService.Helpers;
using GameService.Models;
using Grpc.Net.Client;
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

        public AccountDataClient(IOptions<AppSettings> appSettings, IMapper mapper)
        {
            this.appSettings = appSettings;
            this.mapper = mapper;
        }

        public IEnumerable<Users> ReturnAllAccounts()
        {
            Console.WriteLine($"--> Calling GRPC {appSettings.Value.GrpcAccount}");

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
                Console.WriteLine($"--> Couldn't call GRPC Server {ex.Message}");
                return null;
            }
        }
    }
}

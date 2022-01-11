using AccountService.Context;
using AccountService.Data;
using AutoMapper;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AccountService.GrpcAccount;

namespace AccountService.Grpc
{
    public class GrpcAccountService : GrpcAccountBase
    {
        private readonly IDbContextFactory<AppDbContext> contextFactory;
        private readonly IMapper mapper;

        public GrpcAccountService(IDbContextFactory<AppDbContext> contextFactory, IMapper mapper)
        {
            this.contextFactory = contextFactory;
            this.mapper = mapper;
        }

        public override Task<AccountResponse> GetAllAccounts(GetAllRequest request, ServerCallContext context)
        {
            using var db = contextFactory.CreateDbContext();

            var users = db.Users.ToList();
            var response = new AccountResponse();

            foreach(var user in users)
            {
                response.Accounts.Add(mapper.Map<GrpcAccountModel>(user));
            }

            return Task.FromResult(response);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using net_core_backend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace net_core_backend.Context
{
    public class ContextFactory : IDesignTimeDbContextFactory<DefaultContext>, IContextFactory
    {
        private readonly string connectionString;
        public ContextFactory(string connectionString)
        {
            this.connectionString = connectionString;


            var options = new DbContextOptionsBuilder<DefaultContext>();
            options.UseSqlServer(connectionString);

            var context = new DefaultContext(options.Options);
            
            context.Database.EnsureCreated();
        }

        public DefaultContext CreateDbContext(string[] args = null)
        {
            var options = new DbContextOptionsBuilder<DefaultContext>();
            options.UseSqlServer(connectionString);

            return new DefaultContext(options.Options);
        }
    }
}

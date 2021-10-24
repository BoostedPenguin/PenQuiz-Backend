using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using conquiz_backend.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace conquiz_backend.Context
{
    public class ContextFactory : IDesignTimeDbContextFactory<DefaultContext>, IContextFactory
    {
        private readonly string connectionString;
        public ContextFactory(string connectionString)
        {
            this.connectionString = connectionString;


            var options = new DbContextOptionsBuilder<DefaultContext>();
            options.UseSqlServer(connectionString);
        }

        public ContextFactory()
        {
            string path = Directory.GetCurrentDirectory();

            IConfigurationBuilder builder =
                new ConfigurationBuilder()
                    .SetBasePath(path)
                    .AddJsonFile("appsettings.json");

            IConfigurationRoot config = builder.Build();

            connectionString = config.GetConnectionString("SQLCONNSTR_Database");
        }

        public DefaultContext CreateDbContext(string[] args = null)
        {
            var options = new DbContextOptionsBuilder<DefaultContext>();
            options.UseSqlServer(connectionString);

            return new DefaultContext(options.Options);
        }
    }
}

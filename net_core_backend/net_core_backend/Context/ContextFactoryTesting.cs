using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using net_core_backend.Models;


namespace net_core_backend.Context
{
    public class ContextFactoryTesting : IDesignTimeDbContextFactory<DefaultContext>, IContextFactory
    {
        private readonly string connectionString;

        public ContextFactoryTesting(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public DefaultContext CreateDbContext(string[] args = null)
        {
            var options = new DbContextOptionsBuilder<DefaultContext>();
            options.UseInMemoryDatabase("TestingDatabase");

            return new DefaultContext(options.Options);
        }
    }
}

using GameService.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServiceUnitTests
{
    public class TestDbContextFactory : IDbContextFactory<DefaultContext>
    {
        private DbContextOptions<DefaultContext> _options;

        public TestDbContextFactory(string databaseName = "InMemoryTest")
        {
            _options = new DbContextOptionsBuilder<DefaultContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
        }

        public DefaultContext CreateDbContext()
        {
            return new DefaultContext(_options);
        }
    }
}

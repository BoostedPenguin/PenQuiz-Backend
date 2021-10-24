using conquiz_backend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace conquiz_backend.Context
{
    public interface IContextFactory
    {
        public DefaultContext CreateDbContext(string[] args = null);
    }
}

using GameService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Context
{
    public interface IContextFactory
    {
        public DefaultContext CreateDbContext(string[] args = null);
    }
}

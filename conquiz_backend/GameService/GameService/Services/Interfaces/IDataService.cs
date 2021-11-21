using GameService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Services.Interfaces
{
    public interface IDataService<T>
    {
        Task<T> Create(T entity);
        Task<T> Delete(int id);
        Task<T> Get(int id);
        Task<IEnumerable<T>> GetAll();
        Task<T> Update(int id, T entity);
    }
}

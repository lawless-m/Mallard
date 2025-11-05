using System.Threading.Tasks;
using Mallard.Models;

namespace Mallard
{
    public interface IQueryExecutor
    {
        Task<QueryResult> ExecuteAsync(string sql);
        Task<bool> TestConnectionAsync();
        string GetExecutorType();
    }
}

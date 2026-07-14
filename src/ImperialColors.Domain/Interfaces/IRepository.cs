using ImperialColors.Domain.Entities;
using System.Linq.Expressions;

namespace ImperialColors.Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> ObterPorIdAsync(int id);
    Task<IEnumerable<T>> ObterTodosAsync();
    Task<IEnumerable<T>> BuscarAsync(Expression<Func<T, bool>> predicate);
    Task<T> AdicionarAsync(T entity);
    Task<T> AtualizarAsync(T entity);
    Task RemoverAsync(int id);
    Task<bool> ExisteAsync(int id);
    Task<int> ContarAsync(Expression<Func<T, bool>>? predicate = null);
}

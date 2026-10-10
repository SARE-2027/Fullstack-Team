namespace SARE.Application.Common.Interfaces;

public interface IUnitOfWork
{
    Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default);
    Task ExecuteTransactionAsync(Func<Task> action, CancellationToken ct = default);
}

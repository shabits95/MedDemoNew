using MedDemo.Infrastructure.Interface;

namespace MedDemo.Application;

public interface IUnitOfWork
{
    IMedicineRepository MedicineRepository { get; }
    Task SaveChangesAsync(CancellationToken token);
    Task ExecuteTransactionAsync(Action action, CancellationToken token);
    Task ExecuteTransactionAsync(Func<Task> action, CancellationToken token);
}

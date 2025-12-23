using MedDemo.Application.Interface;

namespace MedDemo.Application.Interfaces.Common
{

    public interface IUnitOfWork
    {
        IMedicineRepository MedicineRepository { get; }
        Task SaveChangesAsync(CancellationToken token);
        Task ExecuteTransactionAsync(Action action, CancellationToken token);
        Task ExecuteTransactionAsync(Func<Task> action, CancellationToken token);
    }
}

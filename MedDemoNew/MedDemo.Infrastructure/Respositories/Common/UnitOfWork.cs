using MedDemo.Application;
using MedDemo.Application.Exceptions;
using MedDemo.Infrastructure.Data;
using MedDemo.Infrastructure.Interface;

namespace MedDemo.Infrastructure.Repositories.Common
{

    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public IMedicineRepository MedicineRepository { get; }

        public UnitOfWork(ApplicationDbContext dbContext)
        {
            _context = dbContext;
            MedicineRepository = new MedicineRepository(_context);
        }
        public async Task SaveChangesAsync(CancellationToken token)
            => await _context.SaveChangesAsync(token);

        public async Task ExecuteTransactionAsync(Action action, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                action();
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                throw TransactionException.TransactionNotExecuteException(ex);
            }
        }

        public async Task ExecuteTransactionAsync(Func<Task> action, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                await action();
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                throw TransactionException.TransactionNotExecuteException(ex);
            }
        }
    }
}

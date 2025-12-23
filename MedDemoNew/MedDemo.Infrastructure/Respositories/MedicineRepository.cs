using MedDemo.Domain.Entities;
using MedDemo.Infrastructure.Data;
using MedDemo.Application.Interface;
using MedDemo.Infrastructure.Repositories.Common;

namespace MedDemo.Infrastructure.Repositories
{

    public sealed class MedicineRepository(ApplicationDbContext context) : GenericRepository<Medicine>(context), IMedicineRepository
    {
    }
}

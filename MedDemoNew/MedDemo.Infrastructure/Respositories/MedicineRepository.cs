using MedDemo.Domain.Entities;
using MedDemo.Infrastructure.Data;
using MedDemo.Infrastructure.Interface;
using MedDemo.Infrastructure.Repositories.Common;

namespace MedDemo.Infrastructure.Repositories
{

    public class MedicineRepository(ApplicationDbContext context) : GenericRepository<Medicine>(context), IMedicineRepository { }
}

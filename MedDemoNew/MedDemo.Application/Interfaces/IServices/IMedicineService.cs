using MedDemo.Application.DTO;
using MedDemo.Application.Utilities;

namespace MedDemo.Application.Interfaces.IServices
{
    public interface IMedicineService
    {
        Task<Pagination<MedicineDTO>> Get(int pageIndex, int pageSize);
        Task<MedicineDTO> Get(Guid id);
        Task<MedicineDTO> Add(AddMedicineRequest request, CancellationToken token);
        Task<MedicineDTO> Update(UpdateMedicineRequest request, CancellationToken token);
        Task<MedicineDTO> Delete(Guid id, CancellationToken token);
    }
}

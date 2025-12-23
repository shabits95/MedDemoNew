using AutoMapper;
using MedDemo.Application.DTO;
using MedDemo.Application.Exceptions;
using MedDemo.Application.Interfaces.Common;
using MedDemo.Application.Interfaces.IServices;
using MedDemo.Application.Utilities;
using MedDemo.Domain.Entities;

namespace MedDemo.Application.Services
{
    public class MedicineService : IMedicineService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public MedicineService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Pagination<MedicineDTO>> Get(int pageIndex, int pageSize)
        {
            var medicines = await _unitOfWork.MedicineRepository.ToPagination(
                pageIndex: pageIndex,
                pageSize: pageSize,
                orderBy: x => x.Name,
                ascending: true,
                selector: x => new MedicineDTO
                {
                    Name = x.Name,
                    Price = x.Price,
                    Description = x.Description
                }
            );

            return medicines;
        }

        public async Task<MedicineDTO> Get(Guid id)
        {
            var book = await _unitOfWork.MedicineRepository.FirstOrDefaultAsync(x => x.Id == id);
            return _mapper.Map<MedicineDTO>(book);
        }

        public async Task<MedicineDTO> Add(AddMedicineRequest request, CancellationToken token)
        {
            var book = _mapper.Map<Medicine>(request);
            await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.MedicineRepository.AddAsync(book), token);
            return _mapper.Map<MedicineDTO>(book);
        }

        public async Task<MedicineDTO> Update(UpdateMedicineRequest request, CancellationToken token)
        {
            if (await _unitOfWork.MedicineRepository.AnyAsync(x => x.Id != request.Id))
                throw new UserFriendlyException("Medicine not found", "Medicine not found");

            var book = _mapper.Map<Medicine>(request);
            await _unitOfWork.ExecuteTransactionAsync(() => _unitOfWork.MedicineRepository.Update(book), token);
            return _mapper.Map<MedicineDTO>(book);
        }

        public async Task<MedicineDTO> Delete(Guid id, CancellationToken token)
        {

            var existBook = await _unitOfWork.MedicineRepository.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new UserFriendlyException("Medicine not found", "Medicine not found");

            await _unitOfWork.ExecuteTransactionAsync(() => _unitOfWork.MedicineRepository.Delete(existBook), token);
            return _mapper.Map<MedicineDTO>(existBook);
        }
    }
}

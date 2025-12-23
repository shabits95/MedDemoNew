using AutoMapper;
using MedDemo.Application.DTO;
using MedDemo.Domain.Entities;

namespace MedDemo.Application.Mappings;

public class MapProfile : Profile
{
    public MapProfile()
    {
        CreateMap<Medicine, MedicineDTO>().ReverseMap();
        CreateMap<Medicine, AddMedicineRequest>().ReverseMap();
        CreateMap<Medicine, UpdateMedicineRequest>().ReverseMap();
    }
}

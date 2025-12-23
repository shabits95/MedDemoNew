using MedDemo.Application.DTO;
using MedDemo.Application.Interfaces.IServices;
using MedDemo.Application.Services;
using MedDemo.Application.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MedDemo.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicineController : ControllerBase
    {
        private readonly IMedicineService _medicineService;

        public MedicineController(IMedicineService medicineService)
        {
            _medicineService = medicineService;
        }


        /// <summary>
        /// get a book by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [SwaggerResponse(200, "Medicine details retrieved successfully.", typeof(MedicineDTO))]
        [SwaggerResponse(404, "Medicine not found.")]
        public async Task<IActionResult> Get(Guid id)
            => Ok(await _medicineService.Get(id));

        /// <summary>
        /// get a list of medicine
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        [SwaggerResponse(200, "Medicine retrieved successfully.", typeof(Pagination<MedicineDTO>))]
        public async Task<IActionResult> Get(int pageIndex = 0, int pageSize = 10)
            => Ok(await _medicineService.Get(pageIndex, pageSize));

        /// <summary>
        /// add a medicine
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost]
        [SwaggerResponse(201, "Medicine added successfully.", typeof(MedicineDTO))]
        [SwaggerResponse(400, "Invalid request.")]
        public async Task<IActionResult> Add(AddMedicineRequest request, CancellationToken token)
            => Ok(await _medicineService.Add(request, token));

        /// <summary>
        /// update a medicine
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPut]
        [SwaggerResponse(200, "Medicine updated successfully.", typeof(MedicineDTO))]
        [SwaggerResponse(400, "Invalid request.")]
        [SwaggerResponse(404, "Medicine not found.")]
        public async Task<IActionResult> Update(UpdateMedicineRequest request, CancellationToken token)
            => Ok(await _medicineService.Update(request, token));

        /// <summary>
        /// delete a medicine by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("{id}")]
        [SwaggerResponse(200, "Book deleted successfully.")]
        [SwaggerResponse(404, "Book not found.")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken token)
            => Ok(await _medicineService.Delete(id, token));

    }
}

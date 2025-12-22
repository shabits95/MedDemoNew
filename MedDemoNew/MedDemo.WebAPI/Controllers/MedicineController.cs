using Microsoft.AspNetCore.Mvc;

namespace MedDemo.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MedicineController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<MedicineController> _logger;

        public MedicineController(ILogger<MedicineController> logger)
        {
            _logger = logger;
        }
        
    }
}

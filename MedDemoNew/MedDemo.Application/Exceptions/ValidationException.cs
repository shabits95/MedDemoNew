using MedDemo.Application.DTO.Errors;

namespace MedDemo.Application.Exceptions
{

    public class ValidationException(ErrorResponse errorResponse) : Exception
    {
        public ErrorResponse ErrorResponse { get; private set; } = errorResponse;
    }
}

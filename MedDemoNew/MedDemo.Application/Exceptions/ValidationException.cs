using MedDemo.Application.DTO.Errors;

namespace MedDemo.Application.Common.Exceptions;

public class ValidationException(ErrorResponse errorResponse) : Exception
{
    public ErrorResponse ErrorResponse { get; private set; } = errorResponse;
}

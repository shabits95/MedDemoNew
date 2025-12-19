using System.Diagnostics.CodeAnalysis;
using MedDemo.Domain.Constants;
using MedDemo.Domain.Enums;

namespace MedDemo.Application.Common.Exceptions;

[ExcludeFromCodeCoverage]
public static class ProgramException
{
    public static UserFriendlyException AppsettingNotSetException()
        => new(ErrorCode.Internal, ErrorMessage.AppConfigurationMessage, ErrorMessage.InternalError);
}

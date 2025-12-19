using System.Text.Json.Serialization;

namespace MedDemo.Shared.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Role
{
    Admin,
    User
}

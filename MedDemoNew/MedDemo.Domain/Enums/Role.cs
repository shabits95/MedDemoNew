using System.Text.Json.Serialization;

namespace MedDemo.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Role
{
    Admin,
    User
}

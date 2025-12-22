namespace MedDemo.Application.DTO.Errors;

public class Error(string code, string message)
{
    public string Code { get; set; } = code;
    public string Message { get; set; } = message;
    public string Property { get; set; }

    public void AddErrorProperty(ErrorProperty property)
    {
        Property = property.Property;
    }
}

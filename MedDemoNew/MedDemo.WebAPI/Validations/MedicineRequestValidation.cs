using MedDemo.Domain.Entities;
using FluentValidation;

namespace MedDemo.Web.Validations;

public class MedicineRequestValidation : AbstractValidator<Medicine>
{
    public MedicineRequestValidation()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Brand is required.")
            .MaximumLength(100).WithMessage("Brand must not exceed 100 characters.");

        RuleFor(x => x.Dosage)
            .NotEmpty().WithMessage("Dosage is required.")
            .MaximumLength(100).WithMessage("Dosage must not exceed 100 characters.");

        RuleFor(x => x.Form)
            .NotEmpty().WithMessage("Form is required.")
            .MaximumLength(100).WithMessage("Form must not exceed 100 characters.");

        RuleFor(x => x.Price)
             .NotEmpty().WithMessage("Price is required.")
             .Must(BeAValidDecimal).WithMessage("Price must be a valid decimal number.")
             .GreaterThan(0).WithMessage("Price must be greater than zero.")
             .PrecisionScale(10, 2, true).WithMessage("Price must not exceed 10 digits with 2 decimal places.");

        RuleFor(x => x.ExpiryDate)
            .NotEmpty().WithMessage("Expiry date is required.")
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Expiry date must be in the future.");

        RuleFor(x => x.IsPrescriptionRequired)
            .NotNull()
            .WithMessage("Please specify whether a prescription is required.");
    }
    // Helper method
    private bool BeAValidDecimal(decimal value)
    {
        return value is decimal;
    }
}

namespace MedDemo.Application.DTO
{
    public class MedicineDTO
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Brand { get; set; }

        public string? Description { get; set; }

        public string? Status { get; set; }

        public string Dosage { get; set; } = string.Empty; // e.g. "500 mg"

        public string Form { get; set; } = string.Empty; // Tablet, Syrup, Injection

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public DateTime ExpiryDate { get; set; }

        public bool IsPrescriptionRequired { get; set; }
    }
    public class AddMedicineRequest
    {
        public string Name { get; set; } = string.Empty;

        public string? Brand { get; set; }

        public string? Description { get; set; }

        public string? Status { get; set; }

        public string Dosage { get; set; } = string.Empty; // e.g. "500 mg"

        public string Form { get; set; } = string.Empty; // Tablet, Syrup, Injection

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public DateTime ExpiryDate { get; set; }

        public bool IsPrescriptionRequired { get; set; }
    }
    public class UpdateMedicineRequest
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public string? Brand { get; set; }

        public string? Description { get; set; }

        public string? Status { get; set; }

        public string Dosage { get; set; } = string.Empty; // e.g. "500 mg"

        public string Form { get; set; } = string.Empty; // Tablet, Syrup, Injection

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public DateTime ExpiryDate { get; set; }

        public bool IsPrescriptionRequired { get; set; }
    }
}

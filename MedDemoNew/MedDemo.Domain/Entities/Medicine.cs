namespace MedDemo.Domain.Entities
{
    public class Medicine
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Brand { get; set; }

        public string? Description { get; set; }

        public string Dosage { get; set; } = string.Empty; // e.g. "500 mg"

        public string Form { get; set; } = string.Empty; // Tablet, Syrup, Injection

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public DateTime ExpiryDate { get; set; }

        public bool IsPrescriptionRequired { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }

}

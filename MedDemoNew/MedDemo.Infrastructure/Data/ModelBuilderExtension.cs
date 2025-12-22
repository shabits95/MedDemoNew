using MedDemo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedDemo.Infrastructure.Data
{

    public static class ModelBuilderExtension
    {
        public static void Seed(this ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Medicine>().HasData(
                new Medicine
                {
                    Id = Guid.NewGuid(),
                    Name = "Paracetamol",
                    Brand = "Calpol",
                    Description = "Pain reliever and fever reducer",
                    Status = "Active",
                    Dosage = "500 mg",
                    Form = "Tablet",
                    Price = 5.99m,
                    StockQuantity = 500,
                    ExpiryDate = new DateTime(2026, 12, 31),
                    IsPrescriptionRequired = false,
                    CreatedAt = DateTime.UtcNow
                },
                new Medicine
                {
                    Id = Guid.NewGuid(),
                    Name = "Amoxicillin",
                    Brand = "Amoxil",
                    Description = "Antibiotic used to treat bacterial infections",
                    Status = "Active",
                    Dosage = "250 mg",
                    Form = "Capsule",
                    Price = 15.50m,
                    StockQuantity = 300,
                    ExpiryDate = new DateTime(2026, 6, 30),
                    IsPrescriptionRequired = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Medicine
                {
                    Id = Guid.NewGuid(),
                    Name = "Ibuprofen",
                    Brand = "Advil",
                    Description = "Nonsteroidal anti-inflammatory drug (NSAID)",
                    Status = "Active",
                    Dosage = "400 mg",
                    Form = "Tablet",
                    Price = 8.99m,
                    StockQuantity = 450,
                    ExpiryDate = new DateTime(2027, 3, 15),
                    IsPrescriptionRequired = false,
                    CreatedAt = DateTime.UtcNow
                },
                new Medicine
                {
                    Id = Guid.NewGuid(),
                    Name = "Cetirizine",
                    Brand = "Zyrtec",
                    Description = "Antihistamine for allergy relief",
                    Status = "Active",
                    Dosage = "10 mg",
                    Form = "Tablet",
                    Price = 12.75m,
                    StockQuantity = 200,
                    ExpiryDate = new DateTime(2026, 9, 20),
                    IsPrescriptionRequired = false,
                    CreatedAt = DateTime.UtcNow
                },
                new Medicine
                {
                    Id = Guid.NewGuid(),
                    Name = "Omeprazole",
                    Brand = "Prilosec",
                    Description = "Proton pump inhibitor for acid reflux and heartburn",
                    Status = "Active",
                    Dosage = "20 mg",
                    Form = "Capsule",
                    Price = 18.99m,
                    StockQuantity = 350,
                    ExpiryDate = new DateTime(2027, 1, 10),
                    IsPrescriptionRequired = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Medicine
                {
                    Id = Guid.NewGuid(),
                    Name = "Metformin",
                    Brand = "Glucophage",
                    Description = "Medication for type 2 diabetes",
                    Status = "Active",
                    Dosage = "500 mg",
                    Form = "Tablet",
                    Price = 22.50m,
                    StockQuantity = 400,
                    ExpiryDate = new DateTime(2026, 11, 30),
                    IsPrescriptionRequired = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Medicine
                {
                    Id = Guid.NewGuid(),
                    Name = "Cough Syrup",
                    Brand = "Benadryl",
                    Description = "Relief from cough and cold symptoms",
                    Status = "Active",
                    Dosage = "10 ml",
                    Form = "Syrup",
                    Price = 7.25m,
                    StockQuantity = 150,
                    ExpiryDate = new DateTime(2025, 8, 15),
                    IsPrescriptionRequired = false,
                    CreatedAt = DateTime.UtcNow
                },
                new Medicine
                {
                    Id = Guid.NewGuid(),
                    Name = "Insulin Glargine",
                    Brand = "Lantus",
                    Description = "Long-acting insulin for diabetes management",
                    Status = "Active",
                    Dosage = "100 units/ml",
                    Form = "Injection",
                    Price = 125.00m,
                    StockQuantity = 75,
                    ExpiryDate = new DateTime(2025, 12, 31),
                    IsPrescriptionRequired = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Medicine
                {
                    Id = Guid.NewGuid(),
                    Name = "Aspirin",
                    Brand = "Bayer",
                    Description = "Pain reliever and blood thinner",
                    Status = "Active",
                    Dosage = "75 mg",
                    Form = "Tablet",
                    Price = 4.50m,
                    StockQuantity = 600,
                    ExpiryDate = new DateTime(2027, 5, 20),
                    IsPrescriptionRequired = false,
                    CreatedAt = DateTime.UtcNow
                },
                new Medicine
                {
                    Id = Guid.NewGuid(),
                    Name = "Losartan",
                    Brand = "Cozaar",
                    Description = "Medication for high blood pressure",
                    Status = "Active",
                    Dosage = "50 mg",
                    Form = "Tablet",
                    Price = 28.00m,
                    StockQuantity = 250,
                    ExpiryDate = new DateTime(2026, 7, 25),
                    IsPrescriptionRequired = true,
                    CreatedAt = DateTime.UtcNow
                }
            );      
        }
    }
}

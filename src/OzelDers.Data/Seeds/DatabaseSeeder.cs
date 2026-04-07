using OzelDers.Data.Context;
using OzelDers.Data.Entities;
using OzelDers.Data.Enums;

namespace OzelDers.Data.Seeds;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // 1. Şehirler yoksa ekle
        if (!context.Cities.Any())
        {
            var istanbul = new City { Id = 1, Name = "İstanbul", Slug = "istanbul", PlateCode = 34 };
            var ankara = new City { Id = 2, Name = "Ankara", Slug = "ankara", PlateCode = 06 };
            var izmir = new City { Id = 3, Name = "İzmir", Slug = "izmir", PlateCode = 35 };
            
            await context.Cities.AddRangeAsync(istanbul, ankara, izmir);
            await context.SaveChangesAsync();

            // 2. İlçeler
            await context.Districts.AddRangeAsync(
                new District { CityId = istanbul.Id, Name = "Kadıköy", Slug = "kadikoy" },
                new District { CityId = istanbul.Id, Name = "Beşiktaş", Slug = "besiktas" },
                new District { CityId = ankara.Id, Name = "Çankaya", Slug = "cankaya" }
            );
            await context.SaveChangesAsync();
        }

        // 3. Branşlar
        if (!context.Branches.Any())
        {
            await context.Branches.AddRangeAsync(
                new Branch { Name = "Matematik", Slug = "matematik", IsPopular = true, DisplayOrder = 1 },
                new Branch { Name = "Fizik", Slug = "fizik", IsPopular = false, DisplayOrder = 2 },
                new Branch { Name = "İngilizce", Slug = "ingilizce", IsPopular = true, DisplayOrder = 3 },
                new Branch { Name = "Yazılım (C#)", Slug = "yazilim-csharp", IsPopular = true, DisplayOrder = 4 }
            );
            await context.SaveChangesAsync();
        }

        // Not: Jeton ve Vitrin paketleri artık PackageConfigurations.cs içinde EF Seed Data olarak yönetiliyor.
        // Bu seeder'da tekrar tanımlamaya gerek yok.
    }
}

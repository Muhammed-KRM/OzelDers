using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OzelDers.Data.Entities;

namespace OzelDers.Data.Configurations;

public class TokenPackageConfiguration : IEntityTypeConfiguration<TokenPackage>
{
    public void Configure(EntityTypeBuilder<TokenPackage> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Price).HasColumnType("decimal(18,2)");
    }
}

public class VitrinPackageConfiguration : IEntityTypeConfiguration<VitrinPackage>
{
    public void Configure(EntityTypeBuilder<VitrinPackage> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Name).IsRequired().HasMaxLength(50);
        builder.Property(v => v.Price).HasColumnType("decimal(18,2)");
    }
}

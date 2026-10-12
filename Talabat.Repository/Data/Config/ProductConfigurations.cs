using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Talabat.Core.Models;

namespace Talabat.Repository.Data.Config;

public class ProductConfigurations : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(p => p.Description)
            .IsRequired();
        builder.Property(p => p.PictureUrl)
            .IsRequired();
        builder.Property(p => p.Price)
            .HasColumnType("decimal(18,2)");
    }
}

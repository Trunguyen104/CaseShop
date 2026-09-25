using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CaseShop.Web.Entities;

namespace CaseShop.Web.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderCode).IsRequired().HasMaxLength(20);
        builder.Property(x => x.CustomerName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Phone).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Address).IsRequired().HasMaxLength(255);
        builder.Property(x => x.PaymentMethod)
               .IsRequired()
               .HasMaxLength(50)
               .HasConversion<string>();
        builder.Property(x => x.Status)
               .IsRequired()
               .HasMaxLength(50)
               .HasConversion<string>();
        builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
        
        builder.HasIndex(x => x.OrderCode).IsUnique();

        builder.HasMany(x => x.Items)
               .WithOne(x => x.Order)
               .HasForeignKey(x => x.OrderId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

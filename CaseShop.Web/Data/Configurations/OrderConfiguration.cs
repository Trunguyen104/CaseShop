using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CaseShop.Web.Entities;

namespace CaseShop.Web.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", table =>
        {
            table.HasCheckConstraint("CK_Orders_Amounts", "\"SubtotalAmount\" >= 0 AND \"ShippingFee\" >= 0 AND \"TotalAmount\" = \"SubtotalAmount\" + \"ShippingFee\"");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderCode).IsRequired().HasMaxLength(20);
        builder.Property(x => x.CustomerName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Phone).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Address).IsRequired().HasMaxLength(255);
        builder.Property(x => x.ProvinceName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.WardName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.AddressLine).IsRequired().HasMaxLength(200);
        builder.Property(x => x.PaymentMethod)
               .IsRequired()
               .HasMaxLength(50)
               .HasConversion<string>();
        builder.Property(x => x.Status)
               .IsRequired()
               .HasMaxLength(50)
               .HasConversion<string>();
        builder.Property(x => x.PaymentStatus)
               .IsRequired()
               .HasMaxLength(30)
               .HasConversion<string>();
        builder.Property(x => x.ConfirmationEmailStatus)
               .IsRequired()
               .HasMaxLength(30)
               .HasConversion<string>();
        builder.Property(x => x.SubtotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ShippingFee).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PayOsPaymentLinkId).HasMaxLength(100);
        builder.Property(x => x.PayOsCheckoutUrl).HasMaxLength(500);
        builder.Property(x => x.PayOsQrCode).HasMaxLength(2000);
        builder.Property(x => x.PaymentReference).HasMaxLength(100);
        builder.Property(x => x.ConfirmationEmailLastError).HasMaxLength(1000);
        
        builder.HasIndex(x => x.OrderCode).IsUnique();
        builder.HasIndex(x => x.PayOsOrderCode).IsUnique();

        builder.HasMany(x => x.Items)
               .WithOne(x => x.Order)
               .HasForeignKey(x => x.OrderId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

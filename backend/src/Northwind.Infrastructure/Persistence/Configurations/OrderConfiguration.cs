using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northwind.Domain.Entities;

namespace Northwind.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> entity)
    {
        entity.ToTable("Orders");
        entity.HasKey(o => o.OrderID);
        entity.Property(o => o.OrderID).ValueGeneratedOnAdd();
        entity.Property(o => o.Freight).HasColumnType("money");
        entity.Property(o => o.Latitude).HasColumnType("decimal(18,15)").IsRequired(false);
        entity.Property(o => o.Longitude).HasColumnType("decimal(18,15)").IsRequired(false);

        entity.HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerID);

        entity.HasOne(o => o.Employee)
            .WithMany(e => e.Orders)
            .HasForeignKey(o => o.EmployeeID);

        entity.HasOne(o => o.Shipper)
            .WithMany(s => s.Orders)
            .HasForeignKey(o => o.ShipVia);
    }
}

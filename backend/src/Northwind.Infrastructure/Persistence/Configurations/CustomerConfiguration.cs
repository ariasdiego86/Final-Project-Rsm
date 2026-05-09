using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northwind.Domain.Entities;

namespace Northwind.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> entity)
    {
        entity.ToTable("Customers");
        entity.HasKey(c => c.CustomerID);
        entity.Property(c => c.CustomerID).HasMaxLength(5);
        entity.Property(c => c.CompanyName).HasMaxLength(40).IsRequired();
    }
}

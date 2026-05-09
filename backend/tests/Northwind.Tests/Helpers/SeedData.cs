using Northwind.Domain.Entities;
using Northwind.Infrastructure.Persistence;

namespace Northwind.Tests.Helpers;

public static class SeedData
{
    public static void Seed(AppDbContext context)
    {
        context.Customers.AddRange(
            new Customer { CustomerID = "ALFKI", CompanyName = "Alfreds Futterkiste" },
            new Customer { CustomerID = "ANATR", CompanyName = "Ana Trujillo Emparedados" }
        );

        context.Employees.AddRange(
            new Employee { EmployeeID = 1, FirstName = "Nancy", LastName = "Davolio" },
            new Employee { EmployeeID = 2, FirstName = "Andrew", LastName = "Fuller" }
        );

        context.Shippers.Add(new Shipper { ShipperID = 1, CompanyName = "Speedy Express" });

        context.Products.AddRange(
            new Product { ProductID = 1, ProductName = "Chai", UnitPrice = 18m, Discontinued = false },
            new Product { ProductID = 2, ProductName = "Chang", UnitPrice = 19m, Discontinued = false }
        );

        context.Orders.AddRange(
            new Order
            {
                OrderID = 10248,
                CustomerID = "ALFKI",
                EmployeeID = 1,
                ShipVia = 1,
                OrderDate = new DateTime(1996, 7, 4),
                ShipCountry = "Germany",
                ShipRegion = null,
                OrderDetails = new List<OrderDetail>
                {
                    new() { ProductID = 1, UnitPrice = 18m, Quantity = 2, Discount = 0f },
                }
            },
            new Order
            {
                OrderID = 10249,
                CustomerID = "ANATR",
                EmployeeID = 2,
                ShipVia = 1,
                OrderDate = new DateTime(1996, 7, 5),
                ShipCountry = "Brazil",
                ShipRegion = "SP",
                OrderDetails = new List<OrderDetail>
                {
                    new() { ProductID = 2, UnitPrice = 19m, Quantity = 5, Discount = 0.1f },
                }
            },
            new Order
            {
                OrderID = 10250,
                CustomerID = "ALFKI",
                EmployeeID = 1,
                OrderDate = new DateTime(1997, 3, 15),
                ShipCountry = "Germany",
                OrderDetails = new List<OrderDetail>
                {
                    new() { ProductID = 1, UnitPrice = 18m, Quantity = 10, Discount = 0f },
                }
            }
        );

        context.SaveChanges();
    }
}

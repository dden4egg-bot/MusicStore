namespace MusicStore;

public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Customer";
}

public class Customer
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public decimal TotalSpent { get; set; }
    public int DiscountPercent { get; set; }
    public DateTime RegisteredAt { get; set; }
}

public class Record
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Band { get; set; } = "";
    public string Publisher { get; set; } = "";
    public int TracksCount { get; set; }
    public string Genre { get; set; } = "";
    public int ReleaseYear { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int StockQuantity { get; set; }

    public override string ToString() =>
        $"#{Id,-3} \"{Title}\" - {Band} | {Genre} ({ReleaseYear}) | " +
        $"stock: {StockQuantity} | price: {SalePrice:F2} UAH";
}

public class Sale
{
    public int Id { get; set; }
    public int RecordId { get; set; }
    public int? CustomerId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime SoldAt { get; set; }
}

public class Reservation
{
    public int Id { get; set; }
    public int RecordId { get; set; }
    public int CustomerId { get; set; }
    public int Quantity { get; set; }
    public DateTime ReservedAt { get; set; }
    public string Status { get; set; } = "Active";
}

public class Promotion
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string GenreFilter { get; set; } = "";
    public int DiscountPercent { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public bool IsActive => DateTime.Now >= StartDate && DateTime.Now <= EndDate;
}

public enum Period { Day, Week, Month, Year }
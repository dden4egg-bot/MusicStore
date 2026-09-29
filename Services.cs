using Microsoft.Data.Sqlite;

namespace MusicStore;

// ============================================================
//                    AUTH SERVICE
// ============================================================
public class AuthService
{
    private readonly Database _db;
    public AuthService(Database db) { _db = db; }

    public User? Login(string login, string password)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT Id, Login, PasswordHash, Role
                            FROM Users WHERE Login = @l AND PasswordHash = @p";
        cmd.Parameters.AddWithValue("@l", login);
        cmd.Parameters.AddWithValue("@p", Database.Hash(password));

        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        return new User
        {
            Id = r.GetInt32(0),
            Login = r.GetString(1),
            PasswordHash = r.GetString(2),
            Role = r.GetString(3)
        };
    }

    public Customer RegisterCustomer(string login, string password, string fullName)
    {
        using var conn = _db.Open();

        using (var check = conn.CreateCommand())
        {
            check.CommandText = "SELECT COUNT(*) FROM Users WHERE Login = @l";
            check.Parameters.AddWithValue("@l", login);
            long cnt = (long)(check.ExecuteScalar() ?? 0L);
            if (cnt > 0)
                throw new InvalidOperationException("Login already taken");
        }

        long userId;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"INSERT INTO Users (Login, PasswordHash, Role)
                                VALUES (@l, @p, 'Customer');
                                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@l", login);
            cmd.Parameters.AddWithValue("@p", Database.Hash(password));
            userId = (long)(cmd.ExecuteScalar() ?? 0L);
        }

        long customerId;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"INSERT INTO Customers
                                (UserId, FullName, TotalSpent, DiscountPercent, RegisteredAt)
                                VALUES (@u, @n, 0, 0, @d);
                                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@u", userId);
            cmd.Parameters.AddWithValue("@n", fullName);
            cmd.Parameters.AddWithValue("@d", DateTime.Now.ToString("o"));
            customerId = (long)(cmd.ExecuteScalar() ?? 0L);
        }

        return new Customer
        {
            Id = (int)customerId,
            UserId = (int)userId,
            FullName = fullName,
            TotalSpent = 0,
            DiscountPercent = 0,
            RegisteredAt = DateTime.Now
        };
    }

    public Customer? GetCustomerByUserId(int userId)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT Id, UserId, FullName, TotalSpent, DiscountPercent, RegisteredAt
                            FROM Customers WHERE UserId = @u";
        cmd.Parameters.AddWithValue("@u", userId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        return new Customer
        {
            Id = r.GetInt32(0),
            UserId = r.GetInt32(1),
            FullName = r.GetString(2),
            TotalSpent = (decimal)r.GetDouble(3),
            DiscountPercent = r.GetInt32(4),
            RegisteredAt = DateTime.Parse(r.GetString(5))
        };
    }

    public static int CalculateDiscount(decimal totalSpent) => totalSpent switch
    {
        >= 50000 => 10,
        >= 15000 => 7,
        >= 5000 => 5,
        >= 1000 => 3,
        _ => 0
    };
}

// ============================================================
//                    RECORD SERVICE
// ============================================================
public class RecordService
{
    private readonly Database _db;
    public RecordService(Database db) { _db = db; }

    public List<Record> GetAll()
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Records ORDER BY Id";
        return ReadRecords(cmd);
    }

    public Record? GetById(int id)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Records WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        return ReadRecords(cmd).FirstOrDefault();
    }

    public List<Record> Search(string? title, string? band, string? genre)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT * FROM Records
            WHERE (@t IS NULL OR Title LIKE '%' || @t || '%')
              AND (@b IS NULL OR Band  LIKE '%' || @b || '%')
              AND (@g IS NULL OR Genre LIKE '%' || @g || '%')
            ORDER BY Title";
        cmd.Parameters.AddWithValue("@t", (object?)title ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@b", (object?)band ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@g", (object?)genre ?? DBNull.Value);
        return ReadRecords(cmd);
    }

    public void Add(Record r)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Records
            (Title, Band, Publisher, TracksCount, Genre, ReleaseYear, CostPrice, SalePrice, StockQuantity)
            VALUES (@t, @b, @p, @tr, @g, @y, @c, @sp, @q);
            SELECT last_insert_rowid();";
        BindRecord(cmd, r);
        r.Id = (int)(long)(cmd.ExecuteScalar() ?? 0L);
    }

    public void Update(Record r)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Records SET
            Title=@t, Band=@b, Publisher=@p, TracksCount=@tr, Genre=@g,
            ReleaseYear=@y, CostPrice=@c, SalePrice=@sp, StockQuantity=@q
            WHERE Id=@id";
        BindRecord(cmd, r);
        cmd.Parameters.AddWithValue("@id", r.Id);
        cmd.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Records WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public bool WriteOff(int id, int quantity)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Records
                            SET StockQuantity = StockQuantity - @q
                            WHERE Id = @id AND StockQuantity >= @q";
        cmd.Parameters.AddWithValue("@q", quantity);
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    private static void BindRecord(SqliteCommand cmd, Record r)
    {
        cmd.Parameters.AddWithValue("@t", r.Title);
        cmd.Parameters.AddWithValue("@b", r.Band);
        cmd.Parameters.AddWithValue("@p", r.Publisher);
        cmd.Parameters.AddWithValue("@tr", r.TracksCount);
        cmd.Parameters.AddWithValue("@g", r.Genre);
        cmd.Parameters.AddWithValue("@y", r.ReleaseYear);
        cmd.Parameters.AddWithValue("@c", (double)r.CostPrice);
        cmd.Parameters.AddWithValue("@sp", (double)r.SalePrice);
        cmd.Parameters.AddWithValue("@q", r.StockQuantity);
    }

    private static List<Record> ReadRecords(SqliteCommand cmd)
    {
        var list = new List<Record>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Record
            {
                Id = r.GetInt32(0),
                Title = r.GetString(1),
                Band = r.GetString(2),
                Publisher = r.GetString(3),
                TracksCount = r.GetInt32(4),
                Genre = r.GetString(5),
                ReleaseYear = r.GetInt32(6),
                CostPrice = (decimal)r.GetDouble(7),
                SalePrice = (decimal)r.GetDouble(8),
                StockQuantity = r.GetInt32(9)
            });
        }
        return list;
    }
}

// ============================================================
//                    SALE SERVICE
// ============================================================
public class SaleService
{
    private readonly Database _db;
    private readonly RecordService _records;

    public SaleService(Database db, RecordService records)
    {
        _db = db; _records = records;
    }

    public Sale Sell(int recordId, int quantity, Customer? customer)
    {
        var rec = _records.GetById(recordId)
                  ?? throw new InvalidOperationException("Record not found");
        if (rec.StockQuantity < quantity)
            throw new InvalidOperationException("Not enough stock");

        decimal unit = GetEffectivePrice(rec);
        decimal total = unit * quantity;

        if (customer != null)
            total *= (100 - customer.DiscountPercent) / 100m;

        using var conn = _db.Open();
        using var tx = conn.BeginTransaction();

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE Records SET StockQuantity = StockQuantity - @q WHERE Id = @id";
            cmd.Parameters.AddWithValue("@q", quantity);
            cmd.Parameters.AddWithValue("@id", recordId);
            cmd.ExecuteNonQuery();
        }

        long saleId;
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT INTO Sales
                (RecordId, CustomerId, Quantity, UnitPrice, TotalPrice, SoldAt)
                VALUES (@r, @c, @q, @u, @t, @d);
                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@r", recordId);
            cmd.Parameters.AddWithValue("@c", (object?)customer?.Id ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@q", quantity);
            cmd.Parameters.AddWithValue("@u", (double)unit);
            cmd.Parameters.AddWithValue("@t", (double)total);
            cmd.Parameters.AddWithValue("@d", DateTime.Now.ToString("o"));
            saleId = (long)(cmd.ExecuteScalar() ?? 0L);
        }

        if (customer != null)
        {
            customer.TotalSpent += total;
            customer.DiscountPercent = AuthService.CalculateDiscount(customer.TotalSpent);

            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"UPDATE Customers
                                SET TotalSpent = @s, DiscountPercent = @d
                                WHERE Id = @id";
            cmd.Parameters.AddWithValue("@s", (double)customer.TotalSpent);
            cmd.Parameters.AddWithValue("@d", customer.DiscountPercent);
            cmd.Parameters.AddWithValue("@id", customer.Id);
            cmd.ExecuteNonQuery();
        }

        tx.Commit();

        return new Sale
        {
            Id = (int)saleId,
            RecordId = recordId,
            CustomerId = customer?.Id,
            Quantity = quantity,
            UnitPrice = unit,
            TotalPrice = total,
            SoldAt = DateTime.Now
        };
    }

    public void Reserve(int recordId, int customerId, int quantity)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Reservations
            (RecordId, CustomerId, Quantity, ReservedAt, Status)
            VALUES (@r, @c, @q, @d, 'Active')";
        cmd.Parameters.AddWithValue("@r", recordId);
        cmd.Parameters.AddWithValue("@c", customerId);
        cmd.Parameters.AddWithValue("@q", quantity);
        cmd.Parameters.AddWithValue("@d", DateTime.Now.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public List<Reservation> GetActiveReservations()
    {
        var list = new List<Reservation>();
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Reservations WHERE Status='Active' ORDER BY ReservedAt";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Reservation
            {
                Id = r.GetInt32(0),
                RecordId = r.GetInt32(1),
                CustomerId = r.GetInt32(2),
                Quantity = r.GetInt32(3),
                ReservedAt = DateTime.Parse(r.GetString(4)),
                Status = r.GetString(5)
            });
        }
        return list;
    }

    public void AddPromotion(string name, string genre, int discount, DateTime from, DateTime to)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Promotions
            (Name, GenreFilter, DiscountPercent, StartDate, EndDate)
            VALUES (@n, @g, @d, @f, @t)";
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@g", genre);
        cmd.Parameters.AddWithValue("@d", discount);
        cmd.Parameters.AddWithValue("@f", from.ToString("o"));
        cmd.Parameters.AddWithValue("@t", to.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public List<Promotion> GetActivePromotions()
    {
        var list = new List<Promotion>();
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Promotions ORDER BY Id";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var p = new Promotion
            {
                Id = r.GetInt32(0),
                Name = r.GetString(1),
                GenreFilter = r.GetString(2),
                DiscountPercent = r.GetInt32(3),
                StartDate = DateTime.Parse(r.GetString(4)),
                EndDate = DateTime.Parse(r.GetString(5))
            };
            if (p.IsActive) list.Add(p);
        }
        return list;
    }

    public decimal GetEffectivePrice(Record rec)
    {
        var promo = GetActivePromotions()
            .FirstOrDefault(p => string.Equals(p.GenreFilter, rec.Genre,
                                               StringComparison.OrdinalIgnoreCase));

        if (promo == null) return rec.SalePrice;
        return rec.SalePrice * (100 - promo.DiscountPercent) / 100m;
    }
}

// ============================================================
//                    REPORT SERVICE
// ============================================================
public class ReportService
{
    private readonly Database _db;
    public ReportService(Database db) { _db = db; }

    public List<Record> GetNewArrivals(int top = 5)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Records ORDER BY ReleaseYear DESC LIMIT @n";
        cmd.Parameters.AddWithValue("@n", top);
        return ReadRecords(cmd);
    }

    public List<(string Label, int Count)> GetPopularRecords(Period p, int top = 5)
        => PopularBy("r.Title", p, top);

    public List<(string Label, int Count)> GetPopularArtists(Period p, int top = 5)
        => PopularBy("r.Band", p, top);

    public List<(string Label, int Count)> GetPopularGenres(Period p, int top = 5)
        => PopularBy("r.Genre", p, top);

    private List<(string, int)> PopularBy(string column, Period p, int top)
    {
        var from = GetStart(p);

        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            SELECT {column} AS Label, COUNT(*) AS Cnt
            FROM Sales s
            JOIN Records r ON r.Id = s.RecordId
            WHERE s.SoldAt >= @from
            GROUP BY {column}
            ORDER BY Cnt DESC
            LIMIT @top";
        cmd.Parameters.AddWithValue("@from", from.ToString("o"));
        cmd.Parameters.AddWithValue("@top", top);

        var list = new List<(string, int)>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add((r.GetString(0), r.GetInt32(1)));
        return list;
    }

    private static DateTime GetStart(Period p) => p switch
    {
        Period.Day => DateTime.Now.AddDays(-1),
        Period.Week => DateTime.Now.AddDays(-7),
        Period.Month => DateTime.Now.AddMonths(-1),
        Period.Year => DateTime.Now.AddYears(-1),
        _ => DateTime.MinValue
    };

    private static List<Record> ReadRecords(SqliteCommand cmd)
    {
        var list = new List<Record>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Record
            {
                Id = r.GetInt32(0),
                Title = r.GetString(1),
                Band = r.GetString(2),
                Publisher = r.GetString(3),
                TracksCount = r.GetInt32(4),
                Genre = r.GetString(5),
                ReleaseYear = r.GetInt32(6),
                CostPrice = (decimal)r.GetDouble(7),
                SalePrice = (decimal)r.GetDouble(8),
                StockQuantity = r.GetInt32(9)
            });
        }
        return list;
    }
}
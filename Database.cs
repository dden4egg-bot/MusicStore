using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;

namespace MusicStore;

public class Database
{
    private readonly string _connStr;

    public Database(string path = "music_store.db")
    {
        _connStr = $"Data Source={path}";
    }

    public SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connStr);
        conn.Open();
        return conn;
    }

    public void CreateSchema()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                Login         TEXT NOT NULL UNIQUE,
                PasswordHash  TEXT NOT NULL,
                Role          TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Customers (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId          INTEGER NOT NULL,
                FullName        TEXT NOT NULL,
                TotalSpent      REAL NOT NULL DEFAULT 0,
                DiscountPercent INTEGER NOT NULL DEFAULT 0,
                RegisteredAt    TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Records (
                Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                Title          TEXT NOT NULL,
                Band           TEXT NOT NULL,
                Publisher      TEXT NOT NULL,
                TracksCount    INTEGER NOT NULL,
                Genre          TEXT NOT NULL,
                ReleaseYear    INTEGER NOT NULL,
                CostPrice      REAL NOT NULL,
                SalePrice      REAL NOT NULL,
                StockQuantity  INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Sales (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                RecordId    INTEGER NOT NULL,
                CustomerId  INTEGER NULL,
                Quantity    INTEGER NOT NULL,
                UnitPrice   REAL NOT NULL,
                TotalPrice  REAL NOT NULL,
                SoldAt      TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Reservations (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                RecordId    INTEGER NOT NULL,
                CustomerId  INTEGER NOT NULL,
                Quantity    INTEGER NOT NULL,
                ReservedAt  TEXT NOT NULL,
                Status      TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Promotions (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                Name            TEXT NOT NULL,
                GenreFilter     TEXT NOT NULL,
                DiscountPercent INTEGER NOT NULL,
                StartDate       TEXT NOT NULL,
                EndDate         TEXT NOT NULL
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public void SeedIfEmpty()
    {
        using var conn = Open();

        using (var check = conn.CreateCommand())
        {
            check.CommandText = "SELECT COUNT(*) FROM Users";
            long count = (long)(check.ExecuteScalar() ?? 0L);
            if (count > 0) return;
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"INSERT INTO Users (Login, PasswordHash, Role)
                                VALUES (@l, @p, 'Admin')";
            cmd.Parameters.AddWithValue("@l", "admin");
            cmd.Parameters.AddWithValue("@p", Hash("admin"));
            cmd.ExecuteNonQuery();
        }

        var demoRecords = new (string title, string band, string publisher, int tracks,
                               string genre, int year, decimal cost, decimal price, int stock)[]
        {
            ("Kind of Blue",           "Miles Davis",     "Columbia",  5,  "Jazz",       1959, 300, 750,  12),
            ("A Love Supreme",         "John Coltrane",   "Impulse!",  3,  "Jazz",       1965, 320, 800,   8),
            ("Thriller",               "Michael Jackson", "Epic",      9,  "Pop",        1982, 250, 650,  20),
            ("The Wall",               "Pink Floyd",      "Harvest",   26, "Rock",       1979, 400, 950,  10),
            ("Nevermind",              "Nirvana",         "DGC",       13, "Rock",       1991, 280, 700,  15),
            ("Back in Black",          "AC/DC",           "Atlantic",  10, "Rock",       1980, 260, 680,   9),
            ("Bad",                    "Michael Jackson", "Epic",      11, "Pop",        1987, 260, 660,  14),
            ("Blue Train",             "John Coltrane",   "Blue Note", 5,  "Jazz",       1958, 300, 780,   6),
            ("Random Access Memories", "Daft Punk",       "Columbia",  13, "Electronic", 2013, 380, 900,  11),
        };

        foreach (var r in demoRecords)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Records
                (Title, Band, Publisher, TracksCount, Genre, ReleaseYear, CostPrice, SalePrice, StockQuantity)
                VALUES (@t, @b, @p, @tr, @g, @y, @c, @sp, @q)";
            cmd.Parameters.AddWithValue("@t", r.title);
            cmd.Parameters.AddWithValue("@b", r.band);
            cmd.Parameters.AddWithValue("@p", r.publisher);
            cmd.Parameters.AddWithValue("@tr", r.tracks);
            cmd.Parameters.AddWithValue("@g", r.genre);
            cmd.Parameters.AddWithValue("@y", r.year);
            cmd.Parameters.AddWithValue("@c", r.cost);
            cmd.Parameters.AddWithValue("@sp", r.price);
            cmd.Parameters.AddWithValue("@q", r.stock);
            cmd.ExecuteNonQuery();
        }
    }

    public static string Hash(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        return Convert.ToBase64String(sha.ComputeHash(bytes));
    }
}
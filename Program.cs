using MusicStore;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var db = new Database("music_store.db");
db.CreateSchema();
db.SeedIfEmpty();

var auth = new AuthService(db);
var records = new RecordService(db);
var sales = new SaleService(db, records);
var reports = new ReportService(db);

while (true)
{
    Console.WriteLine("\n===== MUSIC STORE =====");
    Console.WriteLine("1. Login");
    Console.WriteLine("2. Register as customer");
    Console.WriteLine("0. Exit");
    Console.Write("Choice: ");

    switch (Console.ReadLine())
    {
        case "1": DoLogin(auth, records, sales, reports); break;
        case "2": DoRegister(auth); break;
        case "0": return;
        default: Console.WriteLine("Invalid choice"); break;
    }
}

// ============================================================
//                   AUTH FLOW
// ============================================================
static void DoLogin(AuthService auth, RecordService records, SaleService sales, ReportService reports)
{
    Console.Write("Login: ");
    var login = Console.ReadLine() ?? "";
    Console.Write("Password: ");
    var pwd = Console.ReadLine() ?? "";

    var user = auth.Login(login, pwd);
    if (user == null) { Console.WriteLine("Invalid login or password"); return; }

    Console.WriteLine($"Welcome, {user.Login} ({user.Role})");

    if (user.Role == "Admin")
        AdminMenu(records, sales, reports);
    else
        CustomerMenu(auth, user, records, sales, reports);
}

static void DoRegister(AuthService auth)
{
    Console.Write("New login: ");
    var login = Console.ReadLine() ?? "";
    Console.Write("Password: ");
    var pwd = Console.ReadLine() ?? "";
    Console.Write("Full name: ");
    var name = Console.ReadLine() ?? "";

    try
    {
        var c = auth.RegisterCustomer(login, pwd, name);
        Console.WriteLine($"Registered. Customer #{c.Id}, discount {c.DiscountPercent}%");
    }
    catch (Exception ex) { Console.WriteLine("Error: " + ex.Message); }
}

// ============================================================
//                   ADMIN MENU
// ============================================================
static void AdminMenu(RecordService records, SaleService sales, ReportService reports)
{
    while (true)
    {
        Console.WriteLine("\n--- ADMIN MENU ---");
        Console.WriteLine("1. Show all records");
        Console.WriteLine("2. Add record");
        Console.WriteLine("3. Edit record");
        Console.WriteLine("4. Delete record");
        Console.WriteLine("5. Write off record");
        Console.WriteLine("6. Add promotion");
        Console.WriteLine("7. Reports");
        Console.WriteLine("0. Logout");
        Console.Write("Choice: ");

        switch (Console.ReadLine())
        {
            case "1": PrintList(records.GetAll()); break;
            case "2": AddRecord(records); break;
            case "3": EditRecord(records); break;
            case "4": DeleteRecord(records); break;
            case "5": WriteOff(records); break;
            case "6": AddPromotion(sales); break;
            case "7": ReportsMenu(reports); break;
            case "0": return;
            default: Console.WriteLine("Invalid choice"); break;
        }
    }
}

// ============================================================
//                   CUSTOMER MENU
// ============================================================
static void CustomerMenu(AuthService auth, User user, RecordService records,
                         SaleService sales, ReportService reports)
{
    var customer = auth.GetCustomerByUserId(user.Id);
    if (customer == null) { Console.WriteLine("Customer record not found"); return; }

    while (true)
    {
        Console.WriteLine($"\n--- CUSTOMER MENU ({customer.FullName}, " +
                          $"spent: {customer.TotalSpent:F2}, discount: {customer.DiscountPercent}%) ---");
        Console.WriteLine("1. Show all records");
        Console.WriteLine("2. Search");
        Console.WriteLine("3. Buy record");
        Console.WriteLine("4. Reserve record");
        Console.WriteLine("5. Reports");
        Console.WriteLine("0. Logout");
        Console.Write("Choice: ");

        switch (Console.ReadLine())
        {
            case "1": PrintList(records.GetAll()); break;
            case "2": SearchRecords(records); break;
            case "3": BuyRecord(sales, customer); break;
            case "4": ReserveRecord(sales, customer); break;
            case "5": ReportsMenu(reports); break;
            case "0": return;
            default: Console.WriteLine("Invalid choice"); break;
        }
    }
}

// ============================================================
//                   REPORTS MENU
// ============================================================
static void ReportsMenu(ReportService reports)
{
    Console.WriteLine("\n--- REPORTS ---");
    Console.WriteLine("1. New arrivals");
    Console.WriteLine("2. Popular records");
    Console.WriteLine("3. Popular artists");
    Console.WriteLine("4. Popular genres");
    Console.Write("Choice: ");

    var choice = Console.ReadLine();
    if (choice == "1")
    {
        PrintList(reports.GetNewArrivals());
        return;
    }

    Console.Write("Period (d/w/m/y): ");
    var p = Console.ReadLine()?.ToLower() switch
    {
        "d" => Period.Day,
        "w" => Period.Week,
        "m" => Period.Month,
        "y" => Period.Year,
        _ => Period.Month
    };

    var data = choice switch
    {
        "2" => reports.GetPopularRecords(p),
        "3" => reports.GetPopularArtists(p),
        "4" => reports.GetPopularGenres(p),
        _ => new List<(string, int)>()
    };

    Console.WriteLine($"\nPeriod: {p}");
    if (data.Count == 0) { Console.WriteLine("No data"); return; }
    foreach (var (label, count) in data)
        Console.WriteLine($"  {label}: {count}");
}

// ============================================================
//                   RECORD HELPERS
// ============================================================
static void AddRecord(RecordService records)
{
    var r = new Record();
    ReadRecordFields(r);
    records.Add(r);
    Console.WriteLine($"Added record #{r.Id}");
}

static void EditRecord(RecordService records)
{
    Console.Write("Record Id: ");
    if (!int.TryParse(Console.ReadLine(), out int id)) return;

    var r = records.GetById(id);
    if (r == null) { Console.WriteLine("Not found"); return; }

    Console.WriteLine("Current: " + r);
    Console.WriteLine("Enter new values (empty = keep current):");

    r.Title = Prompt("Title", r.Title);
    r.Band = Prompt("Band", r.Band);
    r.Publisher = Prompt("Publisher", r.Publisher);
    r.TracksCount = PromptInt("Tracks", r.TracksCount);
    r.Genre = Prompt("Genre", r.Genre);
    r.ReleaseYear = PromptInt("Year", r.ReleaseYear);
    r.CostPrice = PromptDecimal("Cost", r.CostPrice);
    r.SalePrice = PromptDecimal("Sale price", r.SalePrice);
    r.StockQuantity = PromptInt("Stock", r.StockQuantity);

    records.Update(r);
    Console.WriteLine("Updated");
}

static void DeleteRecord(RecordService records)
{
    Console.Write("Record Id: ");
    if (!int.TryParse(Console.ReadLine(), out int id)) return;
    records.Delete(id);
    Console.WriteLine("Deleted (if existed)");
}

static void WriteOff(RecordService records)
{
    Console.Write("Record Id: ");
    if (!int.TryParse(Console.ReadLine(), out int id)) return;
    Console.Write("Quantity to write off: ");
    if (!int.TryParse(Console.ReadLine(), out int q)) return;

    Console.WriteLine(records.WriteOff(id, q) ? "Written off" : "Not enough stock or not found");
}

static void AddPromotion(SaleService sales)
{
    Console.Write("Promotion name: "); var name = Console.ReadLine() ?? "";
    Console.Write("Genre filter: "); var genre = Console.ReadLine() ?? "";
    Console.Write("Discount %: ");
    if (!int.TryParse(Console.ReadLine(), out int d)) return;

    sales.AddPromotion(name, genre, d, DateTime.Now, DateTime.Now.AddDays(7));
    Console.WriteLine("Promotion added for 7 days");
}

static void BuyRecord(SaleService sales, Customer customer)
{
    Console.Write("Record Id: ");
    if (!int.TryParse(Console.ReadLine(), out int id)) return;
    Console.Write("Quantity: ");
    if (!int.TryParse(Console.ReadLine(), out int q)) return;

    try
    {
        var s = sales.Sell(id, q, customer);
        Console.WriteLine($"Sold. Unit = {s.UnitPrice:F2}, Total = {s.TotalPrice:F2} UAH");
        Console.WriteLine($"Your discount is now {customer.DiscountPercent}%");
    }
    catch (Exception ex) { Console.WriteLine("Error: " + ex.Message); }
}

static void ReserveRecord(SaleService sales, Customer customer)
{
    Console.Write("Record Id: ");
    if (!int.TryParse(Console.ReadLine(), out int id)) return;
    Console.Write("Quantity: ");
    if (!int.TryParse(Console.ReadLine(), out int q)) return;

    sales.Reserve(id, customer.Id, q);
    Console.WriteLine("Reserved");
}

static void SearchRecords(RecordService records)
{
    Console.Write("Title (or empty): "); var t = Console.ReadLine();
    Console.Write("Band (or empty): "); var b = Console.ReadLine();
    Console.Write("Genre (or empty): "); var g = Console.ReadLine();

    var list = records.Search(
        string.IsNullOrWhiteSpace(t) ? null : t,
        string.IsNullOrWhiteSpace(b) ? null : b,
        string.IsNullOrWhiteSpace(g) ? null : g);

    PrintList(list);
}

// ============================================================
//                   SMALL HELPERS
// ============================================================
static void ReadRecordFields(Record r)
{
    Console.Write("Title: "); r.Title = Console.ReadLine() ?? "";
    Console.Write("Band: "); r.Band = Console.ReadLine() ?? "";
    Console.Write("Publisher: "); r.Publisher = Console.ReadLine() ?? "";
    Console.Write("Tracks count: "); r.TracksCount = int.Parse(Console.ReadLine() ?? "0");
    Console.Write("Genre: "); r.Genre = Console.ReadLine() ?? "";
    Console.Write("Release year: "); r.ReleaseYear = int.Parse(Console.ReadLine() ?? "0");
    Console.Write("Cost price: "); r.CostPrice = decimal.Parse(Console.ReadLine() ?? "0");
    Console.Write("Sale price: "); r.SalePrice = decimal.Parse(Console.ReadLine() ?? "0");
    Console.Write("Stock: "); r.StockQuantity = int.Parse(Console.ReadLine() ?? "0");
}

static string Prompt(string field, string current)
{
    Console.Write($"{field} [{current}]: ");
    var v = Console.ReadLine();
    return string.IsNullOrWhiteSpace(v) ? current : v;
}

static int PromptInt(string field, int current)
{
    Console.Write($"{field} [{current}]: ");
    var v = Console.ReadLine();
    return int.TryParse(v, out var r) ? r : current;
}

static decimal PromptDecimal(string field, decimal current)
{
    Console.Write($"{field} [{current}]: ");
    var v = Console.ReadLine();
    return decimal.TryParse(v, out var r) ? r : current;
}

static void PrintList(List<Record> list)
{
    if (list.Count == 0) { Console.WriteLine("(empty)"); return; }
    foreach (var r in list) Console.WriteLine(r);
}
using System.Text.Json;
using BudgetTracker.Models;
using BudgetTracker.Services;

class Program
{
    private const string FilePath = "budget.json";
    private const string UsersPath = "users.json";
    private const string LimitsPath = "limits.json";

    private static readonly TransactionService Transactions = new();
    private static readonly UserService Users = new();
    private static readonly BudgetService Budget = new();

    static void Main()
    {
        LoadUsers();
        LoadTransactions();
        LoadLimits();

        while (true)
        {
            Console.WriteLine("\n--- Budget Tracker ---");
            Console.WriteLine("1. Login");
            Console.WriteLine("2. Logout");
            Console.WriteLine("3. View Balance");
            Console.WriteLine("4. Add Income");
            Console.WriteLine("5. Add Expense");
            Console.WriteLine("6. Edit Transaction");
            Console.WriteLine("7. Delete Transaction");
            Console.WriteLine("8. Undo Transaction");
            Console.WriteLine("9. Redo Transaction");
            Console.WriteLine("10. Show Transactions History");
            Console.WriteLine("11. Exit");

            Console.Write("Choose: ");
            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1": Login(); break;
                case "2": Users.Logout(); break;
                case "3":
                    if (!RequireLogin()) break;
                    Console.WriteLine($"Balance: {Transactions.GetBalance():0.00}");
                    break;
                case "4": AddTransaction(true); break;
                case "5": AddTransaction(false); break;
                case "6": EditTransaction(); break;
                case "7": DeleteTransaction(); break;
                case "8": UndoTransaction(); break;
                case "9": RedoTransaction(); break;
                case "10": ShowTransactionsHistory(); break;
                case "11": SaveAll(); return;
                default: Console.WriteLine("Invalid choice!"); break;
            }
        }
    }

    private static bool RequireLogin()
    {
        if (Users.LoggedInUser == null)
        {
            Console.WriteLine("You must be logged in first!");
            return false;
        }
        return true;
    }

    private static void AddTransaction(bool isIncome)
    {
        if (!RequireLogin()) return;

        Console.Write("Description: ");
        var desc = Console.ReadLine()!;
        Console.Write("Category: ");
        var cat = Console.ReadLine()!;
        Console.Write("Amount: ");
        if (!decimal.TryParse(Console.ReadLine(), out var amount))
        {
            Console.WriteLine("Invalid amount!");
            return;
        }
        if (!isIncome) amount *= -1;

        Transactions.AddTransaction(new Transaction
        {
            Description = desc,
            Category = cat,
            Amount = amount,
            Date = DateTime.Now
        });

        foreach (var w in Budget.CheckExceeded(Transactions.Transactions))
            Console.WriteLine(w);
    }

    private static void EditTransaction()
    {
        if (!RequireLogin()) return;

        ShowTransactionsHistory();
        Console.Write("Enter index to edit: ");
        if (!int.TryParse(Console.ReadLine(), out var idx)) return;

        Console.Write("New Description: ");
        var desc = Console.ReadLine()!;
        Console.Write("New Category: ");
        var cat = Console.ReadLine()!;
        Console.Write("New Amount: ");
        decimal.TryParse(Console.ReadLine(), out var amt);

        Transactions.EditTransaction(idx, new Transaction
        {
            Description = desc,
            Category = cat,
            Amount = amt,
            Date = DateTime.Now
        });
    }

    private static void DeleteTransaction()
    {
        if (!RequireLogin()) return;

        ShowTransactionsHistory();
        Console.Write("Enter index to delete: ");
        if (int.TryParse(Console.ReadLine(), out var idx))
        {
            Transactions.DeleteTransaction(idx);
        }
    }

    private static void UndoTransaction()
    {
        if (!RequireLogin()) return;
        Transactions.UndoTransaction();
        Console.WriteLine("Undo completed.");
    }

    private static void RedoTransaction()
    {
        if (!RequireLogin()) return;
        Transactions.RedoTransaction();
        Console.WriteLine("Redo completed.");
    }

    private static void ShowTransactionsHistory()
    {
        if (!RequireLogin()) return;

        var i = 0;
        foreach (var t in Transactions.Transactions)
        {
            Console.WriteLine($"[{i}] {t.Date:d} | {t.Category} | {t.Description} | {t.Amount}");
            i++;
        }
    }

    private static void Login()
    {
        Console.Write("Name: ");
        var name = Console.ReadLine()!;
        Console.Write("Password: ");
        var pass = Console.ReadLine()!;

        if (!Users.Login(name, pass))
            Console.WriteLine("Invalid credentials!");
        else
            Console.WriteLine($"Logged in as {Users.LoggedInUser!.Name}");
    }

    private static void LoadUsers()
    {
        if (!File.Exists(UsersPath)) return;
        var json = File.ReadAllText(UsersPath);
        var loaded = JsonSerializer.Deserialize<List<User>>(json);
        if (loaded != null)
            foreach (var u in loaded)
                if (u.Name != null && u.Password != null) Users.RegisterUser(u.Name, u.Password);
    }

    private static void LoadTransactions()
    {
        if (!File.Exists(FilePath)) return;
        var json = File.ReadAllText(FilePath);
        var loaded = JsonSerializer.Deserialize<List<Transaction>>(json);
        if (loaded != null)
            foreach (var t in loaded) Transactions.AddTransaction(t);
    }

    private static void LoadLimits()
    {
        if (!File.Exists(LimitsPath))
        {
            var defaults = new List<BudgetLimit>
            {
                new() { Category = "Groceries", Limit = 400 },
                new() { Category = "Rent", Limit = 900 },
                new() { Category = "Entertainment", Limit = 100 }
            };
            File.WriteAllText(LimitsPath, JsonSerializer.Serialize(defaults, new JsonSerializerOptions { WriteIndented = true }));
        }

        var json = File.ReadAllText(LimitsPath);
        var limits = JsonSerializer.Deserialize<List<BudgetLimit>>(json)!;
        Budget.SetLimits(limits);
    }

    private static void SaveAll()
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize(Transactions.Transactions, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(UsersPath, JsonSerializer.Serialize(Users.Users, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(LimitsPath, JsonSerializer.Serialize(Budget.Limits, new JsonSerializerOptions { WriteIndented = true }));
    }
}
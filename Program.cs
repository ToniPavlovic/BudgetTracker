using BudgetTracker.Models;
using BudgetTracker.Services;
using BudgetTracker.Storage;

class Program
{
    private static readonly TransactionService Transactions = new(new JsonStorageProvider<Transaction>());
    private static readonly UserService Users = new(new JsonStorageProvider<User>());
    private static readonly BudgetService Budget = new(new JsonStorageProvider<BudgetLimit>());

    static void Main()
    {
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
            Console.WriteLine("11. Admin Menu");
            Console.WriteLine("12. Exit");

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
                case "11": AdminMenu(); break;
                case "12": return;
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
        Console.Write("Enter index of transaction to edit: ");
        if (!int.TryParse(Console.ReadLine(), out var idx)) return;

        Console.Write("New Description (leave blank to keep old): ");
        var desc = Console.ReadLine()!;
        Console.Write("New Category (leave blank to keep old): ");
        var cat = Console.ReadLine()!;
        Console.Write("New Amount (leave blank to keep old): ");
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
    
    private static void AdminMenu()
    {
        if (!RequireLogin()) return;
        if (Users.LoggedInUser!.Role != "Admin")
        {
            Console.WriteLine("Access denied. Admins only!");
            return;
        }

        while (true)
        {
            Console.WriteLine("\n--- Admin Menu ---");
            Console.WriteLine("1. Register New User");
            Console.WriteLine("2. List Users");
            Console.WriteLine("3. Delete User");
            Console.WriteLine("4. Back");

            Console.Write("Choose: ");
            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1": RegisterUserCli(); break;
                case "2": ListUsers(); break;
                case "3": DeleteUserCli(); break;
                case "4": return;
                default: Console.WriteLine("Invalid choice!"); break;
            }
        }
    }

    private static void RegisterUserCli()
    {
        Console.Write("Name: ");
        var name = Console.ReadLine()!;
        Console.Write("Password: ");
        var pass = Console.ReadLine()!;
        Console.Write("Role (Admin/User): ");
        var role = Console.ReadLine() ?? "User";

        Users.RegisterUser(name, pass, role);
    }

    private static void ListUsers()
    {
        foreach (var u in Users.Users)
        {
            Console.WriteLine(u);
        }
    }

    private static void DeleteUserCli()
    {
        ListUsers();
        Console.Write("Enter user ID to delete: ");
        if (int.TryParse(Console.ReadLine(), out var id))
        {
            Users.DeleteUser(id);
        }
    }
}
using System.Text.Json;

namespace BudgetTracker;

class Program
{
    private static List<Transaction> _transactions = new();
    private static List<User> _users = new();
    private static User? _loggedInUser;
    private static int _nextUserId = 1;
    private static Stack<List<Transaction>> _undoStack = new();
    private static Stack<List<Transaction>> _redoStack = new();
    private const string FilePath = "budget.json";
    private const string LimitsPath = "limits.json";
    private const string UsersPath = "users.json";

    private static readonly List<BudgetLimit> Limits = new()
    {
        new BudgetLimit { Category = "Groceries", Limit = 400 },
        new BudgetLimit { Category = "Rent", Limit = 900 },
        new BudgetLimit { Category = "Entertainment", Limit = 100 }
    };

    static void Main()
    {
        LoadFromFile();

        if (!File.Exists(LimitsPath))
        {
            CreateLimitsFile(LimitsPath);
        }

        var loadedLimits = LoadLimitsFile(LimitsPath);
        if (loadedLimits != null && loadedLimits.Any())
        {
            Limits.Clear();
            Limits.AddRange(loadedLimits);
        }

        while (true)
        {
            Console.WriteLine("\n--- Budget Tracker ---");
            Console.WriteLine("1. Login");
            Console.WriteLine("2. Logout");
            Console.WriteLine("3. Add Income");
            Console.WriteLine("4. Add Expense");
            Console.WriteLine("5. View Balance");
            Console.WriteLine("6. Edit Transaction");
            Console.WriteLine("7. Delete Transaction");
            Console.WriteLine("8. Undo Transaction");
            Console.WriteLine("9. Redo Transaction");
            Console.WriteLine("10. View Transaction History");
            Console.WriteLine("11. Show Category Summary");
            Console.WriteLine("12. Show Monthly Report");
            Console.WriteLine("13. Register User");
            Console.WriteLine("14. List Users");
            Console.WriteLine("15. Remove User");
            Console.WriteLine("16. Save & Exit");

            Console.Write("Choose: ");
            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1": LogInUser(); break;
                case "2": LogOutUser(); break;
                case "3": AddTransaction(isIncome: true); break;
                case "4": AddTransaction(isIncome: false); break;
                case "5": ShowBalance(); break;
                case "6": EditTransaction(); break;
                case "7": DeleteTransaction(); break;
                case "8": UndoTransaction(); break;
                case "9": RedoTransaction(); break;
                case "10": ShowHistory(); break;
                case "11": ShowCategorySummary(_transactions); break;
                case "12": ShowMonthlyReport(_transactions); break;
                case "13": RegisterUser(); break;
                case "14": ListUsers(); break;
                case "15": RemoveUser(); break;
                case "16":
                    SaveToFile();
                    Console.WriteLine($"Saved {_transactions.Count} transactions. Goodbye!");
                    return;
                default:
                    Console.WriteLine("Invalid choice!");
                    break;
            }
        }
    }

    private static void LoadFromFile()
    {
        if (File.Exists(FilePath))
        {
            var json = File.ReadAllText(FilePath);
            _transactions = JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();
            Console.WriteLine("Loaded existing transactions.");
        }
        Loadusers();
    }

    static void SaveToFile()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(FilePath, JsonSerializer.Serialize(_transactions, options));
        
        SaveUsers();
    }

    static void ShowBalance()
    {
        if (_loggedInUser == null)
        {
            Console.WriteLine("You must be logged in first.");
            return;
        }
        
        var total = _transactions.Sum(t => t.Amount);
        Console.WriteLine($"\nCurrent balance: ${total:0.00}");
    }

    private static void ShowHistory()
    {
        if (_loggedInUser == null)
        {
            Console.WriteLine("You must be logged in first.");
            return;
        }
        
        if (_transactions.Count == 0)
        {
            Console.WriteLine("No transactions found.");
            return;
        }

        Console.WriteLine("\nDate     | Category   | Amount   | Description");
        Console.WriteLine("-----------------------------------------------");

        foreach (var transaction in _transactions.OrderByDescending(t => t.Date))
        {
            Console.WriteLine(transaction);
        }
    }

    private static void AddTransaction(bool isIncome)
    {
        if (_loggedInUser == null)
        {
            Console.WriteLine("You must be logged in first.");
            return;
        }
        
        _undoStack.Push(CloneTransactions());
        _redoStack.Clear();
        
        Console.Write("Description: ");
        var description = Console.ReadLine();

        Console.Write("Category (e.g. Food, Salary, Rent): ");
        var category = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(category))
        {
            Console.WriteLine("Category and Description are required.");
            return;
        }

        Console.Write("Amount: ");
        if (decimal.TryParse(Console.ReadLine(), out var amount))
        {
            if (!isIncome) amount *= -1;

            _transactions.Add(new Transaction
            {
                Description = description,
                Category = category,
                Amount = amount
            });

            Console.WriteLine($"{(isIncome ? "Income" : "Expense")} recorded.");

            if (!isIncome)
            {
                WarnIfBudgetExceeded(_transactions, Limits);
            }
        }
        else
        {
            Console.WriteLine("Invalid amount.");
        }
    }
    
    private static void EditTransaction()
    {
        if (_loggedInUser == null)
        {
            Console.WriteLine("You must be logged in first.");
            return;
        }
        
        _undoStack.Push(CloneTransactions());
        _redoStack.Clear();
        
        if (_transactions.Count == 0)
        {
            Console.WriteLine("No transactions to edit");
        }

        Console.WriteLine("\n--- Edit Transaction ---");
        for (var i = 0; i < _transactions.Count; i++)
        {
            Console.WriteLine($"{i}:  {_transactions[i]}");
        }

        Console.Write("Enter the number of transaction you want to edit: ");
        if (int.TryParse(Console.ReadLine(), out var index) && index >= 0 && index < _transactions.Count)
        {
            var t = _transactions[index];

            Console.Write($"New description (leave blank to keep `{t.Description}`): ");
            var description = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(description))
                t.Description = description;
            
            Console.Write($"New category (leave blank to keep `{t.Category}`): ");
            var category = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(category))
                t.Category = category;
            
            Console.Write($"New amount (leave blank to keep `{t.Amount}`): ");
            var amount = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(amount) && decimal.TryParse(amount, out var newAmount))
                t.Amount = newAmount;

            SaveToFile();
            Console.WriteLine("Transaction updated and saved.");
        }
        else
        {
            Console.WriteLine("Invalid index.");
        }
    }
    
    private static void DeleteTransaction()
    {
        if (_loggedInUser == null)
        {
            Console.WriteLine("You must be logged in first.");
            return;
        }
        
        _undoStack.Push(CloneTransactions());
        _redoStack.Clear();
        
        if (_transactions.Count == 0)
        {
            Console.WriteLine("No transactions to delete");
        }

        Console.WriteLine("\n--- Delete Transaction ---");
        for (var i = 0; i < _transactions.Count; i++)
        {
            Console.WriteLine($"{i}:  {_transactions[i]}");
        }

        Console.Write("Enter the number of transaction you want to delete: ");
        if (int.TryParse(Console.ReadLine(), out var index) && index >= 0 && index < _transactions.Count)
        {
            Console.WriteLine($"Deleted: {_transactions[index]}");
            _transactions.RemoveAt(index);
            SaveToFile();
            Console.WriteLine("Changes saved.");
        }
        else
        {
            Console.WriteLine("Invalid index.");
        }
    }

    private static List<Transaction> CloneTransactions()
    {
        return _transactions
            .Select(t => new Transaction
            {
                Date = t.Date,
                Description = t.Description,
                Category = t.Category,
                Amount = t.Amount
            }).ToList();
    }
    
    private static void UndoTransaction()
    {
        if (_loggedInUser == null)
        {
            Console.WriteLine("You must be logged in first.");
            return;
        }
        
        if (_undoStack.Count > 0)
        {
            _redoStack.Push(CloneTransactions());
            _transactions = _undoStack.Pop();
            Console.WriteLine("Undo completed.");
        }
        else
        {
            Console.WriteLine("No transaction to undo.");
        }
        SaveToFile();
    }
    
    private static void RedoTransaction()
    {
        if (_loggedInUser == null)
        {
            Console.WriteLine("You must be logged in first.");
            return;
        }
        
        if (_redoStack.Count > 0)
        {
            _undoStack.Push(CloneTransactions());
            _transactions = _redoStack.Pop();
            Console.WriteLine("Redo completed.");
        }
        else
        {
            Console.WriteLine("No transaction to redo.");
        }
        SaveToFile();
    }

    private static void ShowCategorySummary(List<Transaction> transactions)
    {
        if (_loggedInUser == null)
        {
            Console.WriteLine("You must be logged in first.");
            return;
        }
        
        if (transactions.Count == 0)
        {
            Console.WriteLine("No transactions found.");
            return;
        }

        Console.WriteLine("\n--- Category Summary ---");

        var summary = transactions
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
            .OrderByDescending(s => Math.Abs(s.Total));

        foreach (var item in summary)
        {
            var sign = item.Total >= 0 ? "+" : "-";
            Console.WriteLine($"{item.Category?.PadRight(10)} | {sign}€{Math.Abs(item.Total):0.00}");
        }

        Console.WriteLine();
    }

    private static void ShowMonthlyReport(List<Transaction> transactions)
    {
        if (_loggedInUser == null)
        {
            Console.WriteLine("You must be logged in first.");
            return;
        }
        
        Console.Write("\nEnter month and year (MM-yyyy) or leave blank for current: ");
        var input = Console.ReadLine();

        DateTime targetDate;

        if (string.IsNullOrWhiteSpace(input))
        {
            targetDate = DateTime.Now;
        }
        else if (!DateTime.TryParseExact(input, "MM-yyyy", null, System.Globalization.DateTimeStyles.None, out targetDate))
        {
            Console.WriteLine("Invalid format. Please use MM-yyyy.\n");
            return;
        }

        var monthTransactions = transactions
            .Where(t => t.Date.Month == targetDate.Month && t.Date.Year == targetDate.Year)
            .ToList();

        if (!monthTransactions.Any())
        {
            Console.WriteLine("No transactions found for that month.\n");
            return;
        }

        var income = monthTransactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var expenses = monthTransactions.Where(t => t.Amount < 0).Sum(t => t.Amount);
        var net = income + expenses;

        Console.WriteLine($"\n--- {targetDate:MMMM yyyy} Report ---");
        Console.WriteLine($"Income:   {income,10:C}");
        Console.WriteLine($"Expenses: {expenses,10:C}");
        Console.WriteLine($"Net:      {net,10:C}\n");

        var topCategories = monthTransactions
            .Where(t => t.Amount < 0)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
            .OrderBy(s => s.Total)
            .Take(3);

        if (topCategories.Any())
        {
            Console.WriteLine("Top Expense Categories:");
            foreach (var category in topCategories)
            {
                Console.WriteLine($" {category.Category?.PadRight(10)}: €{Math.Abs(category.Total):0.00}");
            }
        }

        Console.WriteLine();
    }

    static void CreateLimitsFile(string filePath)
    {
        var defaultLimits = new List<BudgetLimit>
        {
            new BudgetLimit { Category = "Groceries", Limit = 400 },
            new BudgetLimit { Category = "Rent", Limit = 900 },
            new BudgetLimit { Category = "Entertainment", Limit = 100 }
        };

        var json = JsonSerializer.Serialize(defaultLimits, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
        Console.WriteLine($"Default limits file created at {filePath}");
    }

    private static List<BudgetLimit>? LoadLimitsFile(string filePath)
    {
        if (!File.Exists(filePath))
            return new List<BudgetLimit>();

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<BudgetLimit>>(json, new JsonSerializerOptions { WriteIndented = true });
    }

    static void WarnIfBudgetExceeded(List<Transaction> transactions, List<BudgetLimit> limits)
    {
        var currentMonth = DateTime.Now.Month;
        var currentYear = DateTime.Now.Year;

        var monthlyExpenses = transactions
            .Where(t => t.Date.Month == currentMonth && t.Date.Year == currentYear && t.Amount < 0)
            .GroupBy(t => t.Category)
            .Select(g => new
            {
                Category = g.Key,
                TotalSpent = Math.Abs(g.Sum(t => t.Amount))
            });

        foreach (var limit in limits)
        {
            var match = monthlyExpenses.FirstOrDefault(e => e.Category!.Equals(limit.Category, StringComparison.OrdinalIgnoreCase));
            if (match != null && match.TotalSpent > limit.Limit)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️  Warning: You exceeded your {limit.Category} budget! Limit: €{limit.Limit}, Spent: €{match.TotalSpent}");
                Console.ResetColor();
            }
        }
    }

    private static void RegisterUser()
    {
        if (_loggedInUser == null || !_loggedInUser.IsAdmin)
        {
            Console.WriteLine("Only admin users can perform this action.");
            return;
        }

        Console.Write("Name: ");
        var name = Console.ReadLine();
        Console.Write("Password: ");
        var password = Console.ReadLine();
        bool isAdmin = _users.Count == 0; // only the first user is the admin
        _users.Add(new User {Id = _nextUserId++, Name = name, Password = password, IsAdmin = isAdmin});
        Console.WriteLine("User registered successfully.");
        SaveToFile();
    }

    private static void RemoveUser()
    {
        if (_loggedInUser is not { IsAdmin: true })
        {
            Console.WriteLine("Only admin users can perform this action.");
            return;
        }

        ListUsers();
        Console.Write("Enter the ID of the user you wish to remove: ");
        int.TryParse(Console.ReadLine(), out int id);
        
        bool removed = _users.RemoveAll(user => user.Id == id) > 0;
        if (removed)
        {
            Console.WriteLine("The user was successfully removed.");
        }
        else
        {
            Console.WriteLine("Invalid ID. No matches found.");
        }
        SaveToFile();
    }

    private static void LogInUser()
    {
        Console.Write("Name: ");
        var name = Console.ReadLine();
        Console.Write("Password: ");
        var password = Console.ReadLine();

        bool found = false;
        foreach (User user in _users)
        {
            if (user.Name == name && user.Password == password)
            {
                _loggedInUser = user;
                Console.WriteLine("Logged in as: " + user.Name);
                found = true;
                break;
            }
        }

        if (!found)
        {
            Console.WriteLine("Invalid credentials.");
        }
    }

    private static void LogOutUser()
    {
        if (_loggedInUser != null)
        {
            Console.WriteLine("Logged out." + _loggedInUser.Name);
            _loggedInUser = null;
        }
        else
        {
            Console.WriteLine("No user is currently logged in.");
        }
    }
    
    private static void ListUsers()
    {
        if (_users.Count == 0)
        {
            Console.WriteLine("No users registered.");
        }
        else
        {
            foreach (User user in _users)
            {
                Console.WriteLine(user);
            }
        }
    }

    private static void SaveUsers()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(UsersPath, JsonSerializer.Serialize(_transactions, options));
        }
        catch (Exception e)
        {
            Console.WriteLine("Error saving users: " + e.Message);
        }
    }
    
    private static void Loadusers()
    {
        if (!File.Exists(UsersPath)) return;

        try
        {
            string json = File.ReadAllText(UsersPath);
            var loadedUsers = JsonSerializer.Deserialize<List<User>>(json);
            if (loadedUsers != null)
            {
                _users.AddRange(loadedUsers);
                _nextUserId = _users.Any() ? _users.Max(u => u.Id) + 1 : 1;
            }
        }
        catch (IOException e)
        {
            Console.WriteLine("Error loading users: " + e.Message);
        }
    }
}
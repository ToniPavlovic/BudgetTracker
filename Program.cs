using System.Text.Json;

namespace BudgetTracker;

class Program
{
    private static List<Transaction> _transactions = new();
    private static Stack<List<Transaction>> _undoStack = new();
    private static Stack<List<Transaction>> _redoStack = new();
    private static readonly string FilePath = "budget.json";
    private const string LimitsPath = "limits.json";

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
            Console.WriteLine("1. Add Income");
            Console.WriteLine("2. Add Expense");
            Console.WriteLine("3. View Balance");
            Console.WriteLine("4. Edit Transaction");
            Console.WriteLine("5. Delete Transaction");
            Console.WriteLine("6. Undo Transaction");
            Console.WriteLine("7. Redo Transaction");
            Console.WriteLine("8. View Transaction History");
            Console.WriteLine("9. Show Category Summary");
            Console.WriteLine("10. Show Monthly Report");
            Console.WriteLine("11. Save & Exit");

            Console.Write("Choose: ");
            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1": AddTransaction(isIncome: true); break;
                case "2": AddTransaction(isIncome: false); break;
                case "3": ShowBalance(); break;
                case "4": EditTransaction(); break;
                case "5": DeleteTransaction(); break;
                case "6": UndoTransaction(); break;
                case "7": RedoTransaction(); break;
                case "8": ShowHistory(); break;
                case "9": ShowCategorySummary(_transactions); break;
                case "10": ShowMonthlyReport(_transactions); break;
                case "11":
                    SaveToFile();
                    Console.WriteLine($"Saved {_transactions.Count} transactions. Goodbye!");
                    return;
                default:
                    Console.WriteLine("Invalid choice!");
                    break;
            }
        }
    }

    static void LoadFromFile()
    {
        if (File.Exists(FilePath))
        {
            var json = File.ReadAllText(FilePath);
            _transactions = JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();
            Console.WriteLine("Loaded existing transactions.");
        }
    }

    static void SaveToFile()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(FilePath, JsonSerializer.Serialize(_transactions, options));
    }

    static void ShowBalance()
    {
        var total = _transactions.Sum(t => t.Amount);
        Console.WriteLine($"\nCurrent balance: ${total:0.00}");
    }

    static void ShowHistory()
    {
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

    static void AddTransaction(bool isIncome)
    {
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
    
    static void EditTransaction()
    {
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
    
    static void DeleteTransaction()
    {
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

    static List<Transaction> CloneTransactions()
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
    
    static void UndoTransaction()
    {
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
    
    static void RedoTransaction()
    {
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

    static void ShowCategorySummary(List<Transaction> transactions)
    {
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

    static void ShowMonthlyReport(List<Transaction> transactions)
    {
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

    static List<BudgetLimit>? LoadLimitsFile(string filePath)
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
}

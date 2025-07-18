using System.Text.Json;

namespace BudgetTracker;

class Program
{
    private static List<Transaction> _transactions = new();
    private static readonly string FilePath = "budget.json";
    private const string LimitsPath = "limits.json";

    private static readonly List<BudgetLimit> _limits = new()
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

        var budgetLimits = LoadLimitsFile(LimitsPath);

        while (true)
        {
            Console.WriteLine("\n--- Budget Tracker ---");
            Console.WriteLine("1. Add Income");
            Console.WriteLine("2. Add Expense");
            Console.WriteLine("3. View Balance");
            Console.WriteLine("4. View Transaction History");
            Console.WriteLine("5. Show Category Summary");
            Console.WriteLine("6. Show Monthly Report");
            Console.WriteLine("7. Save & Exit");

            Console.Write("Choose: ");
            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1": AddTransaction(isIncome: true); break;
                case "2": AddTransaction(isIncome: false); break;
                case "3": ShowBalance(); break;
                case "4": ShowHistory(); break;
                case "5": ShowCategorySummary(_transactions); break;
                case "6": ShowMonthlyReport(_transactions); break;
                case "7":
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
            string json = File.ReadAllText(FilePath);
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
        decimal total = _transactions.Sum(t => t.Amount);
        Console.WriteLine($"\nCurrent balance: €{total:0.00}");
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
        Console.Write("Description: ");
        string? description = Console.ReadLine();

        Console.Write("Category (e.g. Food, Salary, Rent): ");
        string? category = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(category))
        {
            Console.WriteLine("Category and Description are required.");
            return;
        }

        Console.Write("Amount: ");
        if (decimal.TryParse(Console.ReadLine(), out decimal amount))
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
                WarnIfBudgetExceeded(_transactions, _limits);
            }
        }
        else
        {
            Console.WriteLine("Invalid amount.");
        }
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
            string sign = item.Total >= 0 ? "+" : "-";
            Console.WriteLine($"{item.Category?.PadRight(10)} | {sign}€{Math.Abs(item.Total):0.00}");
        }

        Console.WriteLine();
    }

    static void ShowMonthlyReport(List<Transaction> transactions)
    {
        Console.Write("\nEnter month and year (MM-yyyy) or leave blank for current: ");
        string? input = Console.ReadLine();

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

        decimal income = monthTransactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
        decimal expenses = monthTransactions.Where(t => t.Amount < 0).Sum(t => t.Amount);
        decimal net = income + expenses;

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

        string json = JsonSerializer.Serialize(defaultLimits, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
        Console.WriteLine($"Default limits file created at {filePath}");
    }

    static List<BudgetLimit>? LoadLimitsFile(string filePath)
    {
        if (!File.Exists(filePath))
            return new List<BudgetLimit>();

        string json = File.ReadAllText(filePath);
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

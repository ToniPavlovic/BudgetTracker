using System.Text.Json;

namespace BudgetTracker;

class Program
{
    static List<Transaction> _transactions = new();
    private static readonly string FilePath = "budget.json";
    static void Main()
    {
        LoadFromFile();

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
                case "1": AddTransaction(true); break;
                case "2": AddTransaction(false); break;
                case "3": ShowBalance(); break;
                case "4": ShowHistory(); break;
                case "5": ShowCategorySummary(_transactions); break;
                case "6": ShowMonthlyReport(_transactions); break;
                case "7": 
                    SaveToFile();
                    Console.WriteLine("Your data has been saved. Goodbye!");
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
            string json =  File.ReadAllText(FilePath);
            _transactions = JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();
            Console.WriteLine("Loaded existing transactions.");
        }
    }

    static void SaveToFile()
    {
        var options =  new JsonSerializerOptions { WriteIndented = true};
        File.WriteAllText(FilePath, JsonSerializer.Serialize(_transactions, options));
    }

    static void ShowBalance()
    {
        decimal total = 0;
        foreach (var transaction in _transactions)
        {
            total += transaction.Amount;
        }

        Console.WriteLine($"\nCurrent balance: ${total:F2}");
    }

    static void ShowHistory()
    {
        if (_transactions.Count == 0)
        {
            Console.WriteLine("No transactions found.");
            return;
        }

        Console.WriteLine("\nDate     | Category | Amount | Description");
        Console.WriteLine("---------------------------------------------");
        foreach (var transaction in _transactions)
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

        Console.Write("Amount: ");
        if (decimal.TryParse(Console.ReadLine(), out decimal amount))
        {
            if (!isIncome) amount *= -1;
            {
                _transactions.Add(new Transaction
                {
                    Description = description,
                    Category = category,
                    Amount = amount
                });
                Console.WriteLine($"{(isIncome ? "Income" : "Expense")} recorded.");
            }
        }
        else
        { 
            Console.WriteLine("Invalid amount");
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
            .GroupBy(transaction => transaction.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(transaction => transaction.Amount) })
            .OrderByDescending(s => Math.Abs(s.Total));

        foreach (var item in summary)
        {
            string sign = item.Total >= 0 ? "+" : "-";
            Console.WriteLine($"{item.Category?.PadRight(10)} | {sign}${Math.Abs(item.Total):0.00}");
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
        else if (DateTime.TryParseExact(input, "MM-yyyy", null, System.Globalization.DateTimeStyles.None, out targetDate))
        {
            Console.WriteLine("Invalid format. Please use MM-yyyy.\n");
        }
        
        var monthTransactions = transactions
            .Where(transaction => transaction.Date.Month == targetDate.Month 
                                  && transaction.Date.Year == targetDate.Year).ToList();

        if (!monthTransactions.Any())
        {
            Console.WriteLine("No transactions found for that month.\n");
        }
        
        decimal income = monthTransactions.Where(transaction => transaction.Amount > 0).Sum(transaction => transaction.Amount);
        decimal expenses = monthTransactions.Where(transaction => transaction.Amount < 0).Sum(transaction => transaction.Amount);
        decimal net = income + expenses;

        Console.WriteLine($"\n--- {targetDate:MMMM yyyy} Report ---");
        Console.WriteLine($"Income: {income,10:C}");
        Console.WriteLine($"Expenses: {expenses,10:C}");
        Console.WriteLine($"Net: {net,10:C}\n");
        
        var topCategories = monthTransactions
            .Where(transaction => transaction.Amount < 0)
            .GroupBy(transaction => transaction.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(transaction => transaction.Amount) })
            .OrderByDescending(s => s.Total)
            .Take(3);

        var categories = topCategories.ToList();
        if (categories.Any())
        {
            Console.WriteLine("Top Expense Categories: ");
            foreach (var category in categories)
            {
                Console.WriteLine($" { category.Category?.PadRight(10)}: €{category.Total:0.00}");
            }
        }
        
        Console.WriteLine();
    }
}
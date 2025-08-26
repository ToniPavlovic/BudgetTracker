using BudgetTracker.Models;
using BudgetTracker.Storage;

namespace BudgetTracker.Services;

public class BudgetService
{
    private readonly IStorageProvider<BudgetLimit> _storage;
    private readonly string _file =  "limits.json";
    private readonly List<BudgetLimit> _limits = new();
    
    public IReadOnlyList<BudgetLimit> Limits => _limits;
    
    public BudgetService(IStorageProvider<BudgetLimit> storage)
    {
        _storage = storage;
        Load();
    }

    public void Load()
    {
        _limits.Clear();
        var loaded = _storage.Load(_file);
        if (loaded.Count == 0)
        {
            loaded = new List<BudgetLimit>
            {
                new() { Category = "Groceries", Limit = 400 },
                new() { Category = "Rent", Limit = 900 },
                new() { Category = "Entertainment", Limit = 100 }
            };
            _storage.Save(_file, loaded);
        }
        _limits.AddRange(loaded);
    }
    
    public void  Save() => _storage.Save(_file, _limits);

    public void SetLimits(List<BudgetLimit>? limits)
    {
        _limits.Clear();
        if (limits != null) _limits.AddRange(limits);
        Save();
    }
    
    public IEnumerable<string> CheckExceeded(IEnumerable<Transaction> transactions)
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

        foreach (var limit in _limits)
        {
            var match = monthlyExpenses.FirstOrDefault(e => e.Category!.Equals(limit.Category, StringComparison.OrdinalIgnoreCase));
            if (match != null && match.TotalSpent > limit.Limit)
            {
                yield return $"⚠️  You exceeded your {limit.Category} budget! Limit: €{limit.Limit}, Spent: €{match.TotalSpent}";
            }
        }
    }
}
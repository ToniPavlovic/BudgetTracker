namespace BudgetTracker;

public class Transaction
{
    internal DateTime Date { get; set; } =  DateTime.Now;
    internal string? Description { get; set; }
    internal string? Category { get; set; }
    internal decimal Amount { get; set; }

    public override string ToString()
    {
        var type = Amount >= 0 ? "+" : "-";
        return $"{Date.ToShortDateString()} | {Category,-10} | {type}${Math.Abs(Amount),7} | {Description}";
    }
}
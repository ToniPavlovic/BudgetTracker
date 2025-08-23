namespace BudgetTracker.Models;

public class Transaction
{
    public DateTime Date { get; set; } =  DateTime.Now;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal Amount { get; set; }

    public override string ToString()
    {
        var type = Amount >= 0 ? "+" : "-";
        return $"{Date.ToShortDateString()} | {Category,-10} | {type}${Math.Abs(Amount),7} | {Description}";
    }
}
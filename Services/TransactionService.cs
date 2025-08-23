using BudgetTracker.Models;

namespace BudgetTracker.Services;

public class TransactionService
{
    private readonly List<Transaction> _transactions = new();
    private readonly Stack<List<Transaction>> _undoStack = new();
    private readonly Stack<List<Transaction>> _redoStack = new();
    
    public IReadOnlyList<Transaction> Transactions => _transactions;

    public void AddTransaction(Transaction transaction)
    {
        _undoStack.Push(CloneTransactions());
        _redoStack.Clear();
        _transactions.Add(transaction);
        
    }
    
    public void EditTransaction(int index, Transaction updated)
    {
        if (index < 0 || index >= _transactions.Count) return;
        _undoStack.Push(CloneTransactions());
        _redoStack.Clear();
        _transactions[index] = updated;
        
    }

    public void DeleteTransaction(int index)
    {
        if (index < 0 || index >= _transactions.Count) return;
        _undoStack.Push(CloneTransactions());
        _redoStack.Clear();
        _transactions.RemoveAt(index);
        
    }

    public void UndoTransaction()
    {
        if (_undoStack.Count == 0) return;
        _redoStack.Push(CloneTransactions());
        _transactions.Clear();
        _transactions.AddRange(_undoStack.Pop());
    }
    
    public void RedoTransaction()
    {
        if (_redoStack.Count == 0) return;
        _undoStack.Push(CloneTransactions());
        _transactions.Clear();
        _transactions.AddRange(_redoStack.Pop());
    }
    
    private List<Transaction> CloneTransactions()
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
    
    public decimal GetBalance() => _transactions.Sum(t => t.Amount);
}
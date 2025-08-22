namespace BudgetTracker;

public class User(int id, string? name, string? password, bool isAdmin)
{
    internal  int Id { get;  } = id;
    internal string? Name { get;  } = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Name cannot be empty");
    internal string? Password { get; } = !string.IsNullOrWhiteSpace(password) ? password : throw new ArgumentException("Password cannot be empty");
    internal bool IsAdmin { get; } = isAdmin;

    public override string ToString()
    {
        return "[" + Id + "] " + Name;
    }
}
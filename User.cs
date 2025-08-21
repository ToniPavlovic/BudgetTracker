namespace BudgetTracker;

public class User
{
    public int Id { get;  }
    public string? Name { get;  }
    public string? Password { get; }
    public bool IsAdmin { get; }

    public User(int id, string? name, string? password, bool isAdmin)
    {
        Id = id;
        Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Name cannot be empty");
        Password = !string.IsNullOrWhiteSpace(password) ? password : throw new ArgumentException("Password cannot be empty");
        IsAdmin = isAdmin;
    }

    public override string ToString()
    {
        return "[" + Id + "] " + Name;
    }
}
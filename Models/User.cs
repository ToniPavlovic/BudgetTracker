namespace BudgetTracker.Models;

public class User
{
    private int Id { get; set; }
    public string? Name { get; set; }
    public string? Password { get; set; }

    public User() { }
    
    public User(int id, string? name, string? password)
    {
        Id = id;
        Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Name cannot be empty");
        Password = !string.IsNullOrWhiteSpace(password) ? password : throw new ArgumentException("Password cannot be empty");
    }
    public override string ToString()
    {
        return "[" + Id + "] " + Name;
    }
}
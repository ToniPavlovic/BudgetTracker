namespace BudgetTracker.Models;

public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Password { get; set; }
    public string? Role { get; set; }

    public User() { }
    
    public User(int id, string? name, string? password, string role)
    {
        Id = id;
        Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Name cannot be empty");
        Password = !string.IsNullOrWhiteSpace(password) ? password : throw new ArgumentException("Password cannot be empty");
        Role  = !string.IsNullOrWhiteSpace(role) ? role : throw new ArgumentException("Role cannot be empty");
    }
    public override string ToString()
    {
        return "[" + Id + "] " + Name;
    }
}
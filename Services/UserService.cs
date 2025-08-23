using BudgetTracker.Models;

namespace BudgetTracker.Services;

public class UserService
{
    private readonly List<User> _users = new();
    private int _nextUserId = 1;
    public User? LoggedInUser { get; private set; }

    public IReadOnlyList<User> Users => _users;

    public void RegisterUser(string name, string password)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("Name and password cannot be empty.");
            return;
        }
        
        _users.Add(new User(_nextUserId++, name, password));
        Console.WriteLine($"User '{name}' registered successfully!");
    }

    public bool Login(string name, string password)
    {
        var user = _users.FirstOrDefault(u => u.Name == name && u.Password == password);
        if (user != null) LoggedInUser = user;
        return user != null;
    }

    public void Logout()
    {
        if (LoggedInUser != null)
        {
            Console.WriteLine($"Logged out: {LoggedInUser.Name}");
            LoggedInUser = null;
        }
        else
        {
            Console.WriteLine("No user is currently logged in.");
        }
    }
}
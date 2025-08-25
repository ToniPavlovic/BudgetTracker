using BudgetTracker.Models;
using BCrypt.Net;

namespace BudgetTracker.Services;

public class UserService
{
    private readonly List<User> _users = new();
    private int _nextUserId = 1;
    public User? LoggedInUser { get; private set; }

    public IReadOnlyList<User> Users => _users;

    public void RegisterUser(string name, string password, string role)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("Name and password cannot be empty.");
            return;
        }
        
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        
        _users.Add(new User(_nextUserId++, name, passwordHash, role));
        Console.WriteLine($"User '{name}' registered successfully!");
    }

    public void DeleteUser(int userId)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            Console.WriteLine("User not found.");
            return;
        }
        
        _users.Remove(user);
        Console.WriteLine($"User '{user.Name}' deleted successfully!");
    }

    public bool Login(string name, string password)
    {
        var user = _users.FirstOrDefault(u => u.Name == name);
        if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            LoggedInUser = user;
            return true;
        }

        return false;
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

    public void LoadUsers(List<User> users)
    {
        foreach (var u in users)
        {
            if (u.Id >= _nextUserId)
            {
                _nextUserId = u.Id + 1;
            }
            _users.Add(u);
        }
    }
}
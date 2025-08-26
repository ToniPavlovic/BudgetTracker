using System.Text.Json;

namespace BudgetTracker.Storage;

public class JsonStorageProvider<T> : IStorageProvider<T>
{
    public List<T> Load(string fileName)
    {
        if (!File.Exists(fileName)) return new List<T>();
        var json = File.ReadAllText(fileName);
        return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
    }

    public void Save(string FileName, List<T> items)
    {
        var json = JsonSerializer.Serialize(items,  new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FileName, json);
    }
}
namespace BudgetTracker.Storage;

public interface IStorageProvider<T>
{
    List<T> Load(string fileName);
    void Save(string fileName, List<T> items);
}
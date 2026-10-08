namespace MetroREX.Data;

internal interface IStore<T>
{
    List<T> Load();

    void Save(IEnumerable<T> items);
}

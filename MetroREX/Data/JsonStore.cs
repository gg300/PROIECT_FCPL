using System.Text.Json;
using System.Text.Json.Serialization;

namespace MetroREX.Data;

internal sealed class JsonStore<T> : IStore<T>
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;
    private readonly object _gate = new();

    public JsonStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
    }

    public List<T> Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_path) || new FileInfo(_path).Length == 0)
            {
                return new List<T>();
            }

            try
            {
                using var stream = File.Open(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return JsonSerializer.Deserialize<List<T>>(stream, Options) ?? new List<T>();
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException($"Fișierul '{_path}' nu conține date valide.", exception);
            }
        }
    }

    public void Save(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var snapshot = items.ToList();

        lock (_gate)
        {
            AtomicFile.Write(_path, stream => JsonSerializer.Serialize(stream, snapshot, Options));
        }
    }
}

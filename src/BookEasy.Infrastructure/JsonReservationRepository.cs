using System.Text.Json;
using BookEasy.Core;

namespace BookEasy.Infrastructure;

public sealed class JsonReservationRepository : IReservationRepository
{
    private readonly string _path;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public JsonReservationRepository(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    }

    public IReadOnlyList<Reservation> GetAll()
    {
        if (!File.Exists(_path)) return new List<Reservation>();
        return JsonSerializer.Deserialize<List<Reservation>>(File.ReadAllText(_path), Options) ?? new List<Reservation>();
    }

    public void Save(IReadOnlyList<Reservation> reservations)
    {
        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(reservations, Options));
        File.Move(temp, _path, overwrite: true);
    }
}

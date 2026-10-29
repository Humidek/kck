using BookEasy.Core;

namespace BookEasy.Application;

public sealed class BookingService
{
    private readonly IReservationRepository _repository;
    public static readonly TimeOnly Opening = new(9, 0);
    public static readonly TimeOnly Closing = new(17, 0);

    public IReadOnlyList<ServiceDefinition> Services { get; } = new List<ServiceDefinition>
    {
        new(1, "Strzyżenie damskie", 60, 90),
        new(2, "Strzyżenie męskie", 30, 50),
        new(3, "Modelowanie", 60, 70)
    };

    public BookingService(IReservationRepository repository) => _repository = repository;

    public IReadOnlyList<Reservation> GetReservations() => _repository.GetAll().OrderBy(r => r.Start).ToList();

    public ServiceDefinition GetService(int id) => Services.First(s => s.Id == id);

    public IReadOnlyList<TimeOnly> GetAvailableSlots(DateOnly day, int serviceId)
    {
        var service = GetService(serviceId);
        var result = new List<TimeOnly>();
        var date = day.ToDateTime(Opening);
        var lastEnd = day.ToDateTime(Closing);
        while (date.AddMinutes(service.DurationMinutes) <= lastEnd)
        {
            if (date > DateTime.Now && IsAvailable(date, service.DurationMinutes))
                result.Add(TimeOnly.FromDateTime(date));
            date = date.AddMinutes(30);
        }
        return result;
    }

    public bool IsAvailable(DateTime start, int durationMinutes)
    {
        var end = start.AddMinutes(durationMinutes);
        return !_repository.GetAll().Any(r =>
        {
            var reservedService = GetService(r.ServiceId);
            return start < r.Start.AddMinutes(reservedService.DurationMinutes) && r.Start < end;
        });
    }

    public Reservation Create(string name, string phone, int serviceId, DateTime start)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Podaj imię i nazwisko klienta.");
        if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Podaj numer telefonu.");
        if (!Services.Any(s => s.Id == serviceId)) throw new ArgumentException("Nieznana usługa.");
        var duration = GetService(serviceId).DurationMinutes;
        if (start <= DateTime.Now) throw new ArgumentException("Termin musi być w przyszłości.");
        if (TimeOnly.FromDateTime(start) < Opening || TimeOnly.FromDateTime(start.AddMinutes(duration)) > Closing)
            throw new ArgumentException("Wizyta wykracza poza godziny pracy.");
        if (!IsAvailable(start, duration)) throw new ArgumentException("Termin jest już zajęty.");
        var booking = new Reservation(Guid.NewGuid(), name.Trim(), phone.Trim(), serviceId, start);
        var all = _repository.GetAll().ToList();
        all.Add(booking);
        _repository.Save(all);
        return booking;
    }

    public bool Cancel(Guid id)
    {
        var all = _repository.GetAll().ToList();
        int deleted = all.RemoveAll(r => r.Id == id);
        if (deleted == 0) return false;
        _repository.Save(all);
        return true;
    }
}

namespace BookEasy.Core;
public interface IReservationRepository
{
    IReadOnlyList<Reservation> GetAll();
    void Save(IReadOnlyList<Reservation> reservations);
}

namespace BookEasy.Core;
public sealed record Reservation(Guid Id, string ClientName, string Phone, int ServiceId, DateTime Start);

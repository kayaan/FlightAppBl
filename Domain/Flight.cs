namespace FlightApp.Domain;

public record Flight(
    Guid Id,
    DateOnly Date,
    TimeSpan Duration,
    Pilot Pilot,
    Glider Glider
);
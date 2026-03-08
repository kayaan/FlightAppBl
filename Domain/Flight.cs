using FlightApp.Analysis;

namespace FlightApp.Domain;

public class Flight
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public DateOnly? Date { get; set; }

    public string? PilotId { get; set; }

    public string? GliderId { get; set; }

    public FlightStats Stats { get; set; } = new();

    public string? FileHash { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
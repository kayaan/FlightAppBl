using FlightApp.Domain;
using FlightApp.ViewModels;

namespace FlightApp.Services;

public class FlightService
{
    private readonly Pilot _pilot = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "Aydin Kaya"
    );

    private readonly Glider _glider1 = new(
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        "Ozone Delta 4"
    );

    private readonly Glider _glider2 = new(
        Guid.Parse("33333333-3333-3333-3333-333333333333"),
        "Ozone Alpina"
    );

    private readonly Glider _glider3 = new(
        Guid.Parse("44444444-4444-4444-4444-444444444444"),
        "Advance Omega"
    );

    public List<Flight> GetDomainFlights()
    {
        return new List<Flight>
        {
            new Flight(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                new DateOnly(2024, 4, 28),
                new TimeSpan(2, 15, 0),
                _pilot,
                _glider1
            ),
            new Flight(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                new DateOnly(2024, 4, 30),
                new TimeSpan(1, 42, 0),
                _pilot,
                _glider2
            ),
            new Flight(
                Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                new DateOnly(2024, 5, 2),
                new TimeSpan(3, 8, 0),
                _pilot,
                _glider3
            )
        };
    }

    public List<FlightListItemVm> GetFlights()
    {
        var flights =  GetDomainFlights().Select(f => new FlightListItemVm(
            f.Id, 
            f.Date.ToString(),
            f.Duration.ToString(@"hh\:mm"), f.Glider.Name)
        ).ToList();

        return flights;
    }

    public FlightListItemVm? GetFlight(Guid id)
    {
        return GetFlights().FirstOrDefault(f => f.Id == id);
    }
}
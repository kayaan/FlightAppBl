namespace FlightApp.Services;

public static class AppStorageKeys
{
    public static readonly StorageKey<FlightsTableState> FlightsTableState =
        new("flights.table.state");
}
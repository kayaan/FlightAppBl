namespace FlightApp.Services;

public enum ToastType
{
    Success,
    Info,
    Error
}

public class ToastMessage
{
    public string Text { get; set; } = "";
    public ToastType Type { get; set; }
}
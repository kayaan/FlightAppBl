namespace FlightApp.Services;

public class ToastService
{
    public event Action<ToastMessage>? OnShow;

    public void Show(string text, ToastType type)
    {
        OnShow?.Invoke(new ToastMessage
        {
            Text = text,
            Type = type
        });
    }
}
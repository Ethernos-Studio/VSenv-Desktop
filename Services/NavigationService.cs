using System.Windows.Controls;

namespace VSenvDesktop.Services;

public static class NavigationService
{
    public static Frame? Frame { get; set; }

    public static void Navigate<T>() where T : Page, new()
    {
        Frame?.Navigate(new T());
    }
}
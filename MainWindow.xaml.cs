// 文件头
using System.Windows;
using iNKORE.UI.WPF.Modern.Controls;   // 0.10.1 的 NavigationView 等

namespace VSenvDesktop;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private void Window_Loaded(object sender, RoutedEventArgs e) =>
        NavView.SelectedItem = NavHome;

    private void NavView_SelectionChanged(NavigationView sender,
                                          NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            if (item == NavHome) RootFrame.Navigate(new Pages.HomePage());
            if (item == NavInstances) RootFrame.Navigate(new Pages.InstancesPage());
            if (item == NavAbout) RootFrame.Navigate(new Pages.AboutPage());
        }
    }
}
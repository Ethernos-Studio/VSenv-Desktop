using System.Text;
using System.Windows;
using VSenvDesktop.Helpers;

namespace VSenvDesktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // ① 先同步一次
        ThemeHelper.SyncToSystem();
        // ② 再监听实时切换
        ThemeHelper.WatchSystemTheme();

        base.OnStartup(e);
    }
}
using System.Text;
using System.Windows;

namespace VSenvDesktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // 必须放在任何 GBK 解码之前
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        base.OnStartup(e);
    }
}
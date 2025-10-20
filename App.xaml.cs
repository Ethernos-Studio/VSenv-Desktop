using System.Text;
using System.Windows;
using System.Threading.Tasks;
using VSenvDesktop.Helpers;

namespace VSenvDesktop;

public partial class App : Application
{
    private SplashScreen? _splashScreen;

    protected override async void OnStartup(StartupEventArgs e)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // 显示启动屏幕
        _splashScreen = new SplashScreen();
        _splashScreen.Show();

        // 初始化主题
        _splashScreen.UpdateStatus("正在初始化主题...");
        ThemeHelper.SyncToSystem();
        ThemeHelper.WatchSystemTheme();

        // 模拟加载过程
        await SimulateLoading();

        // 创建并显示主窗口
        _splashScreen.UpdateStatus("正在加载主界面...");
        var mainWindow = new MainWindow();
        
        // 确保主窗口准备好
        await Task.Delay(500);
        
        // 关闭启动屏幕并显示主窗口
        _splashScreen.CloseSplash();
        mainWindow.Show();

        base.OnStartup(e);
    }

    private async Task SimulateLoading()
    {
        // 模拟不同的加载阶段
        await Task.Delay(1000);
        _splashScreen?.UpdateStatus("正在加载配置...");
        
        await Task.Delay(800);
        _splashScreen?.UpdateStatus("正在初始化服务...");
        
        await Task.Delay(600);
        _splashScreen?.UpdateStatus("正在准备界面...");
        
        await Task.Delay(400);
    }
}
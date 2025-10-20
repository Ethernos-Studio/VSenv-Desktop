using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace VSenvDesktop;

public partial class SplashScreen : Window
{
    private DispatcherTimer? _timer;
    private int _progress = 0;
    private readonly string[] _statusMessages = 
    {
        "正在初始化应用...",
        "正在加载配置...",
        "正在初始化主题...",
        "正在准备界面...",
        "正在加载服务...",
        "即将完成..."
    };

    public SplashScreen()
    {
        InitializeComponent();
        StartLoadingSimulation();
    }

    private void StartLoadingSimulation()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(800)
        };

        _timer.Tick += (sender, e) =>
        {
            if (_progress < _statusMessages.Length - 1)
            {
                _progress++;
                StatusText.Text = _statusMessages[_progress];
            }
        };

        _timer.Start();
    }

    public void UpdateStatus(string message)
    {
        Dispatcher.BeginInvoke(() =>
        {
            StatusText.Text = message;
        });
    }

    public void CloseSplash()
    {
        Dispatcher.BeginInvoke(() =>
        {
            _timer?.Stop();
            
            // 添加淡出动画
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            fadeOut.Completed += (sender, e) => Close();
            BeginAnimation(UIElement.OpacityProperty, fadeOut);
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _timer?.Stop();
    }
}
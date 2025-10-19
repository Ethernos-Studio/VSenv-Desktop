using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using iNKORE.UI.WPF.Modern.Controls;   // 0.10.1 的 Page
using Page = iNKORE.UI.WPF.Modern.Controls.Page;   // 明确用 Modern.Page
using MessageBox = System.Windows.MessageBox;      // 明确用 WPF MessageBox

namespace VSenvDesktop.Pages;

public partial class HomePage : Page
{
    private static readonly string VsEnvExe = FindVsEnv(); // 移入本类
    private static Encoding Gbk => Encoding.GetEncoding(936);
    private record InstanceItem(string Name, string Path, bool IsValid);

    public HomePage()
    {
        InitializeComponent();
        Loaded += async (_, __) => await LoadInstances();
    }

    #region 公共静态方法（原 InstancesPage.FindVsEnv 副本）
    private static string FindVsEnv()
    {
        var dir = Path.GetDirectoryName(Environment.ProcessPath)!;
        var local = Path.Combine(dir, "vsenv.exe");
        if (File.Exists(local)) return local;
        return "vsenv.exe"; // 依赖 PATH
    }
    #endregion

    #region 加载实例
    private async Task LoadInstances()
    {
        CmbInstances.Items.Clear();

        // 1. 取原始 list
        var txt = await Task.Run(() =>
        {
            var psi = new ProcessStartInfo(VsEnvExe, "list --quiet")
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                StandardOutputEncoding = Gbk
            };
            using var ps = Process.Start(psi)!;
            ps.WaitForExit();
            return ps.StandardOutput.ReadToEnd();
        });

        // 2. 解析
        using var reader = new StringReader(txt);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.StartsWith("Instance Name") ||
                line.StartsWith("---") ||
                string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

            var name = parts[0];
            var path = parts[1];
            var isBad = path.Contains("(无效实例)");

            // 3. 只存两样：显示文本 + 是否有效
            CmbInstances.Items.Add(new
            {
                Text = isBad ? $"{name} (无效实例)" : name,
                IsValid = !isBad,
                Name = name
            });
        }

        if (CmbInstances.Items.Count > 0)
            CmbInstances.SelectedIndex = 0;
        else
            BtnLaunch.IsEnabled = false;
    }

    // 1. 图标按钮点击：开关 Popup
    private void BtnSettings_Click(object sender, RoutedEventArgs e)
        => OptionsPopup.IsOpen = !OptionsPopup.IsOpen;

    // 2. 构造启动参数的小工具
    private string BuildExtraArgs()
    {
        var sb = new StringBuilder();
        if (ChkSandbox.IsChecked == true) sb.Append(" --sandbox");
        if (ChkRandomHost.IsChecked == true) sb.Append(" --host");
        if (ChkRandomMac.IsChecked == true) sb.Append(" --mac");
        if (ChkFakeHw.IsChecked == true) sb.Append(" --fake-hw");
        if (!string.IsNullOrWhiteSpace(TxtProxy.Text))
            sb.Append($" --proxy \"{TxtProxy.Text.Trim()}\"");
        return sb.ToString();
    }

    private void CmbInstances_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbInstances.SelectedItem is { } item &&
            item.GetType().GetProperty("IsValid")?.GetValue(item) is bool isValid &&
            item.GetType().GetProperty("Name")?.GetValue(item) is string name)
        {
            BtnLaunch.Content = $"启动 {name}";
            BtnLaunch.IsEnabled = isValid;
            TxtInvalidTip.Visibility = isValid ? Visibility.Collapsed : Visibility.Visible;
        }
        else
        {
            BtnLaunch.Content = "启动 %s";
            BtnLaunch.IsEnabled = false;
            TxtInvalidTip.Visibility = Visibility.Collapsed;
        }
    }
    #endregion

    #region 一键启动
    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        if (CmbInstances.SelectedItem is not { } item ||
            item.GetType().GetProperty("Name")?.GetValue(item) is not string instance)
            return;

        if (item.GetType().GetProperty("IsValid")?.GetValue(item) is false)
        {
            MessageBox.Show($"实例 {instance} 无效，无法启动。", "提示",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 1. 在 UI 线程把参数读好
        string extra = BuildExtraArgs();

        // 2. 再进后台线程
        BtnLaunch.IsEnabled = false;
        var exitCode = await Task.Run(() =>
        {
            var psi = new ProcessStartInfo(VsEnvExe, $"start {instance}{extra} --quiet")
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                StandardOutputEncoding = Gbk,
                StandardErrorEncoding = Gbk
            };
            using var ps = Process.Start(psi)!;
            ps.WaitForExit();
            var output = ps.StandardOutput.ReadToEnd() + ps.StandardError.ReadToEnd();
            return (ps.ExitCode, output);
        });

        // 3. 回到 UI 线程弹提示
        BtnLaunch.IsEnabled = true;
        if (exitCode.ExitCode == 0)
            MessageBox.Show($"实例 {instance} 已启动！", "完成",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        else if (!string.IsNullOrWhiteSpace(exitCode.output))
            MessageBox.Show(exitCode.output, "启动失败",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
    }
    #endregion
}
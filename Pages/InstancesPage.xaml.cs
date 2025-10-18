using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;
using MessageBox = System.Windows.MessageBox;   // 用 WPF 原生 MessageBox

namespace VSenvDesktop.Pages;

public partial class InstancesPage : Page
{
    private static readonly string VsEnvExe = FindVsEnv();
    private static Encoding Gbk => Encoding.GetEncoding(936);
    private const string QuietFlag = " --quiet";
    private const string NotFoundHint = """
【VSenvDesktop 提示】
出现该错误表示实例目录下缺少 Code.exe，可按以下步骤修复：

1. 将官方 VS Code 离线包（zip）解压到：
   {0}\vscode\
   解压后应存在：
   {0}\vscode\Code.exe

2. 或把已安装的 Code.exe 整个目录复制到上述路径。

3. 重新点击【刷新】按钮即可。
""";

    public InstancesPage()
    {
        InitializeComponent();
        Loaded += (_, __) => _ = RefreshInstances();
    }

    #region 原业务代码
    private string GetInstancePath()
    {
        var inst = GetSelectedInstance();
        return inst?.Path ?? "<实例路径>";
    }

    public class InstanceInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsValid { get; set; }
    }

    private InstanceInfo? GetSelectedInstance()
    {
        if (CmbInstances.SelectedItem is ComboBoxItem item && item.Tag is InstanceInfo info)
            return info;
        return null;
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await RefreshInstances();

    private async void BtnCreate_Click(object sender, RoutedEventArgs e)
    {
        string? name = Microsoft.VisualBasic.Interaction.InputBox("新实例名称:", "创建", "myenv");
        if (string.IsNullOrWhiteSpace(name)) return;
        await Run($"create {name}");
    }
    private async void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        var inst = GetSelectedInstance();
        if (inst == null) return;
        if (!inst.IsValid)
        {
            MessageBox.Show($"实例 {inst.Name} 无效，无法启动。请检查路径是否存在 Code.exe。", "提示");
            return;
        }
        var opts = BuildStartOptions();
        await Run($"start {inst.Name} {opts}");
    }

    private async void BtnRemove_Click(object sender, RoutedEventArgs e)
    {
        var inst = GetSelectedInstance();
        if (inst == null) return;
        if (MessageBox.Show($"确定删除 {inst.Name} ?", "确认", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            return;
        await Run($"remove {inst.Name}");
        await RefreshInstances();
    }

    private async void BtnListExt_Click(object sender, RoutedEventArgs e)
    {
        var inst = GetSelectedInstance();
        if (inst == null) return;
        await Run($"extension {inst.Name} list");
    }

    private static string FindVsEnv()
    {
        var dir = Path.GetDirectoryName(Environment.ProcessPath)!;
        var local = Path.Combine(dir, "vsenv.exe");
        if (File.Exists(local)) return local;
        return "vsenv.exe";
    }

    private async Task RefreshInstances()
    {
        CmbInstances.Items.Clear();
        var txt = await RunRaw("list");
        using var reader = new StringReader(txt);
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            line = line.Trim();
            if (line.StartsWith("Instance Name") || line.StartsWith("---") || string.IsNullOrWhiteSpace(line))
                continue;
            var parts = line.Split('\t', StringSplitOptions.TrimEntries);
            if (parts.Length < 2) continue;
            var name = parts[0];
            var path = parts[1];
            bool isInvalid = path.Contains("(无效实例)");
            var cleanPath = path.Replace(" (无效实例)", "").Trim();
            var displayName = isInvalid ? $"{name} (无效实例)" : name;
            var item = new ComboBoxItem
            {
                Content = displayName,
                Tag = new InstanceInfo { Name = name, Path = cleanPath, IsValid = !isInvalid }
            };
            CmbInstances.Items.Add(item);
        }
        if (CmbInstances.Items.Count > 0) CmbInstances.SelectedIndex = 0;
    }

    private string BuildStartOptions()
    {
        var sb = new StringBuilder();
        if (ChkSandbox.IsChecked == true) sb.Append(" --sandbox");
        if (ChkFakeHW.IsChecked == true) sb.Append(" --fake-hw");
        if (ChkHost.IsChecked == true) sb.Append(" --host");
        if (ChkMac.IsChecked == true) sb.Append(" --mac");
        return sb.ToString();
    }

    private async Task Run(string args)
    {
        TxtOutput.Clear();
        var raw = await RunRaw(args);
        string display = raw;
        if (args.Contains("extension", StringComparison.OrdinalIgnoreCase) &&
            args.Contains("list", StringComparison.OrdinalIgnoreCase))
            display = FormatExtensions(raw);
        else if (raw.Contains("Code.exe not found", StringComparison.OrdinalIgnoreCase))
            display = raw + Environment.NewLine + Environment.NewLine +
                      string.Format(NotFoundHint, GetInstancePath());
        else if (raw.Contains("FATAL ERROR", StringComparison.OrdinalIgnoreCase))
            display += Environment.NewLine + Environment.NewLine +
                       "【警告】VS Code 子进程崩溃！可能由损坏或不兼容的扩展引起。" +
                       Environment.NewLine + "建议移除可疑扩展或更新 VS Code。";
        TxtOutput.Text = display;
    }

    private Task<string> RunRaw(string args)
    {
        return Task.Run(() =>
        {
            var psi = new ProcessStartInfo(VsEnvExe, args + QuietFlag)
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                StandardOutputEncoding = Gbk,
                StandardErrorEncoding = Gbk
            };
            using var ps = Process.Start(psi)!;
            var txt = ps.StandardOutput.ReadToEnd() + ps.StandardError.ReadToEnd();
            ps.WaitForExit();
            return txt;
        });
    }

    private string FormatExtensions(string rawList)
    {
        if (string.IsNullOrWhiteSpace(rawList))
            return "未安装任何扩展。";
        var lines = rawList.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        sb.AppendLine("已安装的扩展：");
        sb.AppendLine(new string('-', 50));
        foreach (var line in lines)
        {
            var id = line.Trim();
            if (string.IsNullOrEmpty(id) || id.StartsWith("FATAL ERROR", StringComparison.OrdinalIgnoreCase))
                continue;
            sb.AppendLine($"• {GetFriendlyExtensionName(id)}");
        }
        return sb.ToString();
    }

    private static string GetFriendlyExtensionName(string extensionId)
    {
        var known = extensionId switch
        {
            "ms-python.python" => "Python (by Microsoft)",
            "ms-python.vscode-pylance" => "Pylance – Python Language Server (by Microsoft)",
            "ms-python.debugpy" => "Python Debugger (debugpy) (by Microsoft)",
            "ms-python.vscode-python-envs" => "Python Environments (by Microsoft)",
            "ms-ceintl.vscode-language-pack-zh-hans" => "中文 (简体) 语言包 (by Microsoft)",
            "augment.vscode-augment" => "VSCode Augment (by Augment)",
            _ => null
        };
        if (known != null) return known;
        if (!extensionId.Contains('.')) return $"{extensionId} (格式无效)";
        var parts = extensionId.Split('.', 2);
        var publisher = parts[0];
        var name = parts[1];
        string FormatPublisher(string p) =>
            string.IsNullOrEmpty(p) ? "Unknown" : char.ToUpper(p[0]) + p.Substring(1).ToLower();
        string FormatName(string n)
        {
            if (string.IsNullOrEmpty(n)) return "Unknown Extension";
            var words = n.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
                if (words[i].Length > 0)
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            return string.Join(" ", words);
        }
        var fp = publisher == "undefined_publisher" ? "⚠️ 未知发布者" : FormatPublisher(publisher);
        return $"{FormatName(name)} (by {fp})";
    }
    #endregion

}
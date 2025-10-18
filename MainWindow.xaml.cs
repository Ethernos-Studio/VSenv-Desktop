using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace VSenvDesktop;

public partial class MainWindow : Window
{
    private static readonly string VsEnvExe = FindVsEnv();   // 找 vsenv.exe
    private static Encoding Gbk => Encoding.GetEncoding(936);
    private const string QuietFlag = " --quiet";
    private const string NotFoundHint = """
【VSenvDesktop 提示】
出现该错误表示实例目录下缺少 Code.exe，可按以下步骤修复：

1. 将官方 VS Code 离线包（zip）解压到：
   {0}\\vscode\\
   解压后应存在：
   {0}\\vscode\\Code.exe

2. 或把已安装的 Code.exe 整个目录复制到上述路径。

3. 重新点击【刷新】按钮即可。
""";

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

            var friendly = GetFriendlyExtensionName(id);
            sb.AppendLine($"• {friendly}");
        }

        return sb.ToString();
    }

    private static string GetFriendlyExtensionName(string extensionId)
    {
        // 1. 硬编码常见扩展（优先）
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

        if (known != null)
            return known;

        // 2. 自动解析 publisher.name 格式
        if (string.IsNullOrWhiteSpace(extensionId) || !extensionId.Contains('.'))
            return $"{extensionId} (格式无效)";

        var parts = extensionId.Split('.', 2); // 只 split 第一个点
        var publisher = parts[0];
        var name = parts[1];

        // 处理 publisher：转为首字母大写（简单处理）
        string FormatPublisher(string p)
        {
            if (string.IsNullOrEmpty(p)) return "Unknown";
            // 如果全是小写或带连字符，尝试美化
            return char.ToUpper(p[0]) + p.Substring(1).ToLower();
        }

        // 处理 name：将 kebab-case / snake_case 转为 Title Case
        string FormatName(string n)
        {
            if (string.IsNullOrEmpty(n)) return "Unknown Extension";

            // 支持 kebab-case (my-extension) 和 snake_case (my_extension)
            var separators = new char[] { '-', '_' };
            var words = n.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length == 0) continue;
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
            return string.Join(" ", words);
        }

        var formattedPublisher = publisher == "undefined_publisher"
            ? "⚠️ 未知发布者"
            : FormatPublisher(publisher);

        return $"{FormatName(name)} (by {formattedPublisher})";
    }

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, __) => RefreshInstances();
    }

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

    /*---------------- 按钮事件 ----------------*/
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
        var name = CmbInstances.SelectedItem as string;
        if (name == null) return;
        if (MessageBox.Show($"确定删除 {name} ?", "确认", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            return;
        await Run($"remove {name}");
        await RefreshInstances();
    }

    private async void BtnListExt_Click(object sender, RoutedEventArgs e)
    {
        var inst = GetSelectedInstance();
        if (inst == null) return;
        await Run($"extension {inst.Name} list");
    }

    /*---------------- 内部 ----------------*/
    private static string FindVsEnv()
    {
        // 1. 同目录
        var dir = Path.GetDirectoryName(Environment.ProcessPath)!;
        var local = Path.Combine(dir, "vsenv.exe");
        if (File.Exists(local)) return local;

        // 2. PATH
        return "vsenv.exe";      // 依赖 PATH
    }

    private async Task RefreshInstances()
    {
        TxtStatus.Text = "刷新实例列表…";
        CmbInstances.Items.Clear();

        var txt = await RunRaw("list");
        using var reader = new StringReader(txt);
        string? line;

        bool pastHeader = false;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            line = line.Trim();

            // 跳过表头和分隔线
            if (line.StartsWith("Instance Name") || line.StartsWith("---"))
                continue;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split('\t', StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
                continue;

            var name = parts[0];
            var path = parts[1];

            // 判断是否无效实例
            bool isInvalid = path.Contains("(无效实例)");

            // 去掉路径中的 (无效实例) 标记，保留真实路径
            var cleanPath = path.Replace(" (无效实例)", "").Trim();

            // 显示名称加上无效标记（但不影响内部路径）
            var displayName = isInvalid ? $"{name} (无效实例)" : name;

            // 添加到 ComboBox，Tag 存真实路径
            var item = new ComboBoxItem
            {
                Content = displayName,
                Tag = new InstanceInfo { Name = name, Path = cleanPath, IsValid = !isInvalid }
            };
            CmbInstances.Items.Add(item);
        }

        TxtStatus.Text = "就绪";
    }

    private string BuildStartOptions()
    {
        var sb = new System.Text.StringBuilder();
        if (ChkSandbox.IsChecked == true) sb.Append(" --sandbox");
        if (ChkFakeHW.IsChecked == true) sb.Append(" --fake-hw");
        if (ChkHost.IsChecked == true) sb.Append(" --host");
        if (ChkMac.IsChecked == true) sb.Append(" --mac");
        return sb.ToString();
    }

    private async Task Run(string args)
    {
        TxtOutput.Clear();
        TxtStatus.Text = "执行中…";
        var rawOutput = await RunRaw(args);

        string displayOutput = rawOutput;

        // 如果是列出扩展的命令，尝试美化
        if (args.Contains("extension", StringComparison.OrdinalIgnoreCase) &&
            args.Contains("list", StringComparison.OrdinalIgnoreCase))
        {
            displayOutput = FormatExtensions(rawOutput);
        }
        else if (rawOutput.Contains("Code.exe not found", StringComparison.OrdinalIgnoreCase))
        {
            var instancePath = GetInstancePath();
            var hint = string.Format(NotFoundHint, instancePath);
            displayOutput = rawOutput + Environment.NewLine + Environment.NewLine + hint;
        }
        else if (rawOutput.Contains("FATAL ERROR", StringComparison.OrdinalIgnoreCase))
        {
            displayOutput += Environment.NewLine + Environment.NewLine +
                "【警告】VS Code 子进程崩溃！可能由损坏或不兼容的扩展引起。" +
                Environment.NewLine +
                "建议移除可疑扩展或更新 VS Code。";
        }

        TxtOutput.Text = displayOutput;
        TxtStatus.Text = "完成";
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
                UseShellExecute = false
            };
            // 放在初始化器外面
            psi.StandardOutputEncoding = Encoding.GetEncoding(936);
            psi.StandardErrorEncoding = Encoding.GetEncoding(936);

            using var ps = Process.Start(psi)!;
            var txt = ps.StandardOutput.ReadToEnd() + ps.StandardError.ReadToEnd();
            ps.WaitForExit();
            return txt;
        });
    }
    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "VSenv Desktop\n\n" +
            "一个用于管理 VS Code 多实例的桌面工具。\n" +
            "基于 vsenv.exe 构建。\n\n" +
            $"版本：1.0.0\n" +
            $"构建日期：{DateTime.Now:yyyy-MM-dd}",
            "关于",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }
}
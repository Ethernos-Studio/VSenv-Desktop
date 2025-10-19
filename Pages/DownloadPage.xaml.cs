using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;   // 消除歧义
using Page = iNKORE.UI.WPF.Modern.Controls.Page;   // 明确用 Modern.Page

namespace VSenvDesktop.Pages
{
    public partial class DownloadPage : Page
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(10) };
        private static Encoding Gbk => Encoding.GetEncoding(936);
        private readonly DownloadViewModel Vm = new();
        /// <summary>
        /// VS Code 更新 API 返回的 JSON 结构
        /// </summary>
        private record VSCodeVersion(
            string Name,
            string Version,
            string ProductVersion,
            long Timestamp);

        public DownloadPage()
        {
            InitializeComponent();
            DataContext = Vm;
            Loaded += async (_, __) => await LoadLatestVersion();
        }

        private record DownloadMetrics(
            long BytesDone,
            long TotalBytes,
            double SpeedBps,   // 字节/秒
            TimeSpan Elapsed,
            TimeSpan Remaining);

        #region 获取最新版本
        private async Task LoadLatestVersion()
        {
            TxtOutput.AppendText("正在获取最新版本信息...\r\n");
            // 1. 把 LoadLatestVersion 里原来那段直接替换：
            try
            {
                var api = "https://update.code.visualstudio.com/api/latest/win32-x64-archive/stable";
                var json = await Http.GetFromJsonAsync<VSCodeVersion>(api);
                Vm.Version = json?.ProductVersion ?? "unknown";
                Vm.DownloadUrl = $"https://update.code.visualstudio.com/{Vm.Version}/win32-x64-archive/stable";
                TxtOutput.AppendText($"最新版本：{Vm.Version}\r\n");
            }
            catch (Exception ex)
            {
                TxtOutput.AppendText($"获取版本失败：{ex.Message}\r\n");
                Vm.Version = "unknown";
            }
            TxtOutput.ScrollToEnd();
        }
        #endregion

        #region 下载并创建
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Vm.InstanceName))
            {
                MessageBox.Show("请输入新实例名称！", "提示");
                return;
            }

            var saveFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".vsenv", "downloads");
            Directory.CreateDirectory(saveFolder);
            var zipPath = Path.Combine(saveFolder, $"{Vm.InstanceName}-vscode.zip");

            BtnDownload.IsEnabled = false;
            Vm.Progress = 0;

            try
            {
                // 1. 下载
                TxtOutput.AppendText($"开始下载 {Vm.DownloadUrl} ...\r\n");
                await DownloadFileAsync(Vm.DownloadUrl, zipPath, Vm);

                // 2. 解压到临时目录
                var tempDir = Path.Combine(saveFolder, Vm.InstanceName);
                var vscodeDir = Path.Combine(tempDir, "vscode");   // ← 关键
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                Directory.CreateDirectory(vscodeDir);              // 先建好 vscode 文件夹

                TxtOutput.AppendText("正在解压...\r\n");
                await Task.Run(() =>
                {
                    // 先解压到临时根
                    ZipFile.ExtractToDirectory(zipPath, tempDir);
                    // 把刚解压出来的所有文件/文件夹移进 vscode\
                    foreach (var item in Directory.GetFileSystemEntries(tempDir)
                                                   .Where(p => !p.EndsWith("\\vscode")))
                    {
                        var dest = Path.Combine(vscodeDir, Path.GetFileName(item));
                        if (File.Exists(item))
                            File.Move(item, dest, true);
                        else
                            Directory.Move(item, dest);
                    }
                });

                // 3. 创建实例（指向父目录，vsenv 会自己找 vscode\Code.exe）
                TxtOutput.AppendText("正在创建实例...\r\n");
                var exitCode = await Task.Run(() =>
                {
                    var psi = new ProcessStartInfo(VSenvExe,
                        $"create {Vm.InstanceName} \"{tempDir}\" --quiet")
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
                    if (ps.ExitCode != 0 && !string.IsNullOrWhiteSpace(output))
                        Dispatcher.BeginInvoke(() =>
                            MessageBox.Show(output, "创建失败", MessageBoxButton.OK, MessageBoxImage.Warning));
                    return ps.ExitCode;
                });

                if (exitCode == 0)
                {
                    TxtOutput.AppendText("实例创建完成！\r\n");
                    MessageBox.Show($"实例 {Vm.InstanceName} 已就绪！", "完成",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    File.Delete(zipPath);   // 可选：清理下载包
                }
            }
            catch (Exception ex)
            {
                TxtOutput.AppendText($"错误：{ex.Message}\r\n");
                MessageBox.Show(ex.Message, "下载/安装失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnDownload.IsEnabled = true;
                Vm.Progress = 0;
                TxtOutput.ScrollToEnd();
            }
        }
        #endregion

        private async Task DownloadFileAsync(string url, string target, DownloadViewModel vm)
        {
            using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var totalBytes = response.Content.Headers.ContentLength ?? 0L;

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            const int bufferLen = 8192;
            var buffer = new byte[bufferLen];
            long doneBytes = 0;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            long lastBytes = 0;
            DateTime lastTime = DateTime.Now;

            while (true)
            {
                var bytesRead = await contentStream.ReadAsync(buffer, 0, bufferLen);
                if (bytesRead == 0) break;

                await fileStream.WriteAsync(buffer, 0, bytesRead);
                doneBytes += bytesRead;

                // ---- 每 500ms 输出一次日志 ----
                if (DateTime.Now - lastTime >= TimeSpan.FromMilliseconds(500))
                {
                    var elapsed = sw.Elapsed;
                    var speedBps = (doneBytes - lastBytes) / (DateTime.Now - lastTime).TotalSeconds;
                    lastBytes = doneBytes;
                    lastTime = DateTime.Now;

                    var remainingMs = speedBps > 0 ? (totalBytes - doneBytes) / speedBps * 1000 : 0;
                    var remaining = TimeSpan.FromMilliseconds(remainingMs);

                    var metrics = new DownloadMetrics(
                        BytesDone: doneBytes,
                        TotalBytes: totalBytes,
                        SpeedBps: speedBps,
                        Elapsed: elapsed,
                        Remaining: remaining);

                    LogMetrics(metrics);
                    vm.Progress = totalBytes > 0 ? (int)(doneBytes * 100 / totalBytes) : 0;
                }
            }
            // 最终 100%
            LogMetrics(new DownloadMetrics(doneBytes, totalBytes, 0, sw.Elapsed, TimeSpan.Zero));
            vm.Progress = 100;
        }

        private void LogMetrics(DownloadMetrics m)
        {
            var percent = m.TotalBytes > 0 ? $"{m.BytesDone * 100.0 / m.TotalBytes:F1}%" : "0%";
            var speedStr = m.SpeedBps switch
            {
                >= 1_048_576 => $"{m.SpeedBps / 1_048_576:F1} MB/s",
                >= 1024 => $"{m.SpeedBps / 1024:F1} KB/s",
                _ => $"{m.SpeedBps:F0} B/s"
            };
            var doneMB = m.BytesDone / 1_048_576.0;
            var totalMB = m.TotalBytes / 1_048_576.0;
            var elapsed = m.Elapsed.ToString(@"mm\:ss");
            var remaining = m.Remaining.ToString(@"mm\:ss");

            Dispatcher.BeginInvoke(() =>
            {
                TxtOutput.AppendText($"[{elapsed}] {percent} | {doneMB:F1}/{totalMB:F1} MB | {speedStr} | 剩余 {remaining}\r\n");
                TxtOutput.ScrollToEnd();
            });
        }

        #region vsenv 路径
        private static string VSenvExe
        {
            get
            {
                var dir = Path.GetDirectoryName(Environment.ProcessPath)!;
                var local = Path.Combine(dir, "vsenv.exe");
                return File.Exists(local) ? local : "vsenv.exe";
            }
        }
        #endregion
    }

    #region MVVM
    public class DownloadViewModel : INotifyPropertyChanged
    {
        private string _version = "检测中...";
        private string _downloadUrl = string.Empty;
        private string _instanceName = string.Empty;
        private int _progress;

        public string Version
        {
            get => _version;
            set { _version = value; OnPropertyChanged(); }
        }

        public string DownloadUrl
        {
            get => _downloadUrl;
            set { _downloadUrl = value; OnPropertyChanged(); }
        }

        public string InstanceName
        {
            get => _instanceName;
            set { _instanceName = value; OnPropertyChanged(); }
        }

        public int Progress
        {
            get => _progress;
            set { _progress = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? p = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
    #endregion
}

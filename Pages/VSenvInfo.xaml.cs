using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using iNKORE.UI.WPF.Modern.Controls;
using MessageBox = System.Windows.MessageBox;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace VSenvDesktop.Pages
{
    public partial class VSenvInfo : Page
    {
        private static readonly string VsEnvExe = FindVsEnv();
        private static readonly Encoding Gbk = Encoding.GetEncoding(936);

        public VSenvInfo()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadVersions();
        }

        #region 辅助：找 vsenv.exe
        private static string FindVsEnv()
        {
            var dir = Path.GetDirectoryName(Environment.ProcessPath)!;
            var local = Path.Combine(dir, "vsenv.exe");
            return File.Exists(local) ? local : "vsenv.exe";
        }
        #endregion

        #region 页面加载时获取版本
        private async Task LoadVersions()
        {
            // 本地版本
            var local = await GetLocalVersionAsync();
            TbLocalVer.Text = $"已安装的 VSenv 版本：{local}";

            // 远程版本
            var remote = await GetGitHubLatestVersionAsync();
            TbRemoteVer.Text = $"最新 VSenv 版本 (GitHub)：{remote ?? "获取失败"}";
        }

        private async Task<string?> GetLocalVersionAsync()
        {
            try
            {
                var psi = new ProcessStartInfo(VsEnvExe, "--version --quiet")
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,   // ← 必须加上
                    UseShellExecute = false,
                    StandardOutputEncoding = Gbk,
                    StandardErrorEncoding = Gbk
                };

                using var proc = Process.Start(psi)!;
                await proc.WaitForExitAsync();

                // vsenv 把版本号写到 stderr，合并即可
                var output = (await proc.StandardOutput.ReadToEndAsync()).Trim();
                var error = (await proc.StandardError.ReadToEndAsync()).Trim();
                return string.IsNullOrWhiteSpace(output) ? error : output;
            }
            catch { return null; }
        }

        private async Task<string?> GetGitHubLatestVersionAsync()
        {
            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "VSenvDesktop");
                var json = await http.GetStringAsync("https://api.github.com/repos/Ethernos-Studio/vsenv/releases/latest");
                // 极简解析
                var tag = System.Text.Json.JsonDocument.Parse(json)
                               .RootElement.GetProperty("tag_name").GetString();
                return tag;
            }
            catch { return null; }
        }
        #endregion

        #region 按钮事件
        private async void BtnSelfCheck_Click(object sender, RoutedEventArgs e)
            => await RunVsEnvAsync("doctor --quiet");

        private async void BtnRunDebug_Click(object sender, RoutedEventArgs e)
        {
            var cmd = CmbDebugCmd.SelectedIndex switch
            {
                0 => "-debug token --quiet",
                1 => "-debug proc --quiet",
                _ => ""
            };
            if (!string.IsNullOrEmpty(cmd))
                await RunVsEnvAsync(cmd);
        }

        private void BtnDownload_Click(object sender, RoutedEventArgs e)
            => Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Ethernos-Studio/vsenv",
                UseShellExecute = true
            });
        #endregion

        #region 统一执行 vsenv 并回写日志
        private async Task RunVsEnvAsync(string arguments)
        {
            TxtOutput.Clear();
            Ring.IsActive = true;
            try
            {
                var psi = new ProcessStartInfo(VsEnvExe, arguments)
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    StandardOutputEncoding = Gbk,
                    StandardErrorEncoding = Gbk
                };

                using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
                var sb = new StringBuilder();

                void OnData(string? x) { if (x != null) sb.AppendLine(x); }
                proc.OutputDataReceived += (_, args) => OnData(args.Data);
                proc.ErrorDataReceived += (_, args) => OnData(args.Data);

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                await proc.WaitForExitAsync();

                TxtOutput.Text = sb.ToString();
                if (string.IsNullOrWhiteSpace(TxtOutput.Text))
                    TxtOutput.Text = "(命令执行完毕，无输出)";
            }
            catch (Exception ex)
            {
                TxtOutput.Text = ex.ToString();
            }
            finally
            {
                Ring.IsActive = false;
            }
        }
        #endregion
    }
}
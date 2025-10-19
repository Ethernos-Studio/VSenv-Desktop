using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using VSenvDesktop.Helpers;
using iNKORE.UI.WPF.Modern;   // ThemeManager
using iNKORE.UI.WPF.Modern.Common; // ApplicationTheme
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace VSenvDesktop.Pages
{
    public partial class SettingsPage : Page
    {
        public SettingsPage() => InitializeComponent();

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            TswAutoStart.IsChecked = IsAutoStartEnabled();
            TswTheme.IsChecked = !ThemeHelper.SystemUsesLightTheme();
            TxtDefaultProxy.Text = GetProxy();   // 简单本地 txt，见下方
        }

        #region 主题
        private void TswTheme_Checked(object sender, RoutedEventArgs e)
            => ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;

        private void TswTheme_Unchecked(object sender, RoutedEventArgs e)
            => ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
        #endregion

        #region 开机启动
        private bool IsAutoStartEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue("VSenvDesktop") != null;
        }

        private void TswAutoStart_Checked(object sender, RoutedEventArgs e)
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            var exe = Process.GetCurrentProcess().MainModule!.FileName!;
            key?.SetValue("VSenvDesktop", $"\"{exe}\"");
        }

        private void TswAutoStart_Unchecked(object sender, RoutedEventArgs e)
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            key?.DeleteValue("VSenvDesktop", false);
        }
        #endregion

        #region 默认代理
        private static string ProxyFile => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VSenvDesktop", "proxy.txt");

        private static string GetProxy()
        {
            try { return File.ReadAllText(ProxyFile).Trim(); }
            catch { return string.Empty; }
        }

        private void TxtDefaultProxy_LostFocus(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ProxyFile)!);
            File.WriteAllText(ProxyFile, TxtDefaultProxy.Text.Trim());
        }
        #endregion

        #region 超链接
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        #endregion
    }
}
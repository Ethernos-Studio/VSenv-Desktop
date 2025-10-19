using Microsoft.Win32;               // Registry
using iNKORE.UI.WPF.Modern;          // ThemeManager

namespace VSenvDesktop.Helpers
{
    internal static class ThemeHelper
    {
        private const string RegKey =
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string RegValue = "AppsUseLightTheme";

        /// <summary>
        /// 0 = Dark, 1 = Light
        /// </summary>
        public static bool SystemUsesLightTheme()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegKey);
            return key?.GetValue(RegValue, 1) is int v && v == 1;
        }

        /// <summary>
        /// 应用当前系统主题
        /// </summary>
        public static void SyncToSystem()
        {
            bool light = SystemUsesLightTheme();
            ThemeManager.Current.ApplicationTheme = light ? ApplicationTheme.Light : ApplicationTheme.Dark;
        }

        /// <summary>
        /// 监听系统主题切换（Win10 1903+）
        /// </summary>
        public static void WatchSystemTheme()
        {
            SystemEvents.UserPreferenceChanged += (_, e) =>
            {
                if (e.Category == UserPreferenceCategory.General)
                    SyncToSystem();
            };
        }
    }
}
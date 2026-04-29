using Microsoft.UI.Xaml;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AppAppBar3
{
    // Applies the user's saved theme preference to every WinUI window in the
    // app, and re-applies on Windows theme flips when the user has chosen
    // "Default" (follow Windows).
    //
    // We resolve "Default" to an explicit Light/Dark by reading
    // HKCU\...\Personalize\AppsUseLightTheme rather than leaving
    // RequestedTheme = Default — WinUI 3 desktop windows don't reliably
    // repaint when the system theme flips at runtime.
    public static class ThemeHelper
    {
        private const string SettingKey = "theme";

        private static readonly List<Window> _windows = new();
        private static bool _systemEventsHooked;

        public static ElementTheme LoadSavedTheme()
        {
            var raw = SettingMethods.loadSettings(SettingKey) as string;
            return Enum.TryParse<ElementTheme>(raw, out var t) ? t : ElementTheme.Default;
        }

        public static void SaveAndApply(ElementTheme theme)
        {
            SettingMethods.saveSetting(SettingKey, theme.ToString());
            ApplyAll();
        }

        public static void Register(Window window)
        {
            if (window == null || _windows.Contains(window)) return;
            _windows.Add(window);
            window.Closed += (s, e) => _windows.Remove(window);
            Apply(window, LoadSavedTheme());
            EnsureSystemEventsHooked();
        }

        public static void ApplyAll()
        {
            var saved = LoadSavedTheme();
            foreach (var w in _windows.ToArray()) Apply(w, saved);
        }

        private static void Apply(Window window, ElementTheme saved)
        {
            var resolved = saved == ElementTheme.Default ? ResolveSystemTheme() : saved;

            if (window?.Content is FrameworkElement fe)
            {
                // Setting to the resolved Light/Dark (rather than Default) ensures the
                // ThemeResource brushes flip immediately on a system theme change.
                fe.RequestedTheme = resolved;
            }

            ApplyImmersiveDarkMode(window, resolved);
        }

        private static ElementTheme ResolveSystemTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int i)
                    return i == 0 ? ElementTheme.Dark : ElementTheme.Light;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ResolveSystemTheme failed: " + ex.Message);
            }
            return ElementTheme.Light;
        }

        private static void ApplyImmersiveDarkMode(Window window, ElementTheme resolved)
        {
            if (window == null) return;
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                int useDark = resolved == ElementTheme.Dark ? 1 : 0;
                NativeMethods.DwmSetWindowAttribute(
                    hwnd,
                    NativeMethods.DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE,
                    ref useDark,
                    sizeof(int));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ApplyImmersiveDarkMode failed: " + ex.Message);
            }
        }

        private static void EnsureSystemEventsHooked()
        {
            if (_systemEventsHooked) return;
            _systemEventsHooked = true;
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.General) return;
            // Only react when the user is following Windows; explicit Light/Dark wins.
            if (LoadSavedTheme() != ElementTheme.Default) return;

            // Fires on a non-UI thread — marshal each apply to its window's dispatcher.
            foreach (var w in _windows.ToArray())
            {
                var dq = w.DispatcherQueue;
                if (dq == null) continue;
                dq.TryEnqueue(() => Apply(w, ElementTheme.Default));
            }
        }
    }
}

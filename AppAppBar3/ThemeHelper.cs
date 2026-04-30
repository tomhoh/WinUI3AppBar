using Microsoft.UI.Xaml;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.ViewManagement;

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
        // Held in a static so the GC doesn't collect the UISettings instance and drop our event subscription.
        private static UISettings _uiSettings;

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
                // Toggle through Default before setting the resolved theme. WinUI 3
                // desktop doesn't always re-evaluate ThemeResource bindings on a
                // top-level window's content when RequestedTheme is set to a value
                // equal to the implicit one — the AppBar window in particular (which
                // has had WS_CAPTION/WS_THICKFRAME stripped and SWP_FRAMECHANGED
                // applied) gets stuck at the original theme until app restart.
                if (fe.RequestedTheme != ElementTheme.Default)
                    fe.RequestedTheme = ElementTheme.Default;
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

            // UISettings.ColorValuesChanged is the WinRT-native theme/accent flip event
            // and fires reliably in WinUI 3 desktop where SystemEvents.UserPreferenceChanged
            // sometimes did not (especially while the AppBar's frame was stripped).
            try
            {
                _uiSettings = new UISettings();
                _uiSettings.ColorValuesChanged += OnColorValuesChanged;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("UISettings hook failed, falling back to SystemEvents: " + ex.Message);
            }

            // Belt-and-suspenders: SystemEvents covers cases where UISettings doesn't fire
            // (e.g. the high-contrast toggle that doesn't change AppsUseLightTheme).
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        private static void OnColorValuesChanged(UISettings sender, object args)
        {
            ReapplyToFollowers();
        }

        private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.General) return;
            ReapplyToFollowers();
        }

        private static void ReapplyToFollowers()
        {
            // Only react when the user is following Windows; explicit Light/Dark wins.
            if (LoadSavedTheme() != ElementTheme.Default) return;

            // Both events fire on non-UI threads — marshal each apply to its window's dispatcher.
            foreach (var w in _windows.ToArray())
            {
                var dq = w.DispatcherQueue;
                if (dq == null) continue;
                dq.TryEnqueue(() => Apply(w, ElementTheme.Default));
            }
        }
    }
}

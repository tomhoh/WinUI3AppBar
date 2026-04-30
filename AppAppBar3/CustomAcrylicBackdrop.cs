using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace AppAppBar3
{
    // SystemBackdrop subclass that drives a DesktopAcrylicController directly,
    // exposing TintOpacity and LuminosityOpacity as configurable knobs. The
    // built-in DesktopAcrylicBackdrop in Microsoft.UI.Xaml.Media doesn't expose
    // these in every WinAppSDK 1.x build (DesktopAcrylicKind.Thin / .Kind aren't
    // present on the user's reference assemblies), so we manage the controller
    // ourselves to give the user a slider for translucency.
    public sealed class CustomAcrylicBackdrop : SystemBackdrop
    {
        private DesktopAcrylicController _controller;
        private SystemBackdropConfiguration _config;

        // 0.0 = pure see-through (no tint); 1.0 = solid tint color.
        public float TintOpacity { get; set; } = 0.4f;

        // 0.0 = clear glass; 1.0 = fully frosted.
        public float LuminosityOpacity { get; set; } = 0.85f;

        // null = let the controller pick a theme-appropriate default.
        public Color? TintColor { get; set; }

        protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
        {
            if (!DesktopAcrylicController.IsSupported())
                return;

            _config = new SystemBackdropConfiguration
            {
                IsInputActive = true,
                Theme = ResolveTheme(xamlRoot),
            };

            _controller = new DesktopAcrylicController
            {
                TintOpacity = TintOpacity,
                LuminosityOpacity = LuminosityOpacity,
            };
            if (TintColor.HasValue)
                _controller.TintColor = TintColor.Value;

            _controller.AddSystemBackdropTarget(connectedTarget);
            _controller.SetSystemBackdropConfiguration(_config);
        }

        protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
        {
            if (_controller != null)
            {
                _controller.RemoveSystemBackdropTarget(disconnectedTarget);
                _controller.Dispose();
                _controller = null;
            }
            _config = null;
        }

        private static SystemBackdropTheme ResolveTheme(XamlRoot xamlRoot)
        {
            if (xamlRoot?.Content is FrameworkElement fe)
            {
                return fe.ActualTheme switch
                {
                    ElementTheme.Dark => SystemBackdropTheme.Dark,
                    ElementTheme.Light => SystemBackdropTheme.Light,
                    _ => SystemBackdropTheme.Default,
                };
            }
            return SystemBackdropTheme.Default;
        }
    }
}

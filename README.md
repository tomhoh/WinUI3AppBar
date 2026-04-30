## WINUI 3 Appbar
Implementation of a Desktop AppBar (taskbar) in WINUI 3. Similar to WPF and WinForms Appbars.  
Includes an example webview (webview2) window that is docked to the appbar when open.

![AppApbar3](https://github.com/user-attachments/assets/4b9c7b84-c161-4bde-a1c3-b916f0bee4cf)

### Requirements
- Visual Studio 2022 (17.8 or later)
- .NET 8 SDK
- Windows App SDK 1.8
- Windows App SDK (For VS 2022 C#) : https://aka.ms/windowsappsdk/stable-vsix-2022-cs
- Webview2 Evergreen Runtime (https://developer.microsoft.com/en-us/microsoft-edge/webview2/?form=MA13LH) or simply remove (comment out) the webview from code
- WinUIex.  : https://github.com/dotMorten/WinUIEx?tab=readme-ov-file

You can find more information in here: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/set-up-your-development-environment?tabs=vs-2022

Because the included example docking window uses webview 2 you will also need to install Webview2 Evergreen Runtime (https://developer.microsoft.com/en-us/microsoft-edge/webview2/?form=MA13LH) or simply remove (comment out) the webview from code

For running the **unpackaged** builds (not MSIX) the target machine also needs the Windows App Runtime 1.8 framework installed: https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe



### Capabilities
- Allow docking to any side of the screen
- Auto-hide mode with smooth slide in/out and per-monitor DPI-aware trigger strip
- Multiple monitor support with friendly names ("Display 1", "Display 2", …)
- Handles per-monitor DPI scaling
- Does not show in switcher
- Excluded from aero peek
- Theme picker in Settings (Default / Light / Dark) — "Default" follows the current Windows theme and re-applies live when the system theme flips
- Optional **translucent acrylic background** with a user-controlled tint-opacity slider (Win11-taskbar style); only active when Theme = Default so an explicit color choice is preserved
- Right-click context menu on the bar surface for **Dock Top / Right / Bottom / Left**, Identify Monitor, Settings, and Close — replaces the in-bar toolbar
- Settings window light-dismisses when the user clicks outside it
- Controls (web icon, shortcut buttons) scale proportionally to the configured bar size and reorient when the bar is docked horizontally vs vertically
- Handles Drag and drop shortcut with deletion, autosaving and auto restore on startup
- Option to run at login (autostart).
- Settings persisted to `%LOCALAPPDATA%\AppAppBar3\settings.json` so packaged and unpackaged builds share the same storage.
- Builds in three flavors — MSIX package, unpackaged runtime-dependent `.exe`, and unpackaged fully self-contained `.exe` — all produced automatically by the GitHub Actions workflow in `.github/workflows/build.yml`.

## To do
- Build into Library
- code refractoring
- Bug fixes

## Notes
04/30/26  Bulk merge from **Experimental-2** rolling up the recent UX work:
- **Translucent acrylic background** option in Settings (only active when Theme = Default). Implemented via the lower-level `Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController` in a custom `SystemBackdrop` subclass (`CustomAcrylicBackdrop`) — the built-in `DesktopAcrylicBackdrop.Kind` property isn't on every WinAppSDK 1.x reference, so we drive `TintOpacity` and `LuminosityOpacity` ourselves. A 0–100 slider in Settings drives both proportionally so the visual range goes from near-clear glass to heavily tinted.
- **Right-click context menu** on the AppBar surface for Dock Top / Right / Bottom / Left, Identify Monitor, Settings, and Close. Replaces the old in-bar `CommandBar` and the standalone Close button.
- **Web button** is now an icon-only globe (`FontIcon` `Glyph=""`) with taskbar-style hover/pressed using `SubtleFillColorSecondaryBrush` / `SubtleFillColorTertiaryBrush`, square cell with `Margin="2"` and `CornerRadius="6"`.
- **Borderless AppBar** — root cause was WinUIEx's `IsTitleBarVisible="False"` calling `OverlappedPresenter.SetBorderAndTitleBar(true, false)` (explicitly enabling the border). Removed the property; existing `WS_CAPTION | WS_THICKFRAME` strip continues to keep the title bar hidden.
- **Friendly monitor names** ("Display 1", "Display 2", …). One-shot migration in `MainWindow.OnActivated` rewrites legacy `\\.\DISPLAYn` settings.
- **Bar-size proportional scaling** for controls (web icon FontSize, shortcut icons, `VariableGrid` cell sizes) plus orientation switching driven by the docked edge.
- **Settings window light-dismiss** on focus loss; redundant Close button removed.
- **WebWindow top-clip fix** — `monitorInfo`'s `WorkRect` was captured before the AppBar reserved its strip, so the WebWindow positioned under the bar. `DockToAppBar` now re-queries `MonitorHelper.GetMonitorsInfo()` per call.

04/29/26  Added a user-selectable theme on the **Experimental-2** branch:
- New **Theme** picker in Settings (Default / Light / Dark). "Default" follows the current Windows theme.
- A new `ThemeHelper` resolves "Default" against `HKCU\...\Personalize\AppsUseLightTheme` and hooks `UISettings.ColorValuesChanged` plus `SystemEvents.UserPreferenceChanged` so all open windows repaint immediately when the system theme flips — WinUI 3 desktop windows don't reliably do this on their own with `RequestedTheme = Default`.
- Non-client caption (where still visible) tints via `DwmSetWindowAttribute(DWMWA_USE_IMMERSIVE_DARK_MODE)` to match the resolved theme.

04/20/26  Merged the **Experimental** branch into master. Summary of what changed:
- Added a working **auto-hide** AppBar mode (ABM_SETAUTOHIDEBAREX + a DispatcherTimer state machine that slides the bar in/out with a ~200 ms ease-out, and suppresses itself over fullscreen apps via ABN_FULLSCREENAPP).
- Hardened the Win32 AppBar contract: corrected ABM_ message IDs, forward WM_ACTIVATE / WM_WINDOWPOSCHANGED to the shell, re-register on TaskbarCreated (explorer restart), dispose WindowMessageMonitor, fix WM_DISPLAYCHANGE (was accidentally WM_SETFOCUS), per-monitor DPI on the autohide trigger sliver.
- Retargeted project to **.NET 8** and **Windows App SDK 1.8.260317003**. Dropped legacy package refs and the `IWshRuntimeLibrary` COM type-library reference that used to block `dotnet publish`. Added `global.json` pinning the CLI to 8.0.x.
- Replaced `ApplicationData.Current.LocalSettings` (packaged-only) with a JSON-backed store so the unpackaged `.exe` can persist settings. Load-on-startup branches between `StartupTask` (packaged) and the HKCU Run key (unpackaged).
- GitHub Actions workflow builds all three artifact flavors on every push and uploads them — MSIX signed with a self-generated CI cert, plus two unpackaged variants.

12/08/24  Just realized that the last several commits did not have a default settings for first time run.  Now included in code.  Will cleanup later into a static class file.

### License
MIT License


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
- Multiple monitor support
- Handles per-monitor DPI scaling
- Does not show in switcher
- Excluded from aero peek
- Theme picker in Settings (Default / Light / Dark) — "Default" follows the current Windows theme and re-applies live when the system theme flips
- Handles Drag and drop shortcut with deletion, autosaving and auto restore on startup
- Option to run at login (autostart).
- Settings persisted to `%LOCALAPPDATA%\AppAppBar3\settings.json` so packaged and unpackaged builds share the same storage.
- Builds in three flavors — MSIX package, unpackaged runtime-dependent `.exe`, and unpackaged fully self-contained `.exe` — all produced automatically by the GitHub Actions workflow in `.github/workflows/build.yml`.

## To do
- Build into Library
- code refractoring
- Bug fixes

## Notes
04/29/26  Added a user-selectable theme on the **Experimental-2** branch:
- New **Theme** picker in Settings (Default / Light / Dark). "Default" follows the current Windows theme.
- A new `ThemeHelper` resolves "Default" against `HKCU\...\Personalize\AppsUseLightTheme` and hooks `SystemEvents.UserPreferenceChanged` so all open windows repaint immediately when the system theme flips — WinUI 3 desktop windows don't reliably do this on their own with `RequestedTheme = Default`.
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


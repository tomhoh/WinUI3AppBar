# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

WinUI 3 Desktop AppBar — a docked taskbar-style window (similar to legacy WPF/WinForms AppBars) implemented on top of the Windows App SDK 1.8 + WinUIEx, with auto-hide, multi-monitor, per-monitor DPI, drag-and-drop shortcuts, and an optional WebView2 docked window.

Single project (`AppAppBar3/AppAppBar3.csproj`) targeting `net8.0-windows10.0.19041.0`. The `.NET` SDK is pinned via `global.json` to `8.0.x` (`rollForward: latestFeature`). Solution file: `AppAppBar3.sln`. Platforms: `x86`, `x64`, `ARM64` (Release/Debug + a custom `x64test` configuration).

## Build / publish

The project must be built on Windows. Three artifact flavors are produced — see `.github/workflows/build.yml` for the canonical commands. **Use `msbuild`, not `dotnet publish`**: the Windows App SDK 1.8 targets reference `Microsoft.Build.Packaging.Pri.Tasks.dll`, which ships with Visual Studio rather than the standalone `dotnet` SDK (otherwise MSB4062).

```pwsh
# A — MSIX (signed, sideload). The MSIX subscriber must import ci-cert.cer
# into Local Machine > Trusted People before installing.
msbuild AppAppBar3/AppAppBar3.csproj /restore `
  /p:Configuration=Release /p:Platform=x64 `
  /p:AppxPackageSigningEnabled=true `
  /p:GenerateAppxPackageOnBuild=true `
  /p:AppxBundle=Never /p:UapAppxPackageBuildMode=SideloadOnly

# B — Unpackaged, runtime-dependent (needs Windows App Runtime 1.8 on target).
msbuild AppAppBar3/AppAppBar3.csproj /restore /t:Publish `
  /p:Configuration=Release /p:Platform=x64 /p:RuntimeIdentifier=win-x64 `
  /p:SelfContained=false /p:WindowsPackageType=None

# C — Unpackaged, fully self-contained (~100 MB, portable).
msbuild AppAppBar3/AppAppBar3.csproj /restore /t:Publish `
  /p:Configuration=Release /p:Platform=x64 /p:RuntimeIdentifier=win-x64 `
  /p:SelfContained=true /p:WindowsAppSDKSelfContained=true `
  /p:WindowsPackageType=None
```

Setting `WindowsPackageType=None` flips the csproj into unpackaged mode (disables MSIX tooling, signing, AppInstaller).

There are no automated tests in this repo. CI (`.github/workflows/build.yml`) runs all three builds on every push to `master`/`Experimental` and on PRs to `master`.

## Runtime requirements

- Visual Studio 2022 17.8+, Windows App SDK 1.8 (VSIX), .NET 8 SDK, WebView2 Evergreen Runtime.
- `WinUIEx` 2.9.0 (NuGet) — used for `WindowEx` and `WindowMessageMonitor`.
- Settings persist to `%LOCALAPPDATA%\AppAppBar3\settings.json` (works for packaged and unpackaged builds — packaged-only `ApplicationData.LocalSettings` was removed deliberately). Drag-and-drop shortcuts persist to `%LOCALAPPDATA%\shortcuts.txt`.

## Architecture

The whole app is one WinUI 3 window that talks to the Windows shell via the Win32 AppBar API (`SHAppBarMessage`, `ABM_*` / `ABN_*`). Most of the interesting code is in `MainWindow.xaml.cs`.

### AppBar lifecycle (MainWindow.xaml.cs)

1. `OnActivated` runs once on first activation. It loads settings (calling `setDefaultValues()` if `settings.json` is empty), captures the `AppWindow`/`HWND`, removes the window from Aero Peek (`DwmSetWindowAttribute(DWMWA_EXCLUDED_FROM_PEEK)`), and calls `RegisterAppBar`. Subscribers to `Activated` are detached after the first run.
2. `RegisterAppBar` sends `ABM_NEW` with a `RegisterWindowMessage("AppBarMessage")` callback id stored in `uCallBack`, then delegates to `ABSetPos`.
3. `ABSetPos` reads the current `autohide` setting and chooses **`ApplyDocked`** or **`ApplyAutohide`** (mutually exclusive — switching modes releases the other registration first).
4. `ApplyDocked` follows the MSDN AppBar sample contract: prime `abd.rc` with the monitor rect, pre-apply thickness, `ABM_QUERYPOS` → re-apply thickness → `ABM_SETPOS` → re-apply thickness → `SetWindowPos` → `ABM_WINDOWPOSCHANGED`. The double re-apply is intentional; the shell shrinks oversized proposals asymmetrically for Left vs. Right.
5. `ApplyAutohide` registers via `ABM_SETAUTOHIDEBAREX` (falls back to docked mode if the edge is already owned, e.g. by Windows' own taskbar autohide), then computes physical-pixel `shownRect` / `hiddenRect` / `triggerRect` and starts a `DispatcherTimer` state machine (`AutohideState.Hidden → Showing → Shown → Hiding`) that interpolates rects with a ~200 ms ease-out. Timer interval flips between 100 ms idle and 16 ms (~60 fps) while animating.
6. `OnWindowMessageReceived` (the `WindowMessageMonitor` WndProc) handles the AppBar callback (`ABN_POSCHANGED` → reconfigure; `ABN_FULLSCREENAPP` → snap hidden and pause), forwards `WM_ACTIVATE` and `WM_WINDOWPOSCHANGED` to the shell (`ABM_ACTIVATE` / `ABM_WINDOWPOSCHANGED`) per the AppBar contract, re-registers on `TaskbarCreated` (explorer restart), and rebuilds the monitor list on `WM_DISPLAYCHANGE`.
7. `appbarWindow_Closed` is the safety net for OS-initiated close — it always calls `UnregisterAppBar` and disposes the `WindowMessageMonitor`.

### Critical gotchas baked into the implementation

- **WinUIEx MinWidth clamp.** `WinUIEx.WindowEx` intercepts `WM_WINDOWPOSCHANGING` to enforce its content/MinWidth floor (~132 DIPs), which clobbers Left/Right bar widths. The constructor sets `MinWidth = MinHeight = 1`, and every `SetWindowPos` that resizes the bar passes `SWP_NOSENDCHANGING` to skip that interceptor. Don't remove either.
- **Frame style change.** `ApplyDocked`/`ApplyAutohide` strip `WS_CAPTION | WS_THICKFRAME` and then call `SetWindowPos(... SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE ...)` *before* the move so `WM_NCCALCSIZE` runs against the new frame.
- **Per-monitor DPI.** `app.manifest` declares `PerMonitorV2`. Bar size and the autohide trigger strip are scaled by `Monitor.scale` (from `GetDpiForMonitor` in `MonitorHelper.cs`); a 2-physical-pixel trigger would be invisible at 200% so it's `max(1, round(2 * scale))`.
- **Shortcut resolution.** `.lnk` files are resolved with late-bound `WScript.Shell` (`Type.GetTypeFromProgID`) rather than a `COMReference` to `IWshRuntimeLibrary`, because tlbimp can't process the latter under `dotnet publish` (MSB4803). Don't reintroduce the COM reference.

### Settings (SettingMethods.cs)

Static class. Loads/caches a `Dictionary<string,object>` from `%LOCALAPPDATA%\AppAppBar3\settings.json`, writes back via `JsonSerializer` on every `saveSetting`. Keys in use: `edge` (int — `ABEdge`), `monitor` (string — `\\.\DISPLAYn`), `bar_size` (int, DIPs), `autohide` (bool), `LoadOnStartup` (bool). `IsPackaged()` probes `Windows.ApplicationModel.Package.Current` (throws when unpackaged) and is used by `Settings.xaml.cs` to branch "load on startup" between the packaged `StartupTask` API (`StartupTaskId = "AppAppBar3Id"`, declared in `Package.appxmanifest`) and the unpackaged HKCU `Run` key (`Software\Microsoft\Windows\CurrentVersion\Run` / value `AppAppBar3`).

### Other windows

- `Settings.xaml.cs` — modeless settings window, docked next to the AppBar by `MainWindow.DockToAppBar` (centered along the bar's length, on the outside of the dock edge). Calls back into `parentWindow.restartAppBar()` after toggling `autohide` or `bar_size`.
- `WebWindow.xaml.cs` — example WebView2 window that, when opened, fills the work area of the selected monitor. The README notes you can comment it out if WebView2 Evergreen Runtime isn't installed.
- `WindowDetect.xaml.cs` — transient overlay that shows the display number on each monitor for ~4 s, used by the "Identify Monitor" toolbar button.

### Native interop (NativeMethods.cs)

Single static class with all `[DllImport]`s, structs, and enums (`APPBARDATA`, `MONITORINFOEX`, `RECT`, `POINT`, `AppBarMessages`, `ABNotify`, `ABEdge`, `DwmWindowAttribute`, etc.) plus a couple of helpers (`removeWindowDecoration`, `LogWin32Error`). Files generally pull these in via `using static AppAppBar3.NativeMethods;`. New P/Invokes belong here, not scattered across windows.

## Branching / CI conventions

- CI builds run on pushes to `master` and `Experimental`, and on PRs targeting `master`.
- The MSIX step uses a self-signed cert generated inside the workflow — its public `.cer` is uploaded alongside the `.msix` so sideloaders can trust it.
- The `Experimental` branch was merged into `master` on 04/20/26 (see README "Notes" — that merge is what introduced autohide, the .NET 8 retarget, and the JSON settings store).

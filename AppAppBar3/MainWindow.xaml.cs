using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using WinRT.Interop;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinUIEx.Messaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Microsoft.UI.Xaml.Media.Imaging;
using WinUIEx;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;



namespace AppAppBar3
{
    using static NativeMethods;
    using static MonitorHelper;
    using static SettingMethods;
    
    public sealed partial class MainWindow : WinUIEx.WindowEx, INotifyPropertyChanged
    {

        private ObservableCollection<Monitor> _MonitorList;

        string selectedItemsText;
        WindowMessageMonitor monitor;

        public ObservableCollection<Monitor> MonitorList

        {
            get => _MonitorList;
            set
            {
                _MonitorList = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> ListOfMonitors


        {
            
            get
            {
                List<string> mList = new List<string>();
                foreach (var mon in _MonitorList)
                {
                    mList.Add(mon.MonitorName);
                    
                }
                return new ObservableCollection<string>(mList);
            }
        }


        private List<Window> _OpenWindows = new List<Window>();
        public List<Window> OpenWindows
        {
            get => _OpenWindows;
            set
            {
                _OpenWindows = value;
            }
        } 
      

        private ABEdge _Edge;

        public ABEdge Edge
        {
            get => _Edge;
            set
            {
                _Edge = value;
                OnPropertyChanged();
            }
        }
        private bool fBarRegistered = false;
        public List<string> monitors;
        public List<Monitor> monitorInfo;

        private int uCallBack;
        private int taskbarCreatedMsg;
        private AppWindow appWindow;

        // --- Autohide state ---
        private enum AutohideState { Hidden, Showing, Shown, Hiding }
        private bool autoHideEnabled;
        private bool autohideRegistered;
        private ABEdge autohideRegisteredEdge;
        private RECT autohideMonitorRect;
        private RECT shownRect;
        private RECT hiddenRect;
        private RECT triggerRect;
        private AutohideState autohideState = AutohideState.Hidden;
        private DateTime animationStart;
        private DateTime cursorLeftShownAt = DateTime.MaxValue;
        private bool fullscreenAppActive;
        private DispatcherTimer autohideTimer;
        private const int AutohideAnimMs = 200;
        private const int AutohideHideDebounceMs = 300;
        private const int AutohideTriggerPxUnscaled = 2;
        public MainWindow()
        {

            this.InitializeComponent();
            ThemeHelper.Register(this);
            // Override the XAML MinWidth/MinHeight floor — WinUIEx.WindowEx's
            // WM_WINDOWPOSCHANGING interceptor was clamping our Left/Right bar up to
            // ~132 DIPs even when we asked for 50, because MinWidth=25 in XAML wasn't
            // the actual floor in practice.
            this.MinWidth = 1;
            this.MinHeight = 1;
            this.Activated += OnActivated;
            this.AppWindow.IsShownInSwitchers = false;
            monitorInfo = GetMonitorsInfo();
            MonitorList = new ObservableCollection<Monitor>(GetMonitorsInfo());
            edgeMonitor.DataContext = this;
            monitor = new WindowMessageMonitor(this);
            monitor.WindowMessageReceived += OnWindowMessageReceived;
            taskbarCreatedMsg = RegisterWindowMessage("TaskbarCreated");
            edgeMonitor.ItemsSource = Enum.GetValues(typeof(ABEdge));
        }
       

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Debug.WriteLine("MonitorList changed*****" + propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

       
        private void OnActivated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
        {

            edgeMonitor.SelectionChanged -= edgeComboBox_SelectionChanged;
            // selectedItemsText = @"\\.\DISPLAY1";

            if (appWindow == null)
            {
               

                //check if settings file exists

                if (loadSettings("edge") == null)
                {
                    SettingMethods.setDefaultValues();
                }
                MigrateLegacyMonitorSetting();
                edgeMonitor.SelectedItem = (ABEdge)loadSettings("edge");
               
                
                Debug.WriteLine("Window activated edge from settings " + (ABEdge)loadSettings("edge"));
                IntPtr hWnd = WindowNative.GetWindowHandle(this);
                WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
                appWindow = AppWindow.GetFromWindowId(windowId);

                //remove from aero peek
                    int value = 0x01;
                    int hr = DwmSetWindowAttribute(hWnd, DwmWindowAttribute.DWMWA_EXCLUDED_FROM_PEEK, ref value, Marshal.SizeOf(typeof(int)));

                    // Suppress the residual 1-px window frame that Win11 paints even
                    // after WS_CAPTION/WS_THICKFRAME are stripped. DWMWA_BORDER_COLOR
                    // accepts DWMWA_COLOR_NONE to disable the border outright.
                    // Ignored on Win10 — call is a safe no-op.
                    int noBorder = unchecked((int)DWMWA_COLOR_NONE);
                    DwmSetWindowAttribute(hWnd, DwmWindowAttribute.DWMWA_BORDER_COLOR, ref noBorder, Marshal.SizeOf(typeof(int)));

                // Ensure we only register the app bar once
                if (args.WindowActivationState != WindowActivationState.Deactivated)
                {
                    RegisterAppBar((ABEdge)loadSettings("edge"), (string)loadSettings("monitor"));

                    // Optionally, unsubscribe from Activated event after first activation
                    this.Activated -= OnActivated;
                }
                //monitors = MonitorHelper.GetMonitors();
               
        
                if (monitorInfo != null)
                {
                    monitors = new List<string>();
                    foreach (Monitor monitor in monitorInfo)
                    {
                        monitors.Add(monitor.MonitorName);
                    }

                }
                edgeMonitor.SelectionChanged += edgeComboBox_SelectionChanged;
                loadShortCuts();

                
            }

        }

       

        // settings.json from older builds stored the Win32 device path
        // ("\\.\DISPLAY1"). Convert to the new "Display N" form once so the
        // saved monitor matches a MonitorList entry.
        private static void MigrateLegacyMonitorSetting()
        {
            if (loadSettings("monitor") is string saved
                && saved.StartsWith(@"\\.\", StringComparison.Ordinal))
            {
                var migrated = MonitorHelper.FormatDisplayName(saved);
                if (!string.Equals(migrated, saved, StringComparison.Ordinal))
                    saveSetting("monitor", migrated);
            }
        }

        private void RegisterAppBar(ABEdge edge, string selectedMonitor)
        {
            if (fBarRegistered) return;

            var hWnd = WindowNative.GetWindowHandle(this);
            var abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = hWnd;
            uCallBack = RegisterWindowMessage("AppBarMessage");
            abd.uCallbackMessage = uCallBack;

            IntPtr result = SHAppBarMessage((int)AppBarMessages.ABM_NEW, ref abd);
            if (result == IntPtr.Zero)
            {
                Debug.WriteLine("ABM_NEW failed — shell rejected appbar registration.");
                return;
            }
            fBarRegistered = true;
            ABSetPos(edge, selectedMonitor);
        }

        private void UnregisterAppBar()
        {
            StopAutohideTimer();
            UnregisterAutohideIfNeeded();
            if (!fBarRegistered) return;

            var hWnd = WindowNative.GetWindowHandle(this);
            var abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = hWnd;
            SHAppBarMessage((int)AppBarMessages.ABM_REMOVE, ref abd);
            fBarRegistered = false;
        }
        private void ABSetPos(ABEdge edge, string selectedMonitor)
        {
            Debug.WriteLine("the selected monitor in ABSETPOS " + selectedMonitor);

            autoHideEnabled = (loadSettings("autohide") as bool?) ?? false;

            // Release any prior autohide registration before reconfiguring.
            UnregisterAutohideIfNeeded();

            var hWnd = WindowNative.GetWindowHandle(this);

            Monitor targetMonitor = null;
            foreach (var m in MonitorList)
                if (m.MonitorName == selectedMonitor) { targetMonitor = m; break; }
            if (targetMonitor == null) return;

            int theBarSize = (loadSettings("bar_size") as int?) ?? 50;
            double scaleFactor = targetMonitor.scale > 0 ? targetMonitor.scale : 1.0;
            int barSizeScaled = (int)Math.Round(theBarSize * scaleFactor);

            if (!autoHideEnabled)
            {
                ApplyDocked(hWnd, edge, targetMonitor, barSizeScaled);
            }
            else
            {
                ApplyAutohide(hWnd, edge, targetMonitor, barSizeScaled);
            }
        }

        private void ApplyDocked(IntPtr hWnd, ABEdge edge, Monitor targetMonitor, int barSizeScaled)
        {
            StopAutohideTimer();

            var abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
            abd.hWnd = hWnd;
            abd.uEdge = (int)edge;

            // Start with the full monitor rect and pre-apply the desired thickness on the
            // dock side before ABM_QUERYPOS — this matches the MSDN AppBar sample. Passing
            // the full work area as the proposed rect makes the shell shrink an oversized
            // proposal, and it does so asymmetrically for Left vs Right.
            abd.rc = targetMonitor.MonitorRect;
            ApplyThickness(ref abd.rc, edge, barSizeScaled);

            SHAppBarMessage((int)AppBarMessages.ABM_QUERYPOS, ref abd);
            ApplyThickness(ref abd.rc, edge, barSizeScaled);

            SHAppBarMessage((int)AppBarMessages.ABM_SETPOS, ref abd);
            ApplyThickness(ref abd.rc, edge, barSizeScaled);

            IntPtr style = GetWindowLong(hWnd, GWL_STYLE);
            style = (IntPtr)(style.ToInt64() & ~(WS_CAPTION | WS_THICKFRAME));
            SetWindowLong(hWnd, GWL_STYLE, style);
            // SWP_FRAMECHANGED commits the style change and forces WM_NCCALCSIZE before
            // the move so the window doesn't retain non-client metrics from the old frame.
            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0,
                SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);

            // SWP_NOSENDCHANGING skips WM_WINDOWPOSCHANGING, which WinUIEx.WindowEx was
            // intercepting to clamp our width to ~132 DIPs (its content/MinWidth floor).
            if (!SetWindowPos(hWnd, IntPtr.Zero, abd.rc.left, abd.rc.top,
                abd.rc.right - abd.rc.left, abd.rc.bottom - abd.rc.top,
                SWP_NOACTIVATE | SWP_NOZORDER | SWP_NOSENDCHANGING))
            {
                LogWin32Error("SetWindowPos (docked)");
            }

            SHAppBarMessage((int)AppBarMessages.ABM_WINDOWPOSCHANGED, ref abd);
        }

        private static void ApplyThickness(ref RECT rc, ABEdge edge, int thickness)
        {
            switch (edge)
            {
                case ABEdge.Left:   rc.right  = rc.left + thickness; break;
                case ABEdge.Right:  rc.left   = rc.right - thickness; break;
                case ABEdge.Top:    rc.bottom = rc.top + thickness; break;
                case ABEdge.Bottom: rc.top    = rc.bottom - thickness; break;
            }
        }

        private void ApplyAutohide(IntPtr hWnd, ABEdge edge, Monitor targetMonitor, int barSizeScaled)
        {
            // Release any docked work-area reservation that may exist from a prior mode.
            ReleaseDockedReservation(hWnd, edge);

            var mon = targetMonitor.MonitorRect;

            // Register as an autohide appbar on the chosen edge of this monitor.
            var regAbd = new APPBARDATA();
            regAbd.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
            regAbd.hWnd = hWnd;
            regAbd.uEdge = (int)edge;
            regAbd.rc = mon;
            regAbd.lParam = (IntPtr)1;
            IntPtr result = SHAppBarMessage((int)AppBarMessages.ABM_SETAUTOHIDEBAREX, ref regAbd);
            if (result == IntPtr.Zero)
            {
                Debug.WriteLine("ABM_SETAUTOHIDEBAREX failed — edge already owned (e.g. Windows taskbar autohide). Falling back to docked mode.");
                autoHideEnabled = false;
                saveSetting("autohide", false);
                ApplyDocked(hWnd, edge, targetMonitor, barSizeScaled);
                return;
            }
            autohideRegistered = true;
            autohideRegisteredEdge = edge;
            autohideMonitorRect = mon;

            // Compute shown / hidden / trigger rects in physical pixels.
            shownRect = mon;
            switch (edge)
            {
                case ABEdge.Left:   shownRect.right  = shownRect.left + barSizeScaled; break;
                case ABEdge.Right:  shownRect.left   = shownRect.right - barSizeScaled; break;
                case ABEdge.Top:    shownRect.bottom = shownRect.top + barSizeScaled; break;
                case ABEdge.Bottom: shownRect.top    = shownRect.bottom - barSizeScaled; break;
            }

            // Scale the trigger strip by monitor DPI so the hit area stays usable on
            // high-DPI displays (2 physical px is nearly invisible at 200%).
            double scale = targetMonitor.scale > 0 ? targetMonitor.scale : 1.0;
            int triggerPx = Math.Max(1, (int)Math.Round(AutohideTriggerPxUnscaled * scale));

            hiddenRect = shownRect;
            switch (edge)
            {
                case ABEdge.Left:
                    hiddenRect.left  = mon.left - barSizeScaled + triggerPx;
                    hiddenRect.right = hiddenRect.left + barSizeScaled;
                    break;
                case ABEdge.Right:
                    hiddenRect.right = mon.right + barSizeScaled - triggerPx;
                    hiddenRect.left  = hiddenRect.right - barSizeScaled;
                    break;
                case ABEdge.Top:
                    hiddenRect.top    = mon.top - barSizeScaled + triggerPx;
                    hiddenRect.bottom = hiddenRect.top + barSizeScaled;
                    break;
                case ABEdge.Bottom:
                    hiddenRect.bottom = mon.bottom + barSizeScaled - triggerPx;
                    hiddenRect.top    = hiddenRect.bottom - barSizeScaled;
                    break;
            }

            triggerRect = mon;
            switch (edge)
            {
                case ABEdge.Left:   triggerRect.right  = mon.left + triggerPx; break;
                case ABEdge.Right:  triggerRect.left   = mon.right - triggerPx; break;
                case ABEdge.Top:    triggerRect.bottom = mon.top + triggerPx; break;
                case ABEdge.Bottom: triggerRect.top    = mon.bottom - triggerPx; break;
            }

            IntPtr style = GetWindowLong(hWnd, GWL_STYLE);
            style = (IntPtr)(style.ToInt64() & ~(WS_CAPTION | WS_THICKFRAME));
            SetWindowLong(hWnd, GWL_STYLE, style);
            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0,
                SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);

            // SWP_NOSENDCHANGING skips the same WinUIEx MinWidth clamp that ApplyDocked
            // avoids — autohide sizes the window to barSizeScaled on Left/Right too.
            SetWindowPos(hWnd, HWND_TOPMOST,
                hiddenRect.left, hiddenRect.top,
                hiddenRect.right - hiddenRect.left,
                hiddenRect.bottom - hiddenRect.top,
                SWP_NOACTIVATE | SWP_NOSENDCHANGING);
            autohideState = AutohideState.Hidden;
            cursorLeftShownAt = DateTime.MaxValue;

            StartAutohideTimer();
        }

        private void ReleaseDockedReservation(IntPtr hWnd, ABEdge edge)
        {
            var a = new APPBARDATA();
            a.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
            a.hWnd = hWnd;
            a.uEdge = (int)edge;
            // Zero rect tells the shell our docked appbar reserves no work area.
            a.rc.left = 0; a.rc.top = 0; a.rc.right = 0; a.rc.bottom = 0;
            SHAppBarMessage((int)AppBarMessages.ABM_SETPOS, ref a);
        }

        private void UnregisterAutohideIfNeeded()
        {
            if (!autohideRegistered) return;
            StopAutohideTimer();
            var hWnd = WindowNative.GetWindowHandle(this);
            var a = new APPBARDATA();
            a.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
            a.hWnd = hWnd;
            a.uEdge = (int)autohideRegisteredEdge;
            a.rc = autohideMonitorRect;
            a.lParam = IntPtr.Zero;
            SHAppBarMessage((int)AppBarMessages.ABM_SETAUTOHIDEBAREX, ref a);
            autohideRegistered = false;
        }

        // 16 ms (~60 fps) while animating, 100 ms while idle — the idle rate only
        // needs to be tight enough to feel responsive when the cursor hits the trigger.
        private const int AutohideIdleTickMs   = 100;
        private const int AutohideActiveTickMs = 16;

        private void StartAutohideTimer()
        {
            if (autohideTimer == null)
            {
                autohideTimer = new DispatcherTimer();
                autohideTimer.Tick += AutohideTick;
            }
            SetAutohideTimerInterval(AutohideIdleTickMs);
            autohideTimer.Start();
        }

        private void StopAutohideTimer()
        {
            autohideTimer?.Stop();
        }

        private void SetAutohideTimerInterval(int ms)
        {
            if (autohideTimer == null) return;
            var desired = TimeSpan.FromMilliseconds(ms);
            if (autohideTimer.Interval != desired)
                autohideTimer.Interval = desired;
        }

        private void AutohideTick(object sender, object e)
        {
            if (fullscreenAppActive)
            {
                if (autohideState != AutohideState.Hidden)
                {
                    SnapTo(hiddenRect);
                    autohideState = AutohideState.Hidden;
                }
                SetAutohideTimerInterval(AutohideIdleTickMs);
                return;
            }

            if (!GetCursorPos(out POINT p)) return;
            var now = DateTime.UtcNow;
            const double dur = AutohideAnimMs;

            switch (autohideState)
            {
                case AutohideState.Hidden:
                    if (PointInRect(p, triggerRect))
                    {
                        animationStart = now;
                        autohideState = AutohideState.Showing;
                        SetAutohideTimerInterval(AutohideActiveTickMs);
                    }
                    break;

                case AutohideState.Showing:
                {
                    double t = Math.Min(1.0, (now - animationStart).TotalMilliseconds / dur);
                    double eased = 1 - Math.Pow(1 - t, 2);
                    ApplyInterpolatedRect(hiddenRect, shownRect, eased);
                    if (t >= 1.0)
                    {
                        autohideState = AutohideState.Shown;
                        cursorLeftShownAt = DateTime.MaxValue;
                        SetAutohideTimerInterval(AutohideIdleTickMs);
                    }
                    break;
                }

                case AutohideState.Shown:
                    if (PointInRect(p, shownRect))
                    {
                        cursorLeftShownAt = DateTime.MaxValue;
                    }
                    else
                    {
                        if (cursorLeftShownAt == DateTime.MaxValue)
                            cursorLeftShownAt = now;
                        else if ((now - cursorLeftShownAt).TotalMilliseconds > AutohideHideDebounceMs)
                        {
                            animationStart = now;
                            autohideState = AutohideState.Hiding;
                            SetAutohideTimerInterval(AutohideActiveTickMs);
                        }
                    }
                    break;

                case AutohideState.Hiding:
                {
                    double t = Math.Min(1.0, (now - animationStart).TotalMilliseconds / dur);
                    double eased = 1 - Math.Pow(1 - t, 2);
                    ApplyInterpolatedRect(shownRect, hiddenRect, eased);
                    if (t >= 1.0)
                    {
                        autohideState = AutohideState.Hidden;
                        SetAutohideTimerInterval(AutohideIdleTickMs);
                    }
                    break;
                }
            }
        }

        private void ApplyInterpolatedRect(RECT from, RECT to, double t)
        {
            int x = (int)(from.left + (to.left - from.left) * t);
            int y = (int)(from.top + (to.top - from.top) * t);
            int w = from.right - from.left;
            int h = from.bottom - from.top;
            var hWnd = WindowNative.GetWindowHandle(this);
            SetWindowPos(hWnd, HWND_TOPMOST, x, y, w, h, SWP_NOACTIVATE | SWP_NOSENDCHANGING);
        }

        private void SnapTo(RECT r)
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            SetWindowPos(hWnd, HWND_TOPMOST, r.left, r.top,
                r.right - r.left, r.bottom - r.top, SWP_NOACTIVATE | SWP_NOSENDCHANGING);
        }

        private static bool PointInRect(POINT p, RECT r)
            => p.x >= r.left && p.x < r.right && p.y >= r.top && p.y < r.bottom;

        /******************* OnWindowMessageReceived is WndProc****************/
        private void OnWindowMessageReceived(object sender, WindowMessageEventArgs e)
        {
            // AppBar callback notifications (WParam identifies which ABN_ this is).
            if (e.Message.MessageId == uCallBack)
            {
                switch (e.Message.WParam)
                {
                    case (int)ABNotify.ABN_POSCHANGED:
                        relocateWindowLocation((ABEdge)edgeMonitor.SelectedItem);
                        break;

                    case (int)ABNotify.ABN_FULLSCREENAPP:
                        fullscreenAppActive = (long)e.Message.LParam != 0;
                        break;

                    case (int)ABNotify.ABN_STATECHANGE:
                        // System autohide/alwaystop state changed — no action needed for our bar.
                        break;
                }
                return;
            }

            // Explorer restart: re-register the appbar.
            if (taskbarCreatedMsg != 0 && (int)e.Message.MessageId == taskbarCreatedMsg)
            {
                Debug.WriteLine("TaskbarCreated received — re-registering appbar.");
                fBarRegistered = false;
                autohideRegistered = false;
                RegisterAppBar((ABEdge)loadSettings("edge"), (string)loadSettings("monitor"));
                return;
            }

            switch (e.Message.MessageId)
            {
                case WM_ACTIVATE:
                    // The AppBar contract requires forwarding WM_ACTIVATE so the shell can manage z-order.
                    if (fBarRegistered)
                    {
                        var a = new APPBARDATA();
                        a.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
                        a.hWnd = WindowNative.GetWindowHandle(this);
                        SHAppBarMessage((int)AppBarMessages.ABM_ACTIVATE, ref a);
                    }
                    break;

                case WM_WINDOWPOSCHANGED:
                    if (fBarRegistered)
                    {
                        var a = new APPBARDATA();
                        a.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
                        a.hWnd = WindowNative.GetWindowHandle(this);
                        SHAppBarMessage((int)AppBarMessages.ABM_WINDOWPOSCHANGED, ref a);
                    }
                    break;

                case WM_DISPLAYCHANGE:
                    MonitorList = new ObservableCollection<Monitor>(GetMonitorsInfo());
                    // Rebuild our registration on the current edge/monitor (may have been disconnected).
                    if (fBarRegistered) relocateWindowLocation((ABEdge)edgeMonitor.SelectedItem);
                    break;
            }
        }

        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            Debug.WriteLine("Drag Over");
            if (e.DragUIOverride != null)
            {
                e.DragUIOverride.Caption = "Add Shortcut";
                e.DragUIOverride.IsContentVisible = true;
            }
           
        }

        private void DragLeave(object sender, DragEventArgs e)
        {
        }

#region shortcuts
        // Late-bound COM (WScript.Shell) to resolve .lnk → target path. Avoids a
        // type-library-imported COMReference, which tlbimp can't process under
        // `dotnet publish` (the .NET Core MSBuild — see MSB4803).
        private static string ResolveShortcutTarget(string lnkPath)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return null;
            dynamic shell = null, shortcut = null;
            try
            {
                shell = Activator.CreateInstance(shellType);
                shortcut = shell.CreateShortcut(lnkPath);
                return (string)shortcut.TargetPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ResolveShortcutTarget failed: " + ex.Message);
                return null;
            }
            finally
            {
                if (shortcut != null) Marshal.FinalReleaseComObject(shortcut);
                if (shell != null) Marshal.FinalReleaseComObject(shell);
            }
        }

        private async void loadShortCuts()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\shortcuts.txt";
            if (!System.IO.File.Exists(path)) return;

            string[] lines;
            try
            {
                lines = System.IO.File.ReadAllLines(path);
            }
            catch (Exception error)
            {
                Debug.WriteLine("Failed to read shortcuts.txt: " + error.Message);
                return;
            }

            foreach (var exePath in lines)
            {
                if (string.IsNullOrWhiteSpace(exePath)) continue;
                try
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(exePath);
                    await createShortCut(file, exePath);
                }
                catch (Exception error)
                {
                    // One bad entry (missing file, permission denied, etc.) shouldn't abort the rest.
                    Debug.WriteLine($"Skipping shortcut '{exePath}': {error.Message}");
                }
            }
        }

        
        private async void Grid_Drop(object sender, DragEventArgs e)
        {
            Debug.WriteLine("Dropped");
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> files = await e.DataView.GetStorageItemsAsync();
                StorageFile file = files.First() as StorageFile;

                var name = file.Name;
                var path = file.Path;
                var type = file.FileType;

                Debug.WriteLine("File Type = " + type);
                if (type == ".lnk")
                {
                    path = ResolveShortcutTarget(path) ?? path;
                }
                await createShortCut(file, path);
                try
                {
                    // Create a file that the application will store user specific shortcut data in.
                    using (StreamWriter sw = System.IO.File.AppendText(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\shortcuts.txt"))
                        sw.WriteLine(path);
                }
                catch (IOException error)
                {
                    // Inform the user that an error occurred.
                    Debug.WriteLine("An error occurred while attempting to show the application." +
                                    "The error is:" + error.ToString());

                }
               
            }
        }

        private async Task createShortCut(StorageFile aFile, String aPath)
        {
            try
            {
                var iconThumbnail = await aFile.GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem, 32);
                var bi = new BitmapImage() 
                {
                    DecodePixelHeight = 32,
                    DecodePixelWidth = 32,
                };
                bi.SetSource(iconThumbnail);

                Image ButtonImageEL = new Image()
                {
                    Source = bi,
                    Height = 32,
                    Width = 32,
                 };

                Button testIButton = new Button()
                {
                    Background = new SolidColorBrush(Colors.Transparent),
                    BorderBrush = new SolidColorBrush(Colors.Transparent),
                    Content = ButtonImageEL,
                    Tag = aPath,
                 };
                testIButton.Click += Button_Click;


                MenuFlyout menuFlyout = new MenuFlyout();
                MenuFlyoutItem menuFlyoutItem = new MenuFlyoutItem() 
                {
                    Text = "Delete",
                    Tag = testIButton.Tag,
                    Icon = new SymbolIcon(Symbol.Delete),
                 };
               
                menuFlyoutItem.Click += MenuFlyoutItem_Click;
                menuFlyout.Items.Add(menuFlyoutItem);
                testIButton.ContextFlyout = menuFlyout;
                stPanel.Children.Add(testIButton);

                Debug.WriteLine("File info " + aPath);

            }
            catch (Exception error)
            {
                Debug.WriteLine(error.Message);
            }
        }
        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Clicked on Image " + ((Control)sender).Tag.ToString());
            try
            {
                Process.Start(((Control)sender).Tag.ToString());
            }
            catch (Exception error)
            {
                Debug.WriteLine("Error " + error);
            }
           
        }
        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {

            System.IO.File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\shortcuts.txt");
            foreach (var item in stPanel.Children)
            {
                if(item.GetType() == typeof(Button))
                {
                    if (((Button)item).Tag == ((MenuFlyoutItem)sender).Tag)
                    {
                        
                        stPanel.Children.Remove(item);
                    }
                    else
                    {
                        using (StreamWriter sa = System.IO.File.AppendText(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\shortcuts.txt"))
                            sa.WriteLine(((Button)item).Tag.ToString());
                    }
                }
              
            }           

        }
       

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if(webWindow != null)
            {
                webWindow.Close();
            }

            UnregisterAppBar();
            this.Close();
        }

        public void restartAppBar()
        {
            ABSetPos((ABEdge)SettingMethods.loadSettings("edge"), (string)SettingMethods.loadSettings("monitor"));
            //ABSetPos(theSelectedEdge, cbMonitor.SelectedItem as string);

        }
        Settings settingsWindow;

        public void closeSettingsWindow()
        {
            if (settingsWindow != null)
            {
                settingsWindow.Close();
                OpenWindows.Remove(settingsWindow);
                settingsWindow = null;
            }
        }
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            if (settingsWindow == null)
            {
                settingsWindow = new Settings(monitors,uCallBack,this);
                settingsWindow.ExtendsContentIntoTitleBar = true;
                settingsWindow.Activate();
                
                DockToAppBar(settingsWindow);
                OpenWindows.Add(settingsWindow);
            }
            else
            {
                settingsWindow.Close();
                OpenWindows.Remove(settingsWindow);
                settingsWindow = null;
            }
        }
        WindowDetect wappWindow;
        private void DetectWindow_click(object sender, RoutedEventArgs e)
        {
            foreach (var mon in MonitorList)
            {
                var displayNumString = Regex.Match(mon.MonitorName, @"\d+").Value;
               // foreach (var monitor in monitorInfo)
               // {
//if (mon.MonitorName == mon)
                   // {
                        RECT workarea = mon.WorkRect;
                        wappWindow = new WindowDetect(displayNumString);
                        wappWindow.ExtendsContentIntoTitleBar = true;
                        var dwindow = wappWindow.GetAppWindow();
                        wappWindow.Show();
                        //windowDetect.Show

                        dwindow.MoveAndResize(new Windows.Graphics.RectInt32(workarea.right - dwindow.Size.Width - 50, workarea.bottom - (dwindow.Size.Height + 50), dwindow.Size.Width, dwindow.Size.Height));

                   // }

                //}
                //var workarea = getMonitorWorkRect(mon);

                          }
        }

        // Dock-menu handlers — set Edge in settings, sync the (still-present) edge
        // ComboBox via its SelectionChanged path, and reposition the AppBar.
        private void DockTop_Click(object sender, RoutedEventArgs e)    => SetEdge(ABEdge.Top);
        private void DockRight_Click(object sender, RoutedEventArgs e)  => SetEdge(ABEdge.Right);
        private void DockBottom_Click(object sender, RoutedEventArgs e) => SetEdge(ABEdge.Bottom);
        private void DockLeft_Click(object sender, RoutedEventArgs e)   => SetEdge(ABEdge.Left);

        private void SetEdge(ABEdge edge)
        {
            saveSetting("edge", (int)edge);
            Edge = edge;
            // Assigning SelectedItem fires edgeComboBox_SelectionChanged which
            // does the actual ABSetPos call — no need to call relocateWindowLocation
            // here. If the value is already selected, force the relocate manually.
            if (edgeMonitor.SelectedItem is ABEdge cur && cur == edge)
                relocateWindowLocation(edge);
            else
                edgeMonitor.SelectedItem = edge;
        }

        private void relocateWindowLocation(ABEdge theSelectedEdge)
        {
            Debug.WriteLine("This is the edge var "+Edge);


              ABSetPos(theSelectedEdge, loadSettings("monitor") as string);
            if (Edge == ABEdge.Top || Edge == ABEdge.Bottom)
            {
                Debug.WriteLine("Edge Selection " + Edge);

                stPanel.Orientation = Orientation.Horizontal;
            }
            else
            {
                stPanel.Orientation = Orientation.Vertical;
            }

           
            if (webWindow != null)
            {
                DockToAppBar(webWindow);
            }
        }
        private void edgeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           Edge = ((ABEdge)edgeMonitor.SelectedItem);
            Debug.WriteLine("This is the selecteditem edge "+Edge);
            relocateWindowLocation((ABEdge)edgeMonitor.SelectedItem);
            Debug.WriteLine("Edge Selection Changed********** "+ Edge);
           
        }
        WebWindow webWindow;
        private void openWebWindow(object sender, RoutedEventArgs e)
        {
            if (webWindow == null)
            {
                webWindow = new WebWindow();
                DockToAppBar(webWindow);
                webWindow.Activate();
                OpenWindows.Add(webWindow);
                
            }
            else
            {
                webWindow.Close();
                OpenWindows.Remove(webWindow);
                webWindow = null;
            }
           
        }

        void DockToAppBar(Window webW)
        {
            var wappWindow = webW.GetAppWindow();
            bool isSettings = wappWindow.Title == "Settings";

            Monitor mon = null;
            var savedMonitor = loadSettings("monitor") as string;
            foreach (var m in monitorInfo)
                if (m.MonitorName == savedMonitor) { mon = m; break; }
            if (mon == null) return;

            int x, y, w, h;

            if (isSettings)
            {
                // Settings docks adjacent to the main appbar — it keeps its own size and
                // sits just outside the bar's dock edge, centered along the bar's length.
                w = wappWindow.Size.Width;
                h = wappWindow.Size.Height;
                var abPos = appWindow.Position;
                var abSize = appWindow.Size;

                switch (Edge)
                {
                    case ABEdge.Top:
                        x = abPos.X + (abSize.Width - w) / 2;
                        y = abPos.Y + abSize.Height;
                        break;
                    case ABEdge.Bottom:
                        x = abPos.X + (abSize.Width - w) / 2;
                        y = abPos.Y - h;
                        break;
                    case ABEdge.Left:
                        x = abPos.X + abSize.Width;
                        y = abPos.Y + (abSize.Height - h) / 2;
                        break;
                    case ABEdge.Right:
                    default:
                        x = abPos.X - w;
                        y = abPos.Y + (abSize.Height - h) / 2;
                        break;
                }
            }
            else
            {
                // Web window fills the work area of the selected monitor.
                var workarea = mon.WorkRect;
                x = workarea.left;
                y = workarea.top;
                w = workarea.right - workarea.left;
                h = workarea.bottom - workarea.top;
            }

            webW.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, w, h));
        }

        private void appbarWindow_Closed(object sender, WindowEventArgs args)
        {
            // Safety net: ensure appbar/autohide registrations are released even if
            // the window is closed via an OS-level action (not the Close button).
            UnregisterAppBar();

            if (monitor != null)
            {
                monitor.WindowMessageReceived -= OnWindowMessageReceived;
                monitor.Dispose();
                monitor = null;
            }

            foreach (var window in OpenWindows)
            {
                if (window != null) window.Close();
            }
        }




    }
}

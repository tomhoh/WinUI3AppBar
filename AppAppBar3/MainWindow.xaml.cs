using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Runtime.InteropServices;
using WinRT.Interop;
using Windows.Graphics.Display;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using System.Threading.Tasks;
using Windows.Devices.Display;
using Windows.Devices.Enumeration;
using Windows.Graphics;
using System.Diagnostics;
using System.Threading;
using System.Collections.ObjectModel;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.ViewManagement;



// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AppAppBar3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {
        private String[] _MonItems;// = new String[10];
        private ObservableCollection<string> _MonitorList; 

        public ObservableCollection<string> MonitorList
        {
            get => _MonitorList;
            set
            {
                _MonitorList = value;
                OnPropertyChanged();
            }
        }

        private string _Edge;

        public string Edge
        {
            get => _Edge;
            set
            {
                _Edge = value;
                OnPropertyChanged();
            }
        }
        public string[] MonItems() { return _MonItems; }
        public List<string> monitors; 
        private const uint ABM_NEW = 0x00000000;
        private const uint ABM_REMOVE = 0x00000001;
        private const uint ABM_QUERYPOS = 0x00000002;
        private const uint ABM_SETPOS = 0x00000003;
        private const uint ABM_GETSTATE = 0x00000004;
        private const uint ABM_GETTASKBARPOS = 0x00000005;

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);
        public const int GWL_STYLE = -16;
        public const int WS_CAPTION = 0x00C00000;
        public const int WS_THICKFRAME = 0x00040000;

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    
    public struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public IntPtr lParam;
        }
        public enum AppBarMessages
        {
            New = 0x00000000,
            Remove = 0x00000001,
            QueryPos = 0x00000002,
            SetPos = 0x00000003,
            // Define other messages as needed
        }
        public enum ABEdge
        {
            Left = 0,
            Top = 1,
            Right = 2,
            Bottom = 3
        }
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
        private AppWindow appWindow;
        public MainWindow()
        {
            this.InitializeComponent();
            this.Activated += OnActivated;
            this.Closed += OnClosed;
            this.AppWindow.IsShownInSwitchers = false;
           



            // = getMonitors();

        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Debug.WriteLine("MonitorList changed*****" + propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnActivated(object sender, WindowActivatedEventArgs args)
        {
           
            cbMonitor.DataContext = this;
            edgeMonitor.DataContext = this;
            monitors = MonitorHelper.GetMonitors();
            // Debug.WriteLine("Monitor List*****"+monitors);
           // MonitorList = new ObservableCollection<string>();
            foreach (var monitor in monitors)
            {
              // MonitorList.Add(monitor);
                Debug.WriteLine(monitor);
               // Debug.WriteLine("Monitor List*****" + MonitorList);
                // _MonItems[0] = monitor;
            }
            // If you specifically need an array:
           // _MonItems = monitors.ToArray();
            MonitorList = new ObservableCollection<string>(monitors);

            if (appWindow == null)
            {
                IntPtr hWnd = WindowNative.GetWindowHandle(this);
                WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
                appWindow = AppWindow.GetFromWindowId(windowId);
                // Eventhough we removed the title bar with win32 api, we still need to set the title bar properties to make the content
                // move into window title area
                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
               // appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
               // appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
               // appWindow.TitleBar.ButtonForegroundColor = Colors.Transparent;
                if (appWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.IsResizable = false;
                    presenter.IsMaximizable = false;
                    presenter.IsMinimizable = false;
                   
                }

                // Ensure we only register the app bar once
                if (args.WindowActivationState != WindowActivationState.Deactivated)
                {
                    
                    RegisterAppBar(ABEdge.Top);
                    Edge = "Top";
                    // Optionally, unsubscribe from Activated event after first activation
                    this.Activated -= OnActivated;
                }

            }
            
           
        }
        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        const int SM_CXSCREEN = 0;  // Index for screen width in pixels
        const int SM_CYSCREEN = 1;  // Index for screen height in pixels

        private void RegisterAppBar(ABEdge edge)
        {
            
            var hWnd = WindowNative.GetWindowHandle(this);
            
            int screenWidth = (int)(GetSystemMetrics(SM_CXSCREEN) );
            int screenHeight = (int)(GetSystemMetrics(SM_CYSCREEN) );
            
            
            var abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = hWnd,
                uCallbackMessage = 0, // Use a WM_USER range message for handling AppBar messages
                uEdge = (uint)edge, // Can be Left, Top, Right, Bottom
            };

            RECT rc;

            switch (edge)
            {
                case ABEdge.Left:
                
                    rc.left = 0;
                    rc.top = 0;
                    rc.right = 100; // Width of the AppBar
                    rc.bottom = screenHeight; // Height of the AppBar
                    break;
                case ABEdge.Right:
                    rc.left = screenWidth - 100;
                    rc.top = 0;
                    rc.right = screenWidth; // Width of the AppBar
                    rc.bottom = screenHeight; // Height of the AppBar
                    break;
                case ABEdge.Top:
                    rc.left = 0;
                    rc.top = 0;
                    rc.right = screenWidth; // Width of the AppBar
                    rc.bottom = 100; // Height of the AppBar
                    break;
                case ABEdge.Bottom:
                    rc.left = 0;
                    rc.top = screenHeight - 100;
                    rc.right = screenWidth; // Width of the AppBar
                    rc.bottom = screenHeight; // Height of the AppBar
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(edge));
            }

           // rc.left = 0;
           // rc.top = 0;
           // rc.right = screenWidth; // Width of the AppBar
           // rc.bottom = 100; // Height of the AppBar
            abd.rc = rc;

            SHAppBarMessage(ABM_NEW, ref abd);
            SHAppBarMessage(ABM_QUERYPOS, ref abd);
            SHAppBarMessage(ABM_SETPOS, ref abd);
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
           
            //remove corner radius by removing border and caption
            IntPtr style = GetWindowLong(hwnd, GWL_STYLE);
            style = (IntPtr)(style.ToInt64() & ~(WS_CAPTION | WS_THICKFRAME));
            SetWindowLong(hwnd, GWL_STYLE, style);

            //set window size and position to appbar
           // SetWindowPos(hWnd, IntPtr.Zero, abd.rc.left, abd.rc.top, abd.rc.right - abd.rc.left, abd.rc.bottom - abd.rc.top, SWP_NOZORDER | SWP_NOACTIVATE | WS_EX_TOOLWINDOW | WS_VISIBLE);
            SetWindowPos(hWnd, IntPtr.Zero, abd.rc.left, abd.rc.top, abd.rc.right - abd.rc.left, abd.rc.bottom - abd.rc.top, SWP_NOZORDER | SWP_NOACTIVATE );

        }
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint WS_EX_TOOLWINDOW = 0x00000080;
        const uint WS_VISIBLE = 0x10000000;


        private void OnClosed(object sender, WindowEventArgs args)
        {
            UnregisterAppBar();
        }

        private void UnregisterAppBar()
        {
            var abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = WindowNative.GetWindowHandle(this)
            };
            SHAppBarMessage(ABM_REMOVE, ref abd);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DisplayComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private async Task getMonitors()
        {
           
            Debug.WriteLine("Monitor List**********" );
            var displayList = await DeviceInformation.FindAllAsync
                              (DisplayMonitor.GetDeviceSelector());

            if (!displayList.Any())
                return;
            foreach (var display in displayList)
            {
                var monitorInfo = await DisplayMonitor.FromInterfaceIdAsync(display.Id);
                Debug.WriteLine("Monitor ID**********" + display.Name);
                Debug.WriteLine("Monitor Properites**********" + display.Properties.ToList().ToString());
                Debug.WriteLine("Monitor ID**********" + display.Id);
                Debug.WriteLine("Monitor List**********" + monitorInfo.DisplayName);
                _MonItems[0] = (monitorInfo.DisplayName);
                
            }

            Debug.WriteLine("This is the items array" + _MonItems[0]);



        }

        private void edgeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("Edge Selection Changed********** "+ Edge);
            if (Edge == "Top")
            {
                Debug.WriteLine("Edge Selection Top " + Edge);
                RegisterAppBar(ABEdge.Top);
                stPanel.Orientation = Orientation.Horizontal;
            }
            else if (Edge == "Bottom")
            {
                RegisterAppBar(ABEdge.Bottom);
                stPanel.Orientation = Orientation.Horizontal;
            }
            else if (Edge == "Left")
            {
                RegisterAppBar(ABEdge.Left);
                stPanel.Orientation = Orientation.Vertical;
            }
            else if (Edge == "Right")
            {
                RegisterAppBar(ABEdge.Right);
                stPanel.Orientation = Orientation.Vertical;
            }
        }
        WebWindow webWindow;
        private void openWebWindow(object sender, RoutedEventArgs e)
        {
            if (webWindow == null)
            {
                webWindow = new WebWindow();
                webWindow.Activate();
                DockToAppBar(webWindow);
            }
            else
            {
                webWindow.Close();
                webWindow = null;
            }
            
          
           
        }

        void DockToAppBar(WebWindow webW)
        {
            
            var windowBounds = webW.Bounds;
            var taskbarRect = this.Bounds;
            //var screenRect = Windows.UI.Core.CoreWindow.GetForCurrentThread().Bounds;
            int screenWidth = (int)(GetSystemMetrics(SM_CXSCREEN));
            int screenHeight = (int)(GetSystemMetrics(SM_CYSCREEN));
            var workarea = MonitorHelper.GetWorkArea();

            double appBarWidth = taskbarRect.Width;
            double appBarHeight = taskbarRect.Height;
            int newWindowWidth = 0;// = screenWidth;
            int newWindowHeight =0;// = screenHeight - 100;
            int newWindowX=0;//= (int)(taskbarRect.X);
            int newWindowY=0;//= 100;
            if (Edge == "Top")
            {
                newWindowWidth = screenWidth;
                newWindowHeight = workarea.bottom - 100;
                newWindowX = (int)(taskbarRect.X);
                newWindowY = 100;

            }
            else if (Edge == "Bottom")
            {
                newWindowWidth = screenWidth;
                newWindowHeight = screenHeight - 100;
                newWindowX = 0;
                newWindowY = 0;
            }
            else if (Edge == "Left")
            {
                newWindowWidth = screenWidth-100;
                newWindowHeight = screenHeight;
                newWindowX = 100;
                newWindowY = 0;

            }
            else if (Edge == "Right")
            {
                newWindowWidth = screenWidth - 100;
                newWindowHeight = screenHeight;
                newWindowX = 0;
                newWindowY = 0;
            }
            webW.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(newWindowX, newWindowY, newWindowWidth, newWindowHeight));
           // webW = new Rect(newWindowX,newWindowY,newWindowWidth,newWindowHeight);
            
        }
    }
}

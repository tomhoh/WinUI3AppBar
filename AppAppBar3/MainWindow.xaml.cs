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
using static AppAppBar3.MonitorHelper;



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
        int theSelectedIndex = 0;
        string selectedItemsText;

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

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hWnd);


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
            selectedItemsText = @"\\.\DISPLAY1";

            if (appWindow == null)
            {
                IntPtr hWnd = WindowNative.GetWindowHandle(this);
                WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
                appWindow = AppWindow.GetFromWindowId(windowId);
                SizeInt32 size = new SizeInt32();
                size.Width = 0;
                size.Height = 0;
                appWindow.Resize(size);
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
                   // presenter.Minimize();
                   
                  
                   
                }

                // Ensure we only register the app bar once
                if (args.WindowActivationState != WindowActivationState.Deactivated)
                {
                    
                   // RegisterAppBar(ABEdge.Top);
                    Edge = "Top";
                    // Optionally, unsubscribe from Activated event after first activation
                    this.Activated -= OnActivated;
                }
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
                cbMonitor.SelectedIndex = 0;
            }
            
           
        }
        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        const int SM_CXSCREEN = 0;  // Index for screen width in pixels
        const int SM_CYSCREEN = 1;  // Index for screen height in pixels

       

        APPBARDATA abd;
        private void RegisterAppBar(ABEdge edge, string selectedMonitor)
        {

            

            var workarea = MonitorHelper.GetWorkArea();
            var hWnd = WindowNative.GetWindowHandle(this);
            uint dpiX, dpiY;
            dpiX = dpiY = GetDpiForWindow(hWnd);
            //GetDpiForMonitor(hWnd, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
            // int screenWidth = (int)(GetSystemMetrics(SM_CXSCREEN) );
            // int screenHeight = (int)(GetSystemMetrics(SM_CYSCREEN) );

            if (SHAppBarMessage(ABM_GETSTATE, ref abd) != IntPtr.Zero)
            {
                // Unregister the AppBar
                Debug.WriteLine("Removed appbar before new edge");
                SHAppBarMessage(ABM_REMOVE, ref abd);
                

            }
            abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = hWnd,
                uCallbackMessage = 0, // Use a WM_USER range message for handling AppBar messages
                uEdge = (uint)edge, // Can be Left, Top, Right, Bottom
            };
           
            var wrc = MonitorHelper.getMonitorRect(selectedMonitor);
            Debug.WriteLine("Monitor Width*****" + (wrc.right - wrc.left));
            Debug.WriteLine("Monitor Left*****" + wrc.left);
            Debug.WriteLine("Monitor Top*****" + wrc.top);
            Debug.WriteLine("Monitor Bottom*****" + wrc.bottom);


            RECT rc = new RECT();
            switch (edge)
            {
                case ABEdge.Left:
                
                    rc.left = wrc.left;
                    rc.top = wrc.top;
                    rc.right = rc.left + 100; // Width of the AppBar
                    rc.bottom = wrc.bottom; // Height of the AppBar
                    break;
                case ABEdge.Right:
                    
                    rc.top = wrc.top;
                    rc.right = wrc.right; // Width of the AppBar
                    rc.bottom = wrc.bottom; // Height of the AppBar
                    rc.left = wrc.right - 100;
                    break;
                case ABEdge.Top:
                    rc.left = wrc.left;
                    rc.top = wrc.top;
                    rc.right =wrc.right; // Width of the AppBar
                    rc.bottom = wrc.top + 100; // Height of the AppBar
                    break;
                case ABEdge.Bottom:
                    rc.left = wrc.left;
                    rc.top = wrc.bottom- 100;
                    rc.right = wrc.right; // Width of the AppBar
                    rc.bottom = wrc.bottom; // Height of the AppBar
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
            //IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
           
            //remove corner radius by removing border and caption
            IntPtr style = GetWindowLong(hWnd, GWL_STYLE);
            style = (IntPtr)(style.ToInt64() & ~(WS_CAPTION | WS_THICKFRAME));
            SetWindowLong(hWnd, GWL_STYLE, style);
            Debug.WriteLine("combobox selected item TEXT***" + selectedItemsText);
            //set window size and position to appbar
            // SetWindowPos(hWnd, IntPtr.Zero, abd.rc.left, abd.rc.top, abd.rc.right - abd.rc.left, abd.rc.bottom - abd.rc.top, SWP_NOZORDER | SWP_NOACTIVATE | WS_EX_TOOLWINDOW | WS_VISIBLE);
            SetWindowPos(hWnd, IntPtr.Zero, abd.rc.left, abd.rc.top, (int)(abd.rc.right-abd.rc.left*dpiX/96), (int)(abd.rc.bottom-abd.rc.top*dpiY/96), SWP_NOZORDER | SWP_NOACTIVATE );
            
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
            //var abd = new APPBARDATA
            //{
               // cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
               // hWnd = WindowNative.GetWindowHandle(this)
           // };
           if(abd.hWnd != IntPtr.Zero)
            SHAppBarMessage(ABM_REMOVE, ref abd);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            UnregisterAppBar();
            this.Close();
        }

        private void DisplayComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("Monitor selection changed");
           // if((cbMonitor.SelectedItem as ComboBoxItem) != null)
           // {
                selectedItemsText = (cbMonitor.SelectedItem as String);
                //theSelectedIndex = cbMonitor.SelectedIndex;
                Debug.WriteLine("Selected Monitor Text**********" + (cbMonitor.SelectedItem as string));
                RegisterAppBar(ABEdge.Top, cbMonitor.SelectedItem as string);
         
            //Edge = "Top";
            // }

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
            Debug.WriteLine("CB TEXT********** " + cbMonitor.SelectedItem as string);
           //UnregisterAppBar();
            if (Edge == "Top")
            {
                Debug.WriteLine("Edge Selection Top " + Edge);
                RegisterAppBar(ABEdge.Top, cbMonitor.SelectedItem as string);
                stPanel.Orientation = Orientation.Horizontal;
            }
            else if (Edge == "Bottom")
            {
                RegisterAppBar(ABEdge.Bottom, cbMonitor.SelectedItem as string);
                stPanel.Orientation = Orientation.Horizontal;
            }
            else if (Edge == "Left")
            {
                //UnregisterAppBar();
                RegisterAppBar(ABEdge.Left, cbMonitor.SelectedItem as string);
                stPanel.Orientation = Orientation.Vertical;
            }
            else if (Edge == "Right")
            {
                RegisterAppBar(ABEdge.Right, cbMonitor.SelectedItem as string);
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
                newWindowWidth = workarea.right;
                newWindowHeight = workarea.bottom;
                newWindowX = (int)(taskbarRect.X);
                newWindowY = 100;

            }
            else if (Edge == "Bottom")
            {
                newWindowWidth = workarea.right;
                newWindowHeight = workarea.bottom;
                newWindowX = 0;
                newWindowY = 0;
            }
            else if (Edge == "Left")
            {
                newWindowWidth = workarea.right;
                newWindowHeight = workarea.bottom;
                newWindowX = 100;
                newWindowY = 0;

            }
            else if (Edge == "Right")
            {
                newWindowWidth = workarea.right;
                newWindowHeight = screenHeight;
                newWindowX = 0;
                newWindowY = 0;
            }
            webW.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(newWindowX, newWindowY, newWindowWidth, newWindowHeight));
           // webW = new Rect(newWindowX,newWindowY,newWindowWidth,newWindowHeight);
            
        }
    }
}

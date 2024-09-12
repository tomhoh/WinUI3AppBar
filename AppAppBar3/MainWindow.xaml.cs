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
using Microsoft.Graphics.Display;
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
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Microsoft.Win32;
using Microsoft.UI.Dispatching;
using Windows.System;
using WinUIEx.Messaging;
using static AppAppBar3.MainWindow;
using Microsoft.UI.Composition;



// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AppAppBar3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WinUIEx.WindowEx, INotifyPropertyChanged
    {
        private String[] _MonItems;// = new String[10];
        private ObservableCollection<string> _MonitorList; 
        int theSelectedIndex = 0;
        string selectedItemsText;
        uint dpiX, dpiY;
        double scale;
        WindowMessageMonitor monitor; 
        // RECT currentWorkarea;


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
        private bool fBarRegistered = false;
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

         [DllImport("User32.dll", CharSet=CharSet.Auto)]
        private static extern int RegisterWindowMessage(string msg);
        private int uCallBack;

        [DllImport("User32.dll", ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int cx, int cy, bool repaint);

        public struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uCallbackMessage;
            public int uEdge;
            public RECT rc;
            public int lParam;
        }
        /*public enum AppBarMessages
        {
            New = 0x00000000,
            Remove = 0x00000001,
            QueryPos = 0x00000002,
            SetPos = 0x00000003,
            STATECHANGE = 0x00000032,
            // Define other messages as needed
        }*/
        enum AppBarMessages : int
        {
            ABM_NEW = 0,
            ABM_REMOVE = 1,
            ABM_QUERYPOS = 2,
            ABM_SETPOS = 3,
            ABM_GETSTATE = 4,
            ABM_GETTASKBARPOS = 5,
            ABM_ACTIVATE = 6,
            ABM_GETAUTOHIDEBAR = 7,
            ABM_SETAUTOHIDEBAR = 8,
            ABM_WINDOWPOSCHANGED = 9,
            ABM_SETSTATE = 10
        }
        enum ABNotify : int
        {
            ABN_STATECHANGE = 0,
            ABN_POSCHANGED,
            ABN_FULLSCREENAPP,
            ABN_WINDOWARRANGE
        }

        public enum ABEdge : int
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

            monitor = new WindowMessageMonitor(this);
            monitor.WindowMessageReceived += OnWindowMessageReceived;



        }
       


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Debug.WriteLine("MonitorList changed*****" + propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        //Microsoft.UI.Dispatching.DispatcherQueueTimer wndProcTimer;
        private void OnActivated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
        { 
            cbMonitor.DataContext = this;
            edgeMonitor.DataContext = this;
            selectedItemsText = @"\\.\DISPLAY1";

            if (appWindow == null)
            {
               //scale = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
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
                
                dpiX = dpiY = GetDpiForWindow(hWnd);

               // if (appWindow.Presenter is OverlappedPresenter presenter)
               // {
                   // presenter.IsResizable = false;
//presenter.IsMaximizable = false;
                 //   presenter.IsMinimizable = false;
                   
              //  }

                // Ensure we only register the app bar once
                if (args.WindowActivationState != WindowActivationState.Deactivated)
                {
                    RegisterBar(ABEdge.Top,cbMonitor.SelectedItem as string);
                    //RegisterAppBar(ABEdge.Top);
                    edgeMonitor.SelectionChanged -= edgeComboBox_SelectionChanged;
                    Edge = "Top";
                    
                    // Optionally, unsubscribe from Activated event after first activation
                    this.Activated -= OnActivated;
                }
                monitors = MonitorHelper.GetMonitors();
             
                foreach (var monitor in monitors)
                {
                    // MonitorList.Add(monitor);
                    Debug.WriteLine(monitor);
                    // Debug.WriteLine("Monitor List*****" + MonitorList);
                    // _MonItems[0] = monitor;
                }
                
                MonitorList = new ObservableCollection<string>(monitors);
               
                cbMonitor.SelectedIndex = 0;

            }
           

        }

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        const int SM_CXSCREEN = 0;  // Index for screen width in pixels
        const int SM_CYSCREEN = 1;  // Index for screen height in pixels

       

       APPBARDATA abd;

        private void RegisterBar(ABEdge edge, string selectedMonitor)
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = hWnd;
            if (!fBarRegistered)
            {
                uCallBack = RegisterWindowMessage("AppBarMessage");
                abd.uCallbackMessage = uCallBack;

                SHAppBarMessage((int)AppBarMessages.ABM_NEW, ref abd);
                fBarRegistered = true;

                ABSetPos(edge,selectedMonitor);
            }
            else
            {
                
                SHAppBarMessage((int)AppBarMessages.ABM_REMOVE, ref abd);
                //ABSetPos(edge, selectedMonitor);
                fBarRegistered = false;
            }
        }
        private const int ABS_AUTOHIDE = 0x00000001;

        private const int HWND_TOPMOST = -1;
        private const int HWND_NOTOPMOST = -2;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOSIZE = 0x0001;
        public const int SWP_ASYNCWINDOWPOS = 0x4000;
        private void ABSetPos(ABEdge edge, string selectedMonitor)
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
            abd.hWnd = hWnd;
            abd.uEdge = (int)edge;
            
            //var workarea = MonitorHelper.GetWorkArea();
            //abd.lParam = 0;
            var wrc = MonitorHelper.getMonitorRECT(selectedMonitor);
            
            abd.rc.top = wrc.top;
              abd.rc.bottom = wrc.bottom;
              abd.rc.left = wrc.left;
              abd.rc.right = wrc.right;
            SHAppBarMessage((int)AppBarMessages.ABM_QUERYPOS, ref abd);
           

            // Query the system for an approved size and position. 
           

            // Adjust the rectangle, depending on the edge to which the 
            // appbar is anchored. 
           
             switch (abd.uEdge)
             {
                 case (int)ABEdge.Left:
                     abd.rc.right = abd.rc.left +100;
                     break;
                 case (int)ABEdge.Right:
                    abd.rc.left = abd.rc.right - 100;
                    Debug.WriteLine("the left side " + abd.rc.left +" the right side "+abd.rc.right + " dpix "+dpiX);
                     break;
                 case (int)ABEdge.Top:
                     abd.rc.bottom = abd.rc.top + 100;
                     break;
                 case (int)ABEdge.Bottom:
                    abd.rc.top = abd.rc.bottom - 100;
                     break;
             }
          
            // Pass the final bounding rectangle to the system. 
            //remove corner radius by removing border and caption
            IntPtr style = GetWindowLong(hWnd, GWL_STYLE);
            style = (IntPtr)(style.ToInt64() & ~(WS_CAPTION | WS_THICKFRAME | SWP_NOZORDER | SWP_NOACTIVATE));
           // style = (IntPtr)(style.ToInt64() & ~(WS_CAPTION | WS_THICKFRAME ));

            SetWindowLong(hWnd, GWL_STYLE, style);
            // Move and size the appbar so that it conforms to the 
            // bounding rectangle passed to the system. 
            
           
            SHAppBarMessage((int)AppBarMessages.ABM_SETPOS, ref abd);
            Debug.WriteLine("abd right "+abd.rc.right);
            Debug.WriteLine("abd Left " + abd.rc.left);
            Debug.WriteLine("abd top " + abd.rc.top);
            Debug.WriteLine("abd bottom " + abd.rc.bottom);
            
           // Debug.WriteLine("Window width " + (int)((abd.rc.right - abd.rc.left) * dpiX / 96.0f));
            Debug.WriteLine("Window width " + (abd.rc.right - abd.rc.left));
             // SizeInt32 size = new SizeInt32();
            // size.Width = 100;
            //size.Height = abd.rc.bottom - abd.rc.top;
            // appWindow.Resize(size);
            MoveWindow(hWnd, abd.rc.left, abd.rc.top, (abd.rc.right - abd.rc.left), (abd.rc.bottom - abd.rc.top), true);
           // SetWindowPos(hWnd, (IntPtr)HWND_NOTOPMOST, 0, 0, 100, abd.rc.bottom - abd.rc.top, SWP_ASYNCWINDOWPOS);
           
            Debug.WriteLine("Actual Window width " + appWindow.Size.Width);
            // SHAppBarMessage((int)AppBarMessages.ABM_SETSTATE, ref abd); // Set to autohide
            SHAppBarMessage((int)AppBarMessages.ABM_WINDOWPOSCHANGED, ref abd);
        }

        private void OnWindowMessageReceived(object sender, WindowMessageEventArgs e)
        {
            //Debug.WriteLine("*************Message receieved********** " + e.Message.ToString());


            if (e.Message.MessageId == uCallBack)
            {
               // Debug.WriteLine("*************Message receieved in callback********** " + e.Message.ToString());
                switch (e.Message.WParam)
                {
                     
                    case (int)ABNotify.ABN_POSCHANGED:
                        Debug.WriteLine("*************Message receieved in callback********** " + e.Message.ToString());
                        monitor.WindowMessageReceived -= OnWindowMessageReceived;
                        relocateWindowLocation();
                        monitor.WindowMessageReceived += OnWindowMessageReceived;
                        /* switch (edgeMonitor.SelectedItem as string)
                         {
                             case "Left":
                                 ABSetPos(ABEdge.Left, cbMonitor.SelectedItem as string);
                                 break;
                             case "Right":
                                 ABSetPos(ABEdge.Right, cbMonitor.SelectedItem as string);
                                 break;
                             case "Top":
                                 Debug.WriteLine("*************Message receieved in callback********** " + e.Message.ToString());
                                 ABSetPos(ABEdge.Top, cbMonitor.SelectedItem as string);
                                 break;
                             case "Bottom":
                                 ABSetPos(ABEdge.Bottom, cbMonitor.SelectedItem as string);
                                 break;
                         }   */

                        break;
                }
            }

        }

      /*  private void RegisterAppBar(ABEdge edge, string selectedMonitor)
        {
           
           // DisplayInformation.GetForCurrentView().DpiChanged += DisplayInformation_DpiChanged;
            //var workarea = MonitorHelper.GetWorkArea();
          

            var hWnd = WindowNative.GetWindowHandle(this);
          

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
                uEdge = (int)edge, // Can be Left, Top, Right, Bottom
            };
           
            var wrc = MonitorHelper.getMonitorRect(selectedMonitor);
           // currentWorkarea = new RECT();
           // currentWorkarea.left = wrc.left;
           // currentWorkarea.top = wrc.top;
           // currentWorkarea.right = wrc.right;
          //  currentWorkarea.bottom = wrc.bottom;

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
                    rc.left = wrc.right - 100;// Width of the AppBar
                    rc.top = wrc.top;
                    rc.right = wrc.right; 
                    rc.bottom = wrc.bottom; // Height of the AppBar

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

           
            abd.rc = rc;
            
            SHAppBarMessage(ABM_NEW, ref abd);
            SHAppBarMessage(ABM_QUERYPOS, ref abd);
            SHAppBarMessage(ABM_SETPOS, ref abd);
           
            //remove corner radius by removing border and caption
            IntPtr style = GetWindowLong(hWnd, GWL_STYLE);
            style = (IntPtr)(style.ToInt64() & ~(WS_CAPTION | WS_THICKFRAME));
            SetWindowLong(hWnd, GWL_STYLE, style);
            Debug.WriteLine("combobox selected item TEXT***" + selectedItemsText);
            //set window size and position to appbar
            // SetWindowPos(hWnd, IntPtr.Zero, abd.rc.left, abd.rc.top, abd.rc.right - abd.rc.left, abd.rc.bottom - abd.rc.top, SWP_NOZORDER | SWP_NOACTIVATE | WS_EX_TOOLWINDOW | WS_VISIBLE);
            SetWindowPos(hWnd, IntPtr.Zero, abd.rc.left, abd.rc.top, (int)(abd.rc.right-abd.rc.left*dpiX/96), (int)(abd.rc.bottom-abd.rc.top*dpiY/96), SWP_NOZORDER | SWP_NOACTIVATE );
         
        }*/

       
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint WS_EX_TOOLWINDOW = 0x00000080;
        const uint WS_VISIBLE = 0x10000000;


        private void OnClosed(object sender, WindowEventArgs args)
        {
           // UnregisterAppBar();
        }

        private void UnregisterAppBar()
        {
            RegisterBar(ABEdge.Top, cbMonitor.SelectedItem as string);
            // if(abd.hWnd != IntPtr.Zero)
            //  SHAppBarMessage(ABM_REMOVE, ref abd);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

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

        private void DisplayComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            edgeMonitor.SelectionChanged -= edgeComboBox_SelectionChanged;
            Debug.WriteLine("Monitor selection changed");
            relocateWindowLocation();
            edgeMonitor.SelectionChanged += edgeComboBox_SelectionChanged;
            
            selectedItemsText = (cbMonitor.SelectedItem as String);
               
                Debug.WriteLine("Selected Monitor Text**********" + (cbMonitor.SelectedItem as string));
               


        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {

        }

      /*  private async Task getMonitors()
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
               // _MonItems[0] = (monitorInfo.DisplayName);
                
            }

            //Debug.WriteLine("This is the items array" + _MonItems[0]);



        }*/
        private void relocateWindowLocation()
        {
            Debug.WriteLine("This is the edge var "+Edge);
          // RegisterBar(ABEdge.Top, cbMonitor.SelectedItem as string);
            if (Edge == "Top")
            {
                Debug.WriteLine("Edge Selection Top " + Edge);

                ABSetPos(ABEdge.Top, cbMonitor.SelectedItem as string);
                stPanel.Orientation = Orientation.Horizontal;
            }
            else if (Edge == "Bottom")
            {
                ABSetPos(ABEdge.Bottom, cbMonitor.SelectedItem as string);
                stPanel.Orientation = Orientation.Horizontal;
            }
            else if (Edge == "Left")
            {
                //UnregisterAppBar();
                ABSetPos(ABEdge.Left, cbMonitor.SelectedItem as string);
                stPanel.Orientation = Orientation.Vertical;
            }
            else if (Edge == "Right")
            {
                ABSetPos(ABEdge.Right, cbMonitor.SelectedItem as string);
                stPanel.Orientation = Orientation.Vertical;
            }
            if (webWindow != null)
            {
                DockToAppBar(webWindow);
                // webWindow.Close();
               // webButton.IsChecked = false;
            }
        }
        private void edgeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           Edge = (edgeMonitor.SelectedItem as string);
            relocateWindowLocation();
            Debug.WriteLine("Edge Selection Changed********** "+ Edge);
           
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
            
           // var windowBounds = webW.Bounds;
           var taskbarRect = this.Bounds;
            //var screenRect = Windows.UI.Core.CoreWindow.GetForCurrentThread().Bounds;
           // int screenWidth = (int)(GetSystemMetrics(SM_CXSCREEN));
           // int screenHeight = (int)(GetSystemMetrics(SM_CYSCREEN));
           var workarea = MonitorHelper.GetWorkArea();

            //double appBarWidth = taskbarRect.Width;
           // double appBarHeight = taskbarRect.Height;
            int newWindowWidth = 0;// = screenWidth;
            int newWindowHeight =0;// = screenHeight - 100;
            int newWindowX=0;//= (int)(taskbarRect.X);
            int newWindowY=0;//= 100;
            
            var wrc = getMonitorRect(cbMonitor.SelectedItem as string);

            if (Edge == "Top")
            {
                newWindowWidth = workarea.right;
                newWindowHeight = workarea.bottom;
                newWindowX = workarea.left;
                //newWindowY = ((int)taskbarRect.Height*(int)(dpiY/96));
                newWindowY = workarea.top ;

            }
            else if (Edge == "Bottom")
            {
                newWindowWidth = workarea.right;
                newWindowHeight = workarea.bottom;
                newWindowX = workarea.left;
                newWindowY = wrc.top;
            }
            else if (Edge == "Left")
            {
                newWindowWidth = (wrc.right - wrc.left );
                newWindowHeight = wrc.bottom;
                newWindowX = wrc.left;
                newWindowY = wrc.top;

            }
            else if (Edge == "Right")
            {
                newWindowWidth = (wrc.right - wrc.left);
                newWindowHeight = wrc.bottom;
                newWindowX = wrc.left;
                newWindowY = wrc.top;
            }
            webW.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(newWindowX, newWindowY, newWindowWidth, newWindowHeight));
           
            
        }
    }
}

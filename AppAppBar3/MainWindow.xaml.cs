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
using WinUIEx;



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
       // int theSelectedIndex = 0;
        string selectedItemsText;
        //uint dpiX, dpiY;
       // double scale;
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

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);
        public const int GWL_STYLE = -16;
        public const int WS_CAPTION = 0x00C00000;
        public const int WS_THICKFRAME = 0x00040000;

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

         [DllImport("User32.dll", CharSet=CharSet.Auto)]
        private static extern int RegisterWindowMessage(string msg);
        private int uCallBack;

        [DllImport("User32.dll", ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int cx, int cy, bool repaint);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

       // [DllImport("user32.dll")]
       // static extern int GetSystemMetrics(int nIndex);

       // const int SM_CXSCREEN = 0;  // Index for screen width in pixels
       // const int SM_CYSCREEN = 1;  // Index for screen height in pixels


        public struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uCallbackMessage;
            public int uEdge;
            public RECT rc;
            public IntPtr lParam;
        }
       
        //data structure for setting autohide or show on taskbar
        enum AppBarMessages : int
        {
            ABM_NEW = 0,
            ABM_REMOVE,
            ABM_QUERYPOS,
            ABM_SETPOS,
            ABM_GETSTATE,
            ABM_GETTASKBARPOS ,
            ABM_ACTIVATE,
            ABM_GETAUTOHIDEBAR,
            ABM_SETAUTOHIDEBAR ,
            ABM_WINDOWPOSCHANGED ,
            ABM_SETSTATE
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

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, ref int pvAttribute, int cbAttribute);

        [Flags]
        public enum DwmWindowAttribute : uint
        {
            DWMWA_NCRENDERING_ENABLED = 1,
            DWMWA_NCRENDERING_POLICY,
            DWMWA_TRANSITIONS_FORCEDISABLED,
            DWMWA_ALLOW_NCPAINT,
            DWMWA_CAPTION_BUTTON_BOUNDS,
            DWMWA_NONCLIENT_RTL_LAYOUT,
            DWMWA_FORCE_ICONIC_REPRESENTATION,
            DWMWA_FLIP3D_POLICY,
            DWMWA_EXTENDED_FRAME_BOUNDS,
            DWMWA_HAS_ICONIC_BITMAP,
            DWMWA_DISALLOW_PEEK,
            DWMWA_EXCLUDED_FROM_PEEK,
            DWMWA_CLOAK,
            DWMWA_CLOAKED,
            DWMWA_FREEZE_REPRESENTATION,
            DWMWA_LAST
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


       
        private void OnActivated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
        { 
            cbMonitor.DataContext = this;
            edgeMonitor.DataContext = this;
            selectedItemsText = @"\\.\DISPLAY1";
         
            if (appWindow == null)
            {
                
                IntPtr hWnd = WindowNative.GetWindowHandle(this);
                WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
                appWindow = AppWindow.GetFromWindowId(windowId);
                //Hide window so it is not visible on startup.  Shown at window move
                appWindow.Hide();

                //remove from aero peek
                    int value = 0x01;
                    int hr = DwmSetWindowAttribute(hWnd, DwmWindowAttribute.DWMWA_EXCLUDED_FROM_PEEK, ref value, Marshal.SizeOf(typeof(int)));
                

                // Eventhough we removed the title bar with win32 api, we still need to set the title bar properties to make the content
                // move into window title area
                //// appWindow.TitleBar.ExtendsContentIntoTitleBar = true;

                // Ensure we only register the app bar once
                if (args.WindowActivationState != WindowActivationState.Deactivated)
                {
                    RegisterBar(ABEdge.Top,cbMonitor.SelectedItem as string);
                    edgeMonitor.SelectionChanged -= edgeComboBox_SelectionChanged;
                    Edge = "Top";
                    
                    // Optionally, unsubscribe from Activated event after first activation
                    this.Activated -= OnActivated;
                }
                monitors = MonitorHelper.GetMonitors();
             
                foreach (var monitor in monitors)
                {
                    Debug.WriteLine(monitor);
                }
                
                MonitorList = new ObservableCollection<string>(monitors);
               
                cbMonitor.SelectedIndex = 0;

            }
           

        }


       

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
                fBarRegistered = false;
            }
        }
        private const int ABS_AUTOHIDE = 0x00000001;
        private const int ABS_ALWAYSONTOP = 0x00000002;
      //  private const int HWND_TOPMOST = -1;
      //  private const int HWND_NOTOPMOST = -2;
      //  private const int SWP_NOMOVE = 0x0002;
      //  private const int SWP_NOSIZE = 0x0001;
        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint WS_EX_TOOLWINDOW = 0x00000080;
        const uint WS_VISIBLE = 0x10000000;

        public const int SWP_ASYNCWINDOWPOS = 0x4000;
        private void ABSetPos(ABEdge edge, string selectedMonitor)
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
            abd.hWnd = hWnd;
            abd.uEdge = (int)edge;
            
            var wrc = MonitorHelper.getMonitorRECT(selectedMonitor);
            
            abd.rc.top = wrc.top;
              abd.rc.bottom = wrc.bottom;
              abd.rc.left = wrc.left;
              abd.rc.right = wrc.right;

            // Query the system for an approved size and position. 

            SHAppBarMessage((int)AppBarMessages.ABM_QUERYPOS, ref abd);
           

            // Adjust the rectangle, depending on the edge to which the 
            // appbar is anchored. 
           
             switch (abd.uEdge)
             {
                 case (int)ABEdge.Left:
                     abd.rc.right = abd.rc.left +100;
                     break;
                 case (int)ABEdge.Right:
                    abd.rc.left = abd.rc.right - 100;
                    Debug.WriteLine("the left side " + abd.rc.left +" the right side "+abd.rc.right);
                     break;
                 case (int)ABEdge.Top:
                     abd.rc.bottom = abd.rc.top + 100;
                     break;
                 case (int)ABEdge.Bottom:
                    abd.rc.top = abd.rc.bottom - 100;
                     break;
             }
            
              //remove corner radius by removing border and caption
              IntPtr style = GetWindowLong(hWnd, GWL_STYLE);
            style = (IntPtr)(style.ToInt64() & ~(WS_CAPTION | WS_THICKFRAME | SWP_NOZORDER | SWP_NOACTIVATE));

            SetWindowLong(hWnd, GWL_STYLE, style);

            // Pass the final bounding rectangle to the system. 
            SHAppBarMessage((int)AppBarMessages.ABM_SETPOS, ref abd);
            Debug.WriteLine("abd right "+abd.rc.right);
            Debug.WriteLine("abd Left " + abd.rc.left);
            Debug.WriteLine("abd top " + abd.rc.top);
            Debug.WriteLine("abd bottom " + abd.rc.bottom);
            
            Debug.WriteLine("Window width " + (abd.rc.right - abd.rc.left));

            // Move and size the appbar so that it conforms to the 
            // bounding rectangle passed to the system. 
            
            MoveWindow(hWnd, abd.rc.left, abd.rc.top, (abd.rc.right - abd.rc.left), (abd.rc.bottom - abd.rc.top), true);
            appWindow.Show();
            // SetWindowPos(hWnd, (IntPtr)HWND_NOTOPMOST, 0, 0, 100, abd.rc.bottom - abd.rc.top, SWP_ASYNCWINDOWPOS);
            // abd.lParam = new IntPtr(ABS_AUTOHIDE | ABS_ALWAYSONTOP);
            //IntPtr state = SHAppBarMessage((int)AppBarMessages.ABM_SETSTATE, ref abd); // Set to autohide
            //Debug.WriteLine("Appbar state " + state);

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

      
       



        private void OnClosed(object sender, WindowEventArgs args)
        {
        }

        private void UnregisterAppBar()
        {
            RegisterBar(ABEdge.Top, cbMonitor.SelectedItem as string);
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

        private void relocateWindowLocation()
        {
            Debug.WriteLine("This is the edge var "+Edge);
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
           //var workarea = MonitorHelper.GetWorkArea();

            int newWindowWidth = 0;// = screenWidth;
            int newWindowHeight =0;// = screenHeight - 100;
            int newWindowX=0;//= (int)(taskbarRect.X);
            int newWindowY=0;//= 100;
            
            var workarea = getMonitorWorkRect(cbMonitor.SelectedItem as string);

            if (Edge == "Top")
            {
                newWindowWidth = workarea.right;
                newWindowHeight = workarea.bottom;
                newWindowX = workarea.left;
                newWindowY = workarea.top ;

            }
            else if (Edge == "Bottom")
            {
                newWindowWidth = workarea.right;
                newWindowHeight = workarea.bottom;
                newWindowX = workarea.left;
                newWindowY = workarea.top;
            }
            else if (Edge == "Left")
            {
                newWindowWidth = (workarea.right - workarea.left );
                newWindowHeight = workarea.bottom;
                newWindowX = workarea.left;
                newWindowY = workarea.top;

            }
            else if (Edge == "Right")
            {
                newWindowWidth = (workarea.right - workarea.left);
                newWindowHeight = workarea.bottom;
                newWindowX = workarea.left;
                newWindowY = workarea.top;
            }
            webW.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(newWindowX, newWindowY, newWindowWidth, newWindowHeight));
           
            
        }
    }
}

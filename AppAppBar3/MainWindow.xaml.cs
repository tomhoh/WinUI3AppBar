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


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AppAppBar3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
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
        }

        private void OnActivated(object sender, WindowActivatedEventArgs args)
        {
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
                    RegisterAppBar();
                    // Optionally, unsubscribe from Activated event after first activation
                    this.Activated -= OnActivated;
                }

            }
        }
        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        const int SM_CXSCREEN = 0;  // Index for screen width in pixels
        const int SM_CYSCREEN = 1;  // Index for screen height in pixels

        private void RegisterAppBar()
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            
            int screenWidth = (int)(GetSystemMetrics(SM_CXSCREEN) );
            int screenHeight = (int)(GetSystemMetrics(SM_CYSCREEN) );

            var abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = hWnd,
                uCallbackMessage = 0, // Use a WM_USER range message for handling AppBar messages
                uEdge = (uint)ABEdge.Top, // Can be Left, Top, Right, Bottom
            };

            RECT rc;
            rc.left = 0;
            rc.top = 0;
            rc.right = screenWidth; // Width of the AppBar
            rc.bottom = 60; // Height of the AppBar
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
            SetWindowPos(hWnd, IntPtr.Zero, abd.rc.left, abd.rc.top, abd.rc.right - abd.rc.left, abd.rc.bottom - abd.rc.top, SWP_NOZORDER | SWP_NOACTIVATE | WS_EX_TOOLWINDOW);
        }
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint WS_EX_TOOLWINDOW = 0x00000080;
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
    }
}

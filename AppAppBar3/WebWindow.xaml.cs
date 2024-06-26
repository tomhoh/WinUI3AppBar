using Microsoft.UI;
using Microsoft.UI.Windowing;
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
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AppAppBar3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WebWindow : Window
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private AppWindow appWindow;
        public const int GWL_STYLE = -16;
        public const int WS_CAPTION = 0x00C00000;
        public const int WS_THICKFRAME = 0x00040000;
        public WebWindow()
        {
            this.InitializeComponent();
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            appWindow = AppWindow.GetFromWindowId(windowId);
            // Eventhough we removed the title bar with win32 api, we still need to set the title bar properties to make the content
            // move into window title area
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            //remove corner radius by removing border and caption
            IntPtr style = GetWindowLong(hwnd, GWL_STYLE);
            style = (IntPtr)(style.ToInt64() & ~(WS_CAPTION | WS_THICKFRAME));
            SetWindowLong(hwnd, GWL_STYLE, style);
            uint dpiX, dpiY;
            dpiX = dpiY = MonitorHelper.GetDpiForWindow(hwnd);
        }
        private async void InitializeWebView()
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.Navigate("https://www.microsoft.com");

           
        }
    }
}

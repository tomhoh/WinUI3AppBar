using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;


namespace AppAppBar3
{
    public sealed partial class WebWindow : WinUIEx.WindowEx
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
          
            IntPtr style = GetWindowLong(hwnd, GWL_STYLE);
            style = (IntPtr)(style.ToInt64() & ~(WS_CAPTION | WS_THICKFRAME));
            SetWindowLong(hwnd, GWL_STYLE, style);
           
            
        }
        private async void InitializeWebView()
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.Navigate("https://www.microsoft.com");

           
        }

        private void webWindow_Closed(object sender, WindowEventArgs args)
        {
           
        }
    }
}

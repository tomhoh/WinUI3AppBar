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

        public WebWindow()
        {
            
            this.InitializeComponent();
            InitializeWebView();
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            NativeMethods.removeWindowDecoration(hwnd);
            
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

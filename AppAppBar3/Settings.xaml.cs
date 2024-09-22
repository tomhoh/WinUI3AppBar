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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace AppAppBar3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : WinUIEx.WindowEx
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private AppWindow appWindow;
        public const int GWL_STYLE = -16;
        public const int WS_CAPTION = 0x00C00000;
        public const int WS_THICKFRAME = 0x00040000;

        public ObservableCollection<string> mList;
        public Settings(ObservableCollection<string> MonList)
        {
            mList = MonList;


            this.InitializeComponent();
            this.Activated += OnActivated;
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            appWindow = AppWindow.GetFromWindowId(windowId);
            // Eventhough we removed the title bar with win32 api, we still need to set the title bar properties to make the content
            // move into window title area
           // appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            //remove corner radius by removing border and caption
            IntPtr style = GetWindowLong(hwnd, GWL_STYLE);
            style = (IntPtr)(style.ToInt64() & ~(WS_CAPTION | WS_THICKFRAME));
            SetWindowLong(hwnd, GWL_STYLE, style);
        }
        private void OnActivated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
        {
            cbMonitorSettings.ItemsSource = mList;
        }
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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
using static AppAppBar3.MonitorHelper;
using WinUIEx.Messaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Microsoft.UI.Xaml.Media.Imaging;
using WinUIEx;



namespace AppAppBar3
{
    public sealed partial class MainWindow : WinUIEx.WindowEx, INotifyPropertyChanged
    {
        private String[] _MonItems;// = new String[10];
        private ObservableCollection<string> _MonitorList; 
        string selectedItemsText;
        WindowMessageMonitor monitor; 


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
                loadShortCuts();
                

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
                //remove corner radius by removing border and caption, remove title bar, remove from zorder, do not activate
                IntPtr style = GetWindowLong(hWnd, GWL_STYLE);
                style = (IntPtr)(style.ToInt64() & ~(WS_CAPTION | WS_THICKFRAME | SWP_NOZORDER | SWP_NOACTIVATE));

                SetWindowLong(hWnd, GWL_STYLE, style);
                SHAppBarMessage((int)AppBarMessages.ABM_ACTIVATE, ref abd);
                ABSetPos(edge,selectedMonitor);
                
            }
            else
            {
                
                SHAppBarMessage((int)AppBarMessages.ABM_REMOVE, ref abd);
                fBarRegistered = false;
            }
        }
        private const int ABS_AUTOHIDE = 0x1;
        private const int ABS_ALWAYSONTOP = 0x2;
        private const int HWND_TOPMOST = -1;
        private const int HWND_NOTOPMOST = -2;
      //  private const int SWP_NOMOVE = 0x0002;
      //  private const int SWP_NOSIZE = 0x0001;
        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_NOACTIVATE = 0x0010;
        //const uint WS_EX_TOOLWINDOW = 0x00000080;
       // const uint WS_VISIBLE = 0x10000000;

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

            // Pass the final bounding rectangle to the system. 
            /***********************Autohide not working******************************/
          //  abd.lParam = ABS_ALWAYSONTOP;
           // abd.lParam = (IntPtr)ABS_AUTOHIDE;
           // IntPtr state = SHAppBarMessage((int)AppBarMessages.ABM_SETSTATE, ref abd); // Set to autohide
            
           // Debug.WriteLine("Appbar state " + state);
            SHAppBarMessage((int)AppBarMessages.ABM_SETPOS, ref abd);
            Debug.WriteLine("abd right "+abd.rc.right);
            Debug.WriteLine("abd Left " + abd.rc.left);
            Debug.WriteLine("abd top " + abd.rc.top);
            Debug.WriteLine("abd bottom " + abd.rc.bottom);
            
            Debug.WriteLine("Window width " + (abd.rc.right - abd.rc.left));

            // Move and size the appbar so that it conforms to the bounding rectangle passed to the system. 
            MoveWindow(hWnd, abd.rc.left, abd.rc.top, (abd.rc.right - abd.rc.left), (abd.rc.bottom - abd.rc.top), true);
             //SetWindowPos(hWnd, (IntPtr)HWND_TOPMOST, abd.rc.left, abd.rc.top, (abd.rc.right - abd.rc.left), (abd.rc.bottom - abd.rc.top), SWP_ASYNCWINDOWPOS);
            appWindow.Show();
            
            

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

                        break;
                }
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
           // stPanel.Background = new SolidColorBrush(Colors.DarkGray);
           
        }

        private void DragLeave(object sender, DragEventArgs e)
        {
            
           // stPanel.Background = null;
        }
        private async void loadShortCuts()
        {
            try
            {
                Debug.WriteLine("load short cuts");
                var userDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                //var userDataLocal = @"C:\Users\tomho";

                
                using (StreamReader sr = new StreamReader(userDataLocal + @"\shortcuts.txt"))
                    while (!sr.EndOfStream)
                    {
                        var exePath = sr.ReadLine();
                        StorageFile file = await StorageFile.GetFileFromPathAsync(exePath);
                        var path = exePath;
                        Debug.WriteLine("path of shortcut readline " + exePath + " " + file.FileType);

                            var iconThumbnail = await file.GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem, 32);
                            var bi = new BitmapImage();
                            bi.DecodePixelHeight = 32;
                            bi.DecodePixelWidth = 32;
                            bi.SetSource(iconThumbnail);

                            Image ButtonImageEL = new Image();
                            ButtonImageEL.Source = bi;
                            ButtonImageEL.Height = 32;
                            ButtonImageEL.Width = 32;

                            Button testIButton = new Button();
                            testIButton.Background = new SolidColorBrush(Colors.Transparent);
                            testIButton.BorderBrush = new SolidColorBrush(Colors.Transparent);
                            testIButton.Content = ButtonImageEL;
                            testIButton.Click += Button_Click;
                            testIButton.Tag = path;

                            MenuFlyout menuFlyout = new MenuFlyout();
                            MenuFlyoutItem menuFlyoutItem = new MenuFlyoutItem();
                            menuFlyoutItem.Text = "Delete";
                            menuFlyoutItem.Tag = testIButton.Tag;
                            menuFlyoutItem.Click += MenuFlyoutItem_Click;
                            menuFlyout.Items.Add(menuFlyoutItem);
                            // FontIcon ItemIcon = new FontIcon();
                            // ItemIcon.Glyph = "&#xE72D;";
                            menuFlyoutItem.Icon = new SymbolIcon(Symbol.Delete);
                            testIButton.ContextFlyout = menuFlyout;
                            stPanel.Children.Add(testIButton);

                            Debug.WriteLine("File info " + path);
                        
                    }
            }
            catch (Exception error)
            {
                Debug.WriteLine(error.Message);
            }
        }
        private async void Grid_Drop(object sender, DragEventArgs e)
        {
           // stPanel.Background = null;
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
                    IWshRuntimeLibrary.IWshShell wsh = new IWshRuntimeLibrary.WshShellClass();
                    IWshRuntimeLibrary.IWshShortcut sc = (IWshRuntimeLibrary.IWshShortcut)wsh.CreateShortcut(path);
                    path = sc.TargetPath;
                }
                var iconThumbnail = await file.GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem, 32);
                var bi = new BitmapImage();
                bi.DecodePixelHeight = 32;
                bi.DecodePixelWidth = 32;
                bi.SetSource(iconThumbnail);
               
                Image ButtonImageEL = new Image();
                ButtonImageEL.Source = bi;
                ButtonImageEL.Height = 32;
                ButtonImageEL.Width = 32;

                Button testIButton = new Button();
                testIButton.Background = new SolidColorBrush(Colors.Transparent);
                testIButton.BorderBrush = new SolidColorBrush(Colors.Transparent);
                testIButton.Content = ButtonImageEL;
                testIButton.Click += Button_Click;
                testIButton.Tag = path;
                
                MenuFlyout menuFlyout = new MenuFlyout();
                MenuFlyoutItem menuFlyoutItem = new MenuFlyoutItem();
                menuFlyoutItem.Text = "Delete";
                menuFlyoutItem.Tag = testIButton.Tag;
                menuFlyoutItem.Click += MenuFlyoutItem_Click;
                menuFlyout.Items.Add(menuFlyoutItem);

                // FontIcon ItemIcon = new FontIcon();
                // ItemIcon.Glyph = "&#xE72D;";
                menuFlyoutItem.Icon = new SymbolIcon(Symbol.Delete);
                testIButton.ContextFlyout = menuFlyout;
                stPanel.Children.Add(testIButton);

                Debug.WriteLine("File info " + name + " " + path);

                try
                {
                    // Create a file that the application will store user specific shortcut data in.
                     var userDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    using (StreamWriter sw = System.IO.File.AppendText(userDataLocal + @"\shortcuts.txt"))
                        sw.WriteLine(path);
                    Debug.WriteLine(userDataLocal.ToString());
                }
                catch (IOException error)
                {
                    // Inform the user that an error occurred.
                    Debug.WriteLine("An error occurred while attempting to show the application." +
                                    "The error is:" + error.ToString());

                }
               
            }
        }

        private void UnregisterAppBar()
        {
            RegisterBar(ABEdge.Top, cbMonitor.SelectedItem as string);
        }

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
            Debug.WriteLine("Button Delete Clicked");
            var userDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            System.IO.File.Delete(userDataLocal + @"\shortcuts.txt");
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
                        using (StreamWriter sa = System.IO.File.AppendText(userDataLocal + @"\shortcuts.txt"))
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
        Settings settingsWindow;
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            if (settingsWindow == null)
            {
                settingsWindow = new Settings(MonitorList);
                settingsWindow.Activate();
                DockToAppBar(settingsWindow);
            }
            else
            {
                settingsWindow.Close();
                settingsWindow = null;
            }
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

        void DockToAppBar(Window webW)
        {
            //IntPtr whWnd = WindowNative.GetWindowHandle(webW);
            // WindowId windowId = Win32Interop.GetWindowIdFromWindow(whWnd);
            // var wappWindow = AppWindow.GetFromWindowId(windowId);
            var wappWindow = webW.GetAppWindow();
            
            int newWindowWidth = 0;// = screenWidth;
            int newWindowHeight =0;// = screenHeight - 100;
            int newWindowX=0;//= (int)(taskbarRect.X);
            int newWindowY=0;//= 100;
            
            var workarea = getMonitorWorkRect(cbMonitor.SelectedItem as string);

            if (Edge == "Top")
            {
                newWindowWidth = workarea.right - workarea.left;
                newWindowHeight = workarea.bottom;
                newWindowX = workarea.left;
                newWindowY = workarea.top ;
                if (wappWindow.Title == "Settings")
                {
                    newWindowWidth = wappWindow.Size.Width;
                    newWindowHeight = wappWindow.Size.Height;
                    newWindowX = (int)((appWindow.Size.Width / 2) - (wappWindow.Size.Width / 2));
                }

            }
            else if (Edge == "Bottom")
            {
                newWindowWidth = workarea.right - workarea.left;
                newWindowHeight = workarea.bottom;
                newWindowX = workarea.left;
                newWindowY = workarea.top;
                if (wappWindow.Title == "Settings")
                {
                    newWindowWidth = wappWindow.Size.Width;
                    newWindowHeight = wappWindow.Size.Height;
                    newWindowX = (int)((appWindow.Size.Width / 2) - (wappWindow.Size.Width / 2));
                }
            }
            else if (Edge == "Left")
            {
                newWindowWidth = workarea.right - workarea.left ;
                newWindowHeight = workarea.bottom;
                newWindowX = workarea.left;
                newWindowY = workarea.top;
                if (wappWindow.Title == "Settings")
                {
                    newWindowWidth = wappWindow.Size.Width;
                    newWindowHeight = wappWindow.Size.Height;
                    newWindowY = (int)((appWindow.Size.Height / 2) - (wappWindow.Size.Height / 2));
                }

            }
            else if (Edge == "Right")
            {
                newWindowWidth = workarea.right - workarea.left;
                newWindowHeight = workarea.bottom;
                newWindowX = workarea.left;
                newWindowY = workarea.top;
                if (wappWindow.Title == "Settings")
                {
                    newWindowWidth = wappWindow.Size.Width;
                    newWindowHeight = wappWindow.Size.Height;
                    newWindowY = (int)((appWindow.Size.Height / 2) - (wappWindow.Size.Height / 2));
                }
            }
         


            webW.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(newWindowX, newWindowY, newWindowWidth, newWindowHeight));
            
        }

       
    }
}

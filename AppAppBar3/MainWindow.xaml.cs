using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
using WinUIEx.Messaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Microsoft.UI.Xaml.Media.Imaging;
using WinUIEx;
using System.Text.RegularExpressions;
using System.Threading.Tasks;



namespace AppAppBar3
{
    using static NativeMethods;
    using static MonitorHelper;
    public sealed partial class MainWindow : WinUIEx.WindowEx, INotifyPropertyChanged
    {

        private String[] _MonItems;
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

        private List<Window> _OpenWindows = new List<Window>();
        public List<Window> OpenWindows
        {
            get => _OpenWindows;
            set
            {
                _OpenWindows = value;
               // OnPropertyChanged();
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

        private int uCallBack;
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
               // Debug.WriteLine(monitor);
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
            Debug.WriteLine("wrc right " + wrc.right);
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
            //appWindow.MoveAndResize(new Windows.Graphics.RectInt32(abd.rc.left, abd.rc.top, (abd.rc.right - abd.rc.left), (abd.rc.bottom - abd.rc.top)));
            // Move and size the appbar so that it conforms to the bounding rectangle passed to the system. 
            MoveWindow(hWnd, abd.rc.left, abd.rc.top, (abd.rc.right - abd.rc.left), (abd.rc.bottom - abd.rc.top), true);
           // MoveWindow(hWnd, abd.rc.left, abd.rc.top, (abd.rc.right - abd.rc.left), (abd.rc.bottom - abd.rc.top), true);

            //SetWindowPos(hWnd, (IntPtr)HWND_TOPMOST, abd.rc.left, abd.rc.top, (abd.rc.right - abd.rc.left), (abd.rc.bottom - abd.rc.top), SWP_ASYNCWINDOWPOS);
            appWindow.Show();

            SHAppBarMessage((int)AppBarMessages.ABM_WINDOWPOSCHANGED, ref abd);
        }

        /******************* OnWindowMessageReceived is WndProc****************/
        private void OnWindowMessageReceived(object sender, WindowMessageEventArgs e)
        {
            Debug.WriteLine("*************Message receieved********** " + e.Message.ToString());
            const int WM_DISPLAYCHANGE = 7;

            if (e.Message.MessageId == uCallBack)
            {
                Debug.WriteLine("**!!*****Message Main Window receieved in callback**!!**** " + e.Message.ToString() +" "+e.Message.MessageId.ToString());
                switch (e.Message.WParam)
                {
                     
                    case (int)ABNotify.ABN_POSCHANGED: //arries when bar changes to different monitor
                        Debug.WriteLine("*************Message receieved in callback********** " + e.Message.ToString());
                      //  monitor.WindowMessageReceived -= OnWindowMessageReceived;
                      relocateWindowLocation();
                      //  monitor.WindowMessageReceived += OnWindowMessageReceived;

                        break;

                }
            }
            switch (e.Message.MessageId)
            {
                case (int)AppBarMessages.ABM_WINDOWPOSCHANGED:
                    Debug.WriteLine("window changed position changed notification " + e.Message.ToString());
                    SHAppBarMessage((int)AppBarMessages.ABM_WINDOWPOSCHANGED, ref abd);
                    break;
            }

            switch (e.Message.WParam)
            {
                
                case WM_DISPLAYCHANGE:
                    monitor.WindowMessageReceived -= OnWindowMessageReceived;
                    var seletedMon = (cbMonitor.SelectedItem as String);

                    Debug.WriteLine("Monitor attached ");
                    var list1 = MonitorHelper.GetMonitors();
                    cbMonitor.SelectionChanged -= DisplayComboBox_SelectionChanged;
                    list1.Sort();
                    MonitorList = null;
                    MonitorList = new ObservableCollection<string>(list1);

                    cbMonitor.SelectionChanged += DisplayComboBox_SelectionChanged;

                    cbMonitor.SelectedItem = seletedMon;

                    monitor.WindowMessageReceived += OnWindowMessageReceived;

                    break;
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
           
        }

        private void DragLeave(object sender, DragEventArgs e)
        {
        }

#region shortcuts
        private async void loadShortCuts()
        {
            try
            {
                using (StreamReader sr = new StreamReader(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\shortcuts.txt"))
                    while (!sr.EndOfStream)
                    {
                        var exePath = sr.ReadLine();
                        StorageFile file = await StorageFile.GetFileFromPathAsync(exePath);
                        Debug.WriteLine("path of shortcut readline " + exePath + " " + file.FileType);
                        await createShortCut(file, exePath);
                    }
            }
            catch (Exception error)
            {
                Debug.WriteLine(error.Message);
            }
        }

        
        private async void Grid_Drop(object sender, DragEventArgs e)
        {
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
                await createShortCut(file, path);
                try
                {
                    // Create a file that the application will store user specific shortcut data in.
                    using (StreamWriter sw = System.IO.File.AppendText(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\shortcuts.txt"))
                        sw.WriteLine(path);
                }
                catch (IOException error)
                {
                    // Inform the user that an error occurred.
                    Debug.WriteLine("An error occurred while attempting to show the application." +
                                    "The error is:" + error.ToString());

                }
               
            }
        }

        private async Task createShortCut(StorageFile aFile, String aPath)
        {
            try
            {
                var iconThumbnail = await aFile.GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem, 32);
                var bi = new BitmapImage() 
                {
                    DecodePixelHeight = 32,
                    DecodePixelWidth = 32,
                };
                bi.SetSource(iconThumbnail);

                Image ButtonImageEL = new Image()
                {
                    Source = bi,
                    Height = 32,
                    Width = 32,
                 };

                Button testIButton = new Button()
                {
                    Background = new SolidColorBrush(Colors.Transparent),
                    BorderBrush = new SolidColorBrush(Colors.Transparent),
                    Content = ButtonImageEL,
                    Tag = aPath,
                 };
                testIButton.Click += Button_Click;


                MenuFlyout menuFlyout = new MenuFlyout();
                MenuFlyoutItem menuFlyoutItem = new MenuFlyoutItem() 
                {
                    Text = "Delete",
                    Tag = testIButton.Tag,
                    Icon = new SymbolIcon(Symbol.Delete),
                 };
               
                menuFlyoutItem.Click += MenuFlyoutItem_Click;
                menuFlyout.Items.Add(menuFlyoutItem);
                testIButton.ContextFlyout = menuFlyout;
                stPanel.Children.Add(testIButton);

                Debug.WriteLine("File info " + aPath);

            }
            catch (Exception error)
            {
                Debug.WriteLine(error.Message);
            }
        }
        #endregion
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

            System.IO.File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\shortcuts.txt");
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
                        using (StreamWriter sa = System.IO.File.AppendText(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\shortcuts.txt"))
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
                settingsWindow = new Settings(MonitorList,uCallBack);
                settingsWindow.ExtendsContentIntoTitleBar = true;
                settingsWindow.Activate();
                
                DockToAppBar(settingsWindow);
                OpenWindows.Add(settingsWindow);
            }
            else
            {
                settingsWindow.Close();
                OpenWindows.Remove(settingsWindow);
                settingsWindow = null;
            }
        }
        WindowDetect wappWindow;
        private void DetectWindow_click(object sender, RoutedEventArgs e)
        {
            foreach (var mon in _MonitorList)
            {
                var displayNumString = Regex.Match(mon, @"\d+").Value;
                var workarea = getMonitorWorkRect(mon);

                wappWindow = new WindowDetect(displayNumString);
                wappWindow.ExtendsContentIntoTitleBar = true;
                var dwindow = wappWindow.GetAppWindow();
                wappWindow.Show();
                //windowDetect.Show

                dwindow.MoveAndResize(new Windows.Graphics.RectInt32(workarea.right - dwindow.Size.Width - 50, workarea.bottom - (dwindow.Size.Height + 50), dwindow.Size.Width, dwindow.Size.Height));
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
                OpenWindows.Add(webWindow);
                DockToAppBar(webWindow);
            }
            else
            {
                webWindow.Close();
                OpenWindows.Remove(webWindow);
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

        private void appbarWindow_Closed(object sender, WindowEventArgs args)
        {
            foreach(var window in OpenWindows)
            {
                if(window != null)
                {
                    window.Close();
                }
                
            }

        }
    }
}

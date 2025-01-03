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
using Windows.Data.Xml.Dom;
using Microsoft.Win32;



namespace AppAppBar3
{
    using static NativeMethods;
    using static MonitorHelper;
    using static SettingMethods;
    
    public sealed partial class MainWindow : WinUIEx.WindowEx, INotifyPropertyChanged
    {

       // private ObservableCollection<string> _MonitorList;
        private ObservableCollection<Monitor> _MonitorList;

        string selectedItemsText;
        WindowMessageMonitor monitor;

        //public ObservableCollection<string> MonitorList
        public ObservableCollection<Monitor> MonitorList

        {
            get => _MonitorList;
            set
            {
                _MonitorList = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> ListOfMonitors


        {
            
            get
            {
                List<string> mList = new List<string>();
                foreach (var mon in _MonitorList)
                {
                    mList.Add(mon.MonitorName);
                    
                }
                return new ObservableCollection<string>(mList);
            }
           // get => (Monitor)_MonitorList.MonitorName;
            //set
           // {
               // _MonitorList = value;
               // OnPropertyChanged();
            //}
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
      

        private ABEdge _Edge;

        public ABEdge Edge
        {
            get => _Edge;
            set
            {
                _Edge = value;
                OnPropertyChanged();
            }
        }
        private bool fBarRegistered = false;
        public List<string> monitors;
        public List<Monitor> monitorInfo;

        private int uCallBack;
        private AppWindow appWindow;
        public MainWindow()
        {
         
            this.InitializeComponent();
            this.Activated += OnActivated;
            this.AppWindow.IsShownInSwitchers = false;
            monitorInfo = GetMonitorsInfo();
            MonitorList = new ObservableCollection<Monitor>(GetMonitorsInfo());
            cbMonitor.DataContext = this;
            edgeMonitor.DataContext = this;
            monitor = new WindowMessageMonitor(this);
            monitor.WindowMessageReceived += OnWindowMessageReceived;
            edgeMonitor.ItemsSource = Enum.GetValues(typeof(ABEdge));
        }
       

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Debug.WriteLine("MonitorList changed*****" + propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

       
        private void OnActivated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
        {

            cbMonitor.SelectionChanged -= DisplayComboBox_SelectionChanged;
            edgeMonitor.SelectionChanged -= edgeComboBox_SelectionChanged;
            // selectedItemsText = @"\\.\DISPLAY1";

            if (appWindow == null)
            {
               

                //check if settings file exists

                if (!Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey("edge"))
                {
                    SettingMethods.setDefaultValues();
                    edgeMonitor.SelectedItem = (ABEdge)loadSettings("edge");
                    cbMonitor.SelectedItem = (string)loadSettings("monitor");
                }
               
                else
                {
                    edgeMonitor.SelectedItem = (ABEdge)loadSettings("edge");
                    cbMonitor.SelectedItem = (string)loadSettings("monitor");
                }
               
                
                Debug.WriteLine("Window activated edge from settings " + (ABEdge)loadSettings("edge"));
                IntPtr hWnd = WindowNative.GetWindowHandle(this);
                WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
                appWindow = AppWindow.GetFromWindowId(windowId);

                //remove from aero peek
                    int value = 0x01;
                    int hr = DwmSetWindowAttribute(hWnd, DwmWindowAttribute.DWMWA_EXCLUDED_FROM_PEEK, ref value, Marshal.SizeOf(typeof(int)));

                // Ensure we only register the app bar once
                if (args.WindowActivationState != WindowActivationState.Deactivated)
                {
                    RegisterBar((ABEdge)loadSettings("edge"), (string)loadSettings("monitor"));

                    // Optionally, unsubscribe from Activated event after first activation
                    this.Activated -= OnActivated;
                }
                //monitors = MonitorHelper.GetMonitors();
               
        
                if (monitorInfo != null)
                {
                    monitors = new List<string>();
                    foreach (Monitor monitor in monitorInfo)
                    {
                        monitors.Add(monitor.MonitorName);
                    }
                   // MonitorList = new ObservableCollection<string>(monitors);

                }
                // Debug.WriteLine(monitor);
                // foreach (var monitor in monitors)
                // {
                //Debug.WriteLine(monitor);
                // }

                // MonitorList = new ObservableCollection<string>(monitors);
                edgeMonitor.SelectionChanged += edgeComboBox_SelectionChanged;
                cbMonitor.SelectionChanged += DisplayComboBox_SelectionChanged;
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
       /* private const int ABS_AUTOHIDE = 0x1;
        private const int ABS_ALWAYSONTOP = 0x2;
        private const int HWND_TOPMOST = -1;
        private const int HWND_NOTOPMOST = -2;*/
      //  private const int SWP_NOMOVE = 0x0002;
      //  private const int SWP_NOSIZE = 0x0001;
        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_NOACTIVATE = 0x0010;
        //const uint WS_EX_TOOLWINDOW = 0x00000080;
       // const uint WS_VISIBLE = 0x10000000;

       // public const int SWP_ASYNCWINDOWPOS = 0x4000;
        private void ABSetPos(ABEdge edge, string selectedMonitor)
        {
            

            Debug.WriteLine("the selected monitor in ABSETPOS " + selectedMonitor);
            var hWnd = WindowNative.GetWindowHandle(this);
            abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
            abd.hWnd = hWnd;
            abd.uEdge = (int)edge;
            //monitorInfo = GetMonitorsInfo();
            double scaleFactor =1.5;
           // MonitorList = null;
           // MonitorList = new ObservableCollection<Monitor>(GetMonitorsInfo());
            foreach (var monitor in MonitorList)
            {
                
                if (monitor.MonitorName == selectedMonitor)
                {
                    Debug.WriteLine("wrc right " + monitor.WorkRect.right);
                    abd.rc.top = monitor.WorkRect.top;
                    abd.rc.bottom = monitor.WorkRect.bottom;
                    abd.rc.left = monitor.WorkRect.left;
                    abd.rc.right = monitor.WorkRect.right;
                    //scaleFactor = monitor.scale;
                    scaleFactor = GetScale(monitor.MonitorName);

           // var wrc = MonitorHelper.getMonitorRECT(selectedMonitor);


                    // Query the system for an approved size and position. 

            SHAppBarMessage((int)AppBarMessages.ABM_QUERYPOS, ref abd);

            Debug.WriteLine("********Scale Factor**************** " + GetScale(monitor.MonitorName));
            // Adjust the rectangle, depending on the edge to which the 
            // appbar is anchored. 
            // Eventhough Winui 3 is set to auto scale the Win32 Appbar does not.  we use GetScale(monitor)
            // to get this done.
            //var theBarSize = Convert.ToInt32(loadSettings("bar_size"));
            int theBarSize;
            if (SettingMethods.loadSettings("bar_size") != null)
            {
                theBarSize = (int)SettingMethods.loadSettings("bar_size");
            }
            else
            {
                theBarSize = 50;
            }
                

            switch (abd.uEdge) 
             {
                 case (int)ABEdge.Left:
                     abd.rc.right = (int)(abd.rc.left + (theBarSize * scaleFactor));
                    break;
                 case (int)ABEdge.Right:
                    abd.rc.left = (int)(abd.rc.right - (theBarSize * scaleFactor));
                    Debug.WriteLine("the left side " + abd.rc.left +" the right side "+abd.rc.right);
                     break;
                 case (int)ABEdge.Top:
                     abd.rc.bottom = (int)(abd.rc.top + (theBarSize * scaleFactor));
                    break;
                 case (int)ABEdge.Bottom:
                    abd.rc.top = (int)(abd.rc.bottom - (theBarSize * scaleFactor));
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
            HwndExtensions.SetWindowSize(hWnd, (abd.rc.right - abd.rc.left), (abd.rc.bottom - abd.rc.top));
            bool success = MoveWindow(hWnd, abd.rc.left, abd.rc.top, (abd.rc.right - abd.rc.left), (abd.rc.bottom - abd.rc.top), true);
            Debug.WriteLine("Did we sucessed with resize and move *1* ? " + success);
           // bool success2 = MoveWindow(hWnd, abd.rc.left, abd.rc.top, (abd.rc.right - abd.rc.left), (abd.rc.bottom - abd.rc.top), true);
            //Debug.WriteLine("Did we sucessed with resize and move *2* ? " + success2);
           // HwndExtensions.SetWindowPositionAndSize(hWnd, abd.rc.left, abd.rc.top, (abd.rc.right - abd.rc.left), (abd.rc.bottom - abd.rc.top));
            //SetWindowPos(hWnd, (IntPtr)HWND_TOPMOST, abd.rc.left, abd.rc.top, (abd.rc.right - abd.rc.left), (abd.rc.bottom - abd.rc.top), SWP_ASYNCWINDOWPOS);
            //appWindow.Show();

            SHAppBarMessage((int)AppBarMessages.ABM_WINDOWPOSCHANGED, ref abd);
                    break;
                }

            }
        }

        /******************* OnWindowMessageReceived is WndProc****************/
        private void OnWindowMessageReceived(object sender, WindowMessageEventArgs e)
        {
           // Debug.WriteLine("*************Message receieved********** " + e.Message.ToString());
            const int WM_DISPLAYCHANGE = 7;

            if (e.Message.MessageId == uCallBack)
            {
                Debug.WriteLine("**!!*****Message Main Window receieved in callback**!!**** " + e.Message.ToString() +" "+e.Message.MessageId.ToString());
                switch (e.Message.WParam)
                {
                     
                    case (int)ABNotify.ABN_POSCHANGED: //arries when bar changes to different monitor
                        Debug.WriteLine("*************Message receieved in callback********** " + e.Message.ToString());
                      //  monitor.WindowMessageReceived -= OnWindowMessageReceived;
                      relocateWindowLocation((ABEdge)edgeMonitor.SelectedItem);
                      //  monitor.WindowMessageReceived += OnWindowMessageReceived;

                        break;

                }
            }
            switch (e.Message.MessageId)
            {
                case (int)AppBarMessages.ABM_WINDOWPOSCHANGED:
                    Debug.WriteLine("window changed position changed notification " + e.Message.ToString());
                    //relocateWindowLocation();
                    SHAppBarMessage((int)AppBarMessages.ABM_WINDOWPOSCHANGED, ref abd);
                    break;
            }

            switch (e.Message.WParam)
            {
                
                case WM_DISPLAYCHANGE:
                    monitor.WindowMessageReceived -= OnWindowMessageReceived;
                    var seletedMon = (cbMonitor.SelectedItem as String);

                    Debug.WriteLine("Monitor attached ");
                   // var list1 = MonitorHelper.GetMonitors();
                    cbMonitor.SelectionChanged -= DisplayComboBox_SelectionChanged;
                   // list1.Sort();
                    MonitorList = null;
                    ///////////////// MonitorList = new ObservableCollection<string>(list1);
                    MonitorList = new ObservableCollection<Monitor>(GetMonitorsInfo());
                    cbMonitor.SelectionChanged += DisplayComboBox_SelectionChanged;

                    cbMonitor.SelectedItem = seletedMon;
                   // relocateWindowLocation();
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

        public void restartAppBar()
        {
            ABSetPos((ABEdge)SettingMethods.loadSettings("edge"), (string)SettingMethods.loadSettings("monitor"));
            //ABSetPos(theSelectedEdge, cbMonitor.SelectedItem as string);

        }
        Settings settingsWindow;

        public void closeSettingsWindow()
        {
            if (settingsWindow != null)
            {
                settingsWindow.Close();
                OpenWindows.Remove(settingsWindow);
                settingsWindow = null;
            }
        }
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            if (settingsWindow == null)
            {
                settingsWindow = new Settings(monitors,uCallBack,this);
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
            foreach (var mon in MonitorList)
            {
                var displayNumString = Regex.Match(mon.MonitorName, @"\d+").Value;
               // foreach (var monitor in monitorInfo)
               // {
//if (mon.MonitorName == mon)
                   // {
                        RECT workarea = mon.WorkRect;
                        wappWindow = new WindowDetect(displayNumString);
                        wappWindow.ExtendsContentIntoTitleBar = true;
                        var dwindow = wappWindow.GetAppWindow();
                        wappWindow.Show();
                        //windowDetect.Show

                        dwindow.MoveAndResize(new Windows.Graphics.RectInt32(workarea.right - dwindow.Size.Width - 50, workarea.bottom - (dwindow.Size.Height + 50), dwindow.Size.Width, dwindow.Size.Height));

                   // }

                //}
                //var workarea = getMonitorWorkRect(mon);

                          }
        }

        private void DisplayComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            edgeMonitor.SelectionChanged -= edgeComboBox_SelectionChanged;
            Debug.WriteLine("Monitor selection changed");
            relocateWindowLocation((ABEdge)edgeMonitor.SelectedItem);
            edgeMonitor.SelectionChanged += edgeComboBox_SelectionChanged;
            
            selectedItemsText = (cbMonitor.SelectedItem as String);
               
                Debug.WriteLine("Selected Monitor Text**********" + (cbMonitor.SelectedItem as string));
        }


        private void relocateWindowLocation(ABEdge theSelectedEdge)
        {
            Debug.WriteLine("This is the edge var "+Edge);
            
           
              ABSetPos(theSelectedEdge, cbMonitor.SelectedItem as string);
            if (Edge == ABEdge.Top || Edge == ABEdge.Bottom)
            {
                Debug.WriteLine("Edge Selection " + Edge);

                stPanel.Orientation = Orientation.Horizontal;
            }
            else
            {
                stPanel.Orientation = Orientation.Vertical;
            }

           
            if (webWindow != null)
            {
                DockToAppBar(webWindow);
            }
        }
        private void edgeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           Edge = ((ABEdge)edgeMonitor.SelectedItem);
            Debug.WriteLine("This is the selecteditem edge "+Edge);
            relocateWindowLocation((ABEdge)edgeMonitor.SelectedItem);
            Debug.WriteLine("Edge Selection Changed********** "+ Edge);
           
        }
        WebWindow webWindow;
        private void openWebWindow(object sender, RoutedEventArgs e)
        {
            if (webWindow == null)
            {
                webWindow = new WebWindow();
                DockToAppBar(webWindow);
                webWindow.Activate();
                OpenWindows.Add(webWindow);
                
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
            int newWindowHeight = 0;// = screenHeight - 100;
            int newWindowX = 0;//= (int)(taskbarRect.X);
            int newWindowY = 0;//= 100;
            foreach (var monitor in monitorInfo)
                if (monitor.MonitorName == cbMonitor.SelectedItem as string)
                {

                var workarea = monitor.WorkRect;
            //var workarea = getMonitorWorkRect(cbMonitor.SelectedItem as string);

            if (Edge == ABEdge.Top)
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
            else if (Edge == ABEdge.Bottom)
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
            else if (Edge == ABEdge.Left)
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
            else if (Edge == ABEdge.Right)
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

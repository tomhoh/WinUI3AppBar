using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Windows.Storage;
using WinUIEx.Messaging;



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

       

        private AppWindow settingWindow;
        public const int GWL_STYLE = -16;
        public const int WS_CAPTION = 0x00C00000;
        public const int WS_THICKFRAME = 0x00040000;
        public const int ABN_POSCHANGED = 1;
        public int appBarCallBack;
        WindowMessageMonitor monitor;
        MainWindow parentWindow;

        public ObservableCollection<string> mList;
        public Settings(ObservableCollection<string> MonList, int appBarCall,MainWindow Parent)
        {
            mList = MonList;
            appBarCallBack = appBarCall;
            parentWindow = Parent;
            this.InitializeComponent();
            this.Activated += OnActivated;
            monitor = new WindowMessageMonitor(this);
            monitor.WindowMessageReceived += OnWindowMessageReceived;

            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            settingWindow = AppWindow.GetFromWindowId(windowId);
            // Eventhough we removed the title bar with win32 api, we still need to set the title bar properties to make the content
            // move into window title area
            
           // settingsWindow.ExtendsContentIntoTitleBar = true;
            //remove corner radius by removing border and caption
            IntPtr style = GetWindowLong(hwnd, GWL_STYLE);
            style = (IntPtr)(style.ToInt64() & ~(WS_CAPTION | WS_THICKFRAME));
            SetWindowLong(hwnd, GWL_STYLE, style);
            cbMonitorSettings.SelectedItem = (string)SettingMethods.loadSettings("monitor");
           // bsize.Value = Convert.ToDouble(loadSettings("bar_size"));
            bsize.Value = Convert.ToDouble((int)SettingMethods.loadSettings("bar_size"));

            cbEdgeSettings.ItemsSource = Enum.GetValues(typeof(NativeMethods.ABEdge));
            cbEdgeSettings.SelectedItem = (NativeMethods.ABEdge)SettingMethods.loadSettings("edge");
            
            loadOnStartupCheckBox.IsChecked = loadOnStartup("LoadOnStartup");
        }
        private void OnActivated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
        {
            cbMonitorSettings.ItemsSource = mList;

        }
       


        private void OnWindowMessageReceived(object sender, WindowMessageEventArgs e)
        {
            const int WM_DISPLAYCHANGE = 7;
            Debug.WriteLine("AppBar call back " +appBarCallBack.ToString());
            Debug.WriteLine("*************Settings Window Message receieved********** " + e.Message.MessageId.ToString());

            if (e.Message.MessageId == appBarCallBack)
            {
                // Debug.WriteLine("*************Message receieved in callback********** " + e.Message.ToString());
                switch (e.Message.WParam)
                {

                    case (int)ABN_POSCHANGED:
                         Debug.WriteLine("*************Message callback recieved in Settings Window********** " + e.Message.ToString());
                        //  monitor.WindowMessageReceived -= OnWindowMessageReceived;
                        // relocateWindowLocation();
                        //  monitor.WindowMessageReceived += OnWindowMessageReceived;

                        break;

                }
            }
            switch (e.Message.MessageId)
            {

                case WM_DISPLAYCHANGE:
                    monitor.WindowMessageReceived -= OnWindowMessageReceived;
                   
                    Debug.WriteLine("Monitor attached ");
                   


                    monitor.WindowMessageReceived += OnWindowMessageReceived;

                    break;
               // case (int)AppBarMessages.ABM_WINDOWPOSCHANGED:
                    //Debug.WriteLine("window changed position changed notification " + e.Message.ToString());
                   // SHAppBarMessage((int)AppBarMessages.ABM_WINDOWPOSCHANGED, ref abd);
                   // break;
            }
    



        }
        /* private void saveEdgeSetting(string setting, int value)
         {
             ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

             // Save a setting locally on the device
             localSettings.Values[setting] = value;
         }
         private void saveSetting(string setting,  string value)
         {
             ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

             // Save a setting locally on the device
             localSettings.Values[setting] = value;
         }

         private void saveBoolSetting(string setting, bool value)
         {
             ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

             // Save a setting locally on the device
             localSettings.Values[setting] = value;
         }

         private string loadSettings(string setting)
         {
             ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

             // load a setting that is local to the device
             if (localSettings.Values[setting] != null)
             {
                 return localSettings.Values[setting] as string;
             }
             else
             {
                 return "0";
             }
         }*/
        private bool loadOnStartup(string setting)
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            // load a setting that is local to the device
            if (localSettings.Values[setting] != null)
            {
                return (bool)(localSettings.Values[setting]);
            }
            else
            {
                return false;
            }
        }


        private void cbMonitorSettings_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            SettingMethods.saveSetting("monitor",cbMonitorSettings.SelectedItem as string);
        }

        private void bsize_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
        {
            if(bsize.Value == (int)SettingMethods.loadSettings("bar_size"))
            {
                restartAppBarButton.Visibility = Visibility.Collapsed;

            }
            else
            {
                SettingMethods.saveSetting("bar_size", Convert.ToInt32(bsize.Value));

                restartAppBarButton.Visibility = Visibility.Visible;
                restartAppBarButton.Focus(FocusState.Keyboard);

            }

        }

        private void cbEdgeSettings_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            SettingMethods.saveSetting("edge", (int)cbEdgeSettings.SelectedItem);
        }

        private void closeSettingsButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            parentWindow.closeSettingsWindow();
            //this.Close();
        }

        private void loadOnStartupCheckBox_Checked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
           
            
        }

        private async void loadOnStartupCheckBox_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            Windows.ApplicationModel.StartupTask startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("AppAppBar3Id");
        if((sender as CheckBox).IsChecked == true)
            {
                switch (startupTask.State)
                {
                    case Windows.ApplicationModel.StartupTaskState.Disabled:
                        Windows.ApplicationModel.StartupTaskState state = await startupTask.RequestEnableAsync();
                        SettingMethods.saveSetting("LoadOnStartup", true);
                        break;
                    case Windows.ApplicationModel.StartupTaskState.DisabledByUser:
                        Debug.WriteLine("Run at startup Startup disabled by user");
                        break;
                    case Windows.ApplicationModel.StartupTaskState.DisabledByPolicy:
                        Debug.WriteLine("Run at startup Startup disabled by Policy");
                        break;
                    case Windows.ApplicationModel.StartupTaskState.EnabledByPolicy:
                        Debug.WriteLine("Run at startup Startup Enabled by Policy");
                        break;
                } 
             }else
            {
                startupTask.Disable();
                SettingMethods.saveSetting("LoadOnStartup", false);
            }
        }

        

        private void restartAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            parentWindow.restartAppBar();
            restartAppBarButton.Visibility = Visibility.Collapsed;
            
        }
    }
}

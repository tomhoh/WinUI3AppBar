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
    public sealed partial class Settings : WinUIEx.WindowEx
    {
       
        public const int ABN_POSCHANGED = 1;
        public int appBarCallBack;
        WindowMessageMonitor monitor;
        MainWindow parentWindow;

        public Settings(ObservableCollection<string> MonList, int appBarCall,MainWindow Parent)
        {
            appBarCallBack = appBarCall;
            parentWindow = Parent;
            this.InitializeComponent();
            this.Activated += OnActivated;
            monitor = new WindowMessageMonitor(this);
            monitor.WindowMessageReceived += OnWindowMessageReceived;

            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            // Eventhough we removed the title bar with win32 api, we still need to set the title bar properties to make the content
            // move into window title area

            // settingsWindow.ExtendsContentIntoTitleBar = true;
            //remove corner radius by removing border and caption
            NativeMethods.removeWindowDecoration(hwnd);

            cbMonitorSettings.ItemsSource = MonList;
            cbMonitorSettings.SelectedItem = (string)SettingMethods.loadSettings("monitor");
            bsize.Value = (int)SettingMethods.loadSettings("bar_size");

            cbEdgeSettings.ItemsSource = Enum.GetValues(typeof(NativeMethods.ABEdge));
            cbEdgeSettings.SelectedItem = (NativeMethods.ABEdge)SettingMethods.loadSettings("edge");

            loadOnStartupCheckBox.IsChecked = loadOnStartup("LoadOnStartup");
        }
        private void OnActivated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
        {
            

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

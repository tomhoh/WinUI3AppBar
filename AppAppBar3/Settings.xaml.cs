using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading;
using Windows.Storage;
using WinUIEx.Messaging;



namespace AppAppBar3
{
    using static SettingMethods;
    using static NativeMethods;
    public sealed partial class Settings : WinUIEx.WindowEx
    {

        public int appBarCallBack;
        WindowMessageMonitor monitor;
        MainWindow parentWindow;

       // public Settings(ObservableCollection<string> MonList, int appBarCall,MainWindow Parent)
        public Settings(List<string> MonList, int appBarCall, MainWindow Parent)

        {
            appBarCallBack = appBarCall;
            parentWindow = Parent;
            this.InitializeComponent();
            ThemeHelper.Register(this);
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
            cbMonitorSettings.SelectedItem = (string)loadSettings("monitor");
            bsize.Value = (int)loadSettings("bar_size");

            cbEdgeSettings.ItemsSource = Enum.GetValues(typeof(ABEdge));
            cbEdgeSettings.SelectedItem = (ABEdge)loadSettings("edge");

            loadOnStartupCheckBox.IsChecked = loadOnStartup("LoadOnStartup");

            var autohideSetting = loadSettings("autohide");
            autohideCheckBox.IsChecked = autohideSetting is bool b && b;

            // Populate theme picker after the constructor sets the saved selection,
            // so cbThemeSettings_SelectionChanged doesn't fire during initial load.
            // Use strings rather than ElementTheme values: the WinRT projection wraps
            // boxed enum values in IReference<T>, and ComboBox falls back to the
            // wrapper's ToString() which shows "Windows.Foundation.IReference`1<...>".
            cbThemeSettings.SelectionChanged -= cbThemeSettings_SelectionChanged;
            cbThemeSettings.ItemsSource = Enum.GetNames(typeof(ElementTheme));
            cbThemeSettings.SelectedItem = ThemeHelper.LoadSavedTheme().ToString();
            cbThemeSettings.SelectionChanged += cbThemeSettings_SelectionChanged;
        }

        private void cbThemeSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbThemeSettings.SelectedItem is string s
                && Enum.TryParse<ElementTheme>(s, out var t))
            {
                ThemeHelper.SaveAndApply(t);
            }
        }

        private void autohideCheckBox_Click(object sender, RoutedEventArgs e)
        {
            saveSetting("autohide", autohideCheckBox.IsChecked == true);
            parentWindow.restartAppBar();
        }
        private void OnActivated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
        {
            

        }
       

        private void OnWindowMessageReceived(object sender, WindowMessageEventArgs e)
        {
            Debug.WriteLine("AppBar call back " +appBarCallBack.ToString());
            Debug.WriteLine("*************Settings Window Message receieved********** " + e.Message.MessageId.ToString());

            if (e.Message.MessageId == appBarCallBack)
            {
                switch (e.Message.WParam)
                {

                    case (int)ABNotify.ABN_POSCHANGED:
                         Debug.WriteLine("*************Message callback recieved in Settings Window********** " + e.Message.ToString());
                        break;

                }
            }
            switch (e.Message.MessageId)
            {

                case WM_DISPLAYCHANGE:
                    Debug.WriteLine("Monitor attached ");
                    break;
               // case (int)AppBarMessages.ABM_WINDOWPOSCHANGED:
                    //Debug.WriteLine("window changed position changed notification " + e.Message.ToString());
                   // SHAppBarMessage((int)AppBarMessages.ABM_WINDOWPOSCHANGED, ref abd);
                   // break;
            }
    


        }

        private const string RunKeyPath   = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "AppAppBar3";
        private const string StartupTaskId = "AppAppBar3Id";

        private bool loadOnStartup(string setting)
        {
            // Packaged build: reflect the actual StartupTask state (respects user's Startup Apps page).
            // Unpackaged build: reflect presence of the HKCU Run value.
            if (SettingMethods.IsPackaged())
            {
                try
                {
                    var task = Windows.ApplicationModel.StartupTask.GetAsync(StartupTaskId).GetAwaiter().GetResult();
                    return task.State == Windows.ApplicationModel.StartupTaskState.Enabled
                        || task.State == Windows.ApplicationModel.StartupTaskState.EnabledByPolicy;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StartupTask query failed: " + ex.Message);
                }
            }
            else
            {
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
                    return key?.GetValue(RunValueName) != null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("HKCU Run read failed: " + ex.Message);
                }
            }
            return loadSettings(setting) as bool? ?? false;
        }


        private void cbMonitorSettings_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            saveSetting("monitor",cbMonitorSettings.SelectedItem as string);
        }

        private void bsize_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
        {
            if(bsize.Value == (int)loadSettings("bar_size"))
            {
                restartAppBarButton.Visibility = Visibility.Collapsed;

            }
            else
            {
                saveSetting("bar_size", Convert.ToInt32(bsize.Value));

                restartAppBarButton.Visibility = Visibility.Visible;
                restartAppBarButton.Focus(FocusState.Keyboard);

            }

        }

        private void cbEdgeSettings_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            saveSetting("edge", (int)cbEdgeSettings.SelectedItem);
        }

        private void closeSettingsButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            parentWindow.closeSettingsWindow();
            //this.Close();
        }

       

        private async void loadOnStartupCheckBox_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            bool wantEnabled = (sender as CheckBox).IsChecked == true;

            if (SettingMethods.IsPackaged())
            {
                try
                {
                    var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync(StartupTaskId);
                    if (wantEnabled)
                    {
                        switch (startupTask.State)
                        {
                            case Windows.ApplicationModel.StartupTaskState.Disabled:
                                await startupTask.RequestEnableAsync();
                                break;
                            case Windows.ApplicationModel.StartupTaskState.DisabledByUser:
                                Debug.WriteLine("Startup disabled by user — enable in Settings > Apps > Startup.");
                                break;
                            case Windows.ApplicationModel.StartupTaskState.DisabledByPolicy:
                                Debug.WriteLine("Startup disabled by policy.");
                                break;
                        }
                    }
                    else
                    {
                        startupTask.Disable();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StartupTask toggle failed: " + ex.Message);
                }
            }
            else
            {
                try
                {
                    using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
                    if (wantEnabled)
                    {
                        var exe = Environment.ProcessPath;
                        if (!string.IsNullOrEmpty(exe))
                            key.SetValue(RunValueName, "\"" + exe + "\"");
                    }
                    else
                    {
                        key.DeleteValue(RunValueName, throwOnMissingValue: false);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("HKCU Run write failed: " + ex.Message);
                }
            }

            saveSetting("LoadOnStartup", wantEnabled);
        }


        private void restartAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            parentWindow.restartAppBar();
            restartAppBarButton.Visibility = Visibility.Collapsed;
            
        }
    }
}

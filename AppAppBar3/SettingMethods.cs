using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace AppAppBar3
{
    public static class SettingMethods
    {
        static ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

       public static void setDefaultValues()
        {
            if (localSettings != null)
            {
                saveSetting("bar_size", 50);
                saveSetting("monitor", @"\\.\DISPLAY1");
                saveSetting("LoadOnStartup", true);
                saveSetting("edge", 1);
            }
        }
        
        public static void saveSetting(string setting, object value)
        {

            // Save a setting locally on the device
            localSettings.Values[setting] = value;
        }

      

        public static object loadSettings(string setting)
        {

            // load a setting that is local to the device
            if (localSettings.Values[setting] != null)
            {
                return localSettings.Values[setting];
            }
            else
            {
                return null;
            }
        }
    }
}

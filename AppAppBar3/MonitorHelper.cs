using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace AppAppBar3
{
    using static AppAppBar3.NativeMethods;
    public static class MonitorHelper
    {
        public class Monitor
        {
           // public string FriendlyMonitorName;
            //public SizeAndPosition SizeAndPosition;
            public string MonitorName;
            public double scale;
            public RECT WorkRect;
            public RECT MonitorRect;
        }

        public static List<Monitor> GetMonitorsInfo()
        {
            List<Monitor> monitors = new List<Monitor>();
            MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                double monitorScale = 1;
                uint dpiX;
                uint dpiY;
                
                MONITORINFOEX mi = new MONITORINFOEX();
                mi.cbSize = Marshal.SizeOf(mi);
                bool success = GetMonitorInfo(hMonitor, ref mi);
                if (success)
                {
                    GetDpiForMonitor(hMonitor, DpiType.Effective, out dpiX, out dpiY);

                    if (dpiX > 96)
                        monitorScale = (double)dpiX / 96d;


                    var monitor = new Monitor
                    {
                        MonitorName = mi.szDevice,
                        scale = monitorScale,
                        WorkRect = mi.rcWork,
                        MonitorRect = mi.rcMonitor,
                    };

                    monitors.Add(monitor);

                }
                else
                {
                    throw new Win32Exception();
                }

                return true; // Continue enumeration
            };

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
            // ObservableCollection<string> oList = new ObservableCollection<string>(monitorNames);

            return monitors;
        }



  

        public static double GetScale(string monitor)
        {
            double scale = 1;
            uint dpiX;
            uint dpiY;
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                Debug.WriteLine("monitor name in GetScale******** " + monitor);
                MONITORINFOEX mi = new MONITORINFOEX();
                mi.cbSize = Marshal.SizeOf(mi);
                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    Debug.WriteLine("monitor name " + mi.szDevice + " msent " + monitor + " " + mi.dwFlags.ToString());
                    if (mi.szDevice == monitor)
                    {
                        GetDpiForMonitor(hMonitor, DpiType.Effective, out dpiX, out dpiY);
                        
                         if (dpiX > 96)
                            scale = (double)dpiX / 96d;
                        Debug.WriteLine("!!!!!!!!!!!!!!!scale in GetScale ******** " +dpiX+" " +scale);

                        return false;
                    }
                }
                // Stop enumeration

                return true;
            }, IntPtr.Zero);
            return scale;
           
        }

      
    }
}





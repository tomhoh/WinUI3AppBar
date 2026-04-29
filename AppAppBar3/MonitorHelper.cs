using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;


namespace AppAppBar3
{
    using static AppAppBar3.NativeMethods;
    public static class MonitorHelper
    {
        public class Monitor
        {
            // Friendly label shown in UI (e.g. "Display 1"). Also used as the stable
            // identifier in settings.json — internal code matches on this string,
            // so MonitorName must round-trip through SettingMethods unchanged.
            public string MonitorName;
            public double scale;
            public RECT WorkRect;
            public RECT MonitorRect;
        }

        // Win32 returns the device path (e.g. "\\.\DISPLAY1"). Format it as
        // "Display N" for the UI; the digit suffix is stable across boots.
        public static string FormatDisplayName(string szDevice)
        {
            if (string.IsNullOrEmpty(szDevice)) return szDevice;
            var m = Regex.Match(szDevice, @"DISPLAY(\d+)", RegexOptions.IgnoreCase);
            return m.Success ? "Display " + m.Groups[1].Value : szDevice;
        }

        public static List<Monitor> GetMonitorsInfo()
        {
            List<Monitor> monitors = new List<Monitor>();
            MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                // Throwing inside a native callback is undefined behavior — log and continue.
                try
                {
                    MONITORINFOEX mi = new MONITORINFOEX();
                    mi.cbSize = Marshal.SizeOf(mi);
                    if (!GetMonitorInfo(hMonitor, ref mi))
                    {
                        Debug.WriteLine("GetMonitorInfo failed: err=" + Marshal.GetLastWin32Error());
                        return true;
                    }

                    double monitorScale = 1;
                    if (GetDpiForMonitor(hMonitor, DpiType.Effective, out uint dpiX, out _) == IntPtr.Zero && dpiX > 96)
                        monitorScale = (double)dpiX / 96d;

                    monitors.Add(new Monitor
                    {
                        MonitorName = FormatDisplayName(mi.szDevice),
                        scale = monitorScale,
                        WorkRect = mi.rcWork,
                        MonitorRect = mi.rcMonitor,
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Monitor enumeration entry failed: " + ex.Message);
                }

                return true; // Continue enumeration
            };

            if (!EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero))
                Debug.WriteLine("EnumDisplayMonitors failed: err=" + Marshal.GetLastWin32Error());

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





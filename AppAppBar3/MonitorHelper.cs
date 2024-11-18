using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Threading;
using Windows.UI.Core.AnimationMetrics;


namespace AppAppBar3
{
    using static NativeMethods;
    internal class MonitorHelper
    {
      
        public static List<string> GetMonitors()
        {
            List<string> monitorNames = new List<string>();
            MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                MONITORINFOEX mi = new MONITORINFOEX();
                mi.cbSize = Marshal.SizeOf(mi);
                bool success = GetMonitorInfo(hMonitor, ref mi);
                if (success)
                {
                    monitorNames.Add(mi.szDevice);

                }
                else
                {
                    throw new Win32Exception();
                }

                monitorNames.Sort();
                return true; // Continue enumeration
            };

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
           // ObservableCollection<string> oList = new ObservableCollection<string>(monitorNames);
            
            return monitorNames;
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
                        NativeMethods.GetDpiForMonitor(hMonitor, DpiType.Effective, out dpiX, out dpiY);
                        
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

        public static RECT getMonitorWorkRect(string monitor)
        {
            RECT windowRect = new RECT();
            
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                Debug.WriteLine("monitor name ******** " + monitor);
                MONITORINFOEX mi = new MONITORINFOEX();
                mi.cbSize = Marshal.SizeOf(mi);
                    if (GetMonitorInfo(hMonitor, ref mi))
                    {
                    Debug.WriteLine("monitor name " + mi.szDevice + " msent " + monitor +" "+mi.dwFlags.ToString());
                    if (mi.szDevice == monitor)
                    {
                       // var monitorRect = mi.rcMonitor;
                       var monitorRect = mi.rcWork;

                        windowRect.left = monitorRect.left;
                        windowRect.top = monitorRect.top;
                        windowRect.right = monitorRect.right;
                        windowRect.bottom = monitorRect.bottom;
                        Debug.WriteLine("get Monitor Width*****" + (windowRect.right-100));
                        Debug.WriteLine("get Monitor Left*****" + windowRect.left);
                        Debug.WriteLine("get Monitor Top*****" + windowRect.top);
                        Debug.WriteLine("get Monitor Bottom*****" + windowRect.bottom);
                        return false;
                    }
                    }
                    // Stop enumeration
                
                return true;
            }, IntPtr.Zero);

            return windowRect;
        }
        
        public static RECT getMonitorRECT(string monitor)
        {
            RECT windowRect = new RECT();

            // int foundMonitors = -1;

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                Debug.WriteLine("monitor name ******** " + monitor);
                MONITORINFOEX mi = new MONITORINFOEX();
                mi.cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    Debug.WriteLine("monitor name " + mi.szDevice + " msent " + monitor + " " + mi.dwFlags.ToString());
                    if (mi.szDevice == monitor)
                    {


                        RECT monitorRect = mi.rcMonitor;
                       // windowRect = mi.rcWork;
                        windowRect = monitorRect;
                        

                        Debug.WriteLine("!!!!!!!!!Selected Monitor Rect*****" + monitorRect.ToString());
                        //windowRect.left = (int)(monitorRect.left *dpiX/96.0f);
                      /*  windowRect.left = monitorRect.left;
                        windowRect.top = monitorRect.top;
                    
                        windowRect.right = monitorRect.right;
                        windowRect.bottom = monitorRect.bottom;*/
                        Debug.WriteLine("get Monitor right*****" + (windowRect.right));
                        Debug.WriteLine("get Monitor Left*****" + windowRect.left);
                        Debug.WriteLine("get Monitor Top*****" + windowRect.top);
                        Debug.WriteLine("get Monitor Bottom*****" + windowRect.bottom);
                        return false;
                    }
                }
                // Stop enumeration

                return true;
            }, IntPtr.Zero);

            return windowRect;
            
        }
    }
}





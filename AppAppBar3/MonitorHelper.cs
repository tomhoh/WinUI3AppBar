using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;


namespace AppAppBar3
{
    internal class MonitorHelper
    {
        [DllImport("user32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        public const int MONITOR_DEFAULTTOPRIMARY = 1;

        // Define the MONITORINFOEX structure
         [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
          private struct MONITORINFOEX
          {
              public int cbSize;
              public RECT rcMonitor;
              public RECT rcWork;
              public int dwFlags;
              [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
              public string szDevice;
          }

        // Define the RECT structure
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        const uint SPI_GETWORKAREA = 0x0030;
        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);
        public static ObservableCollection<string> GetMonitors()
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
                return true; // Continue enumeration
            };

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
            ObservableCollection<string> oList = new ObservableCollection<string>(monitorNames);
            
            return oList;
        }
       /* const uint SWP_NOZORDER = 0x0004;
        const uint SWP_NOACTIVATE = 0x0010;
        public static void SetWindowPositionOnMonitor(IntPtr hWnd, int monitorIndex, int x, int y, int width, int height)
        {
            int foundMonitors = -1;
            POINT pt = new POINT { x = 0, y = 0 };

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                foundMonitors++;
                if (foundMonitors == monitorIndex)
                {

                    // MONITORINFOEX mi = new MONITORINFOEX();
                    MONITORINFOEX mi = new MONITORINFOEX();

                    Debug.WriteLine("dwflag**** " + mi.dwFlags +" monitor index "  +monitorIndex + " found monitorindex " +foundMonitors + " y location "+ mi.rcWork.top);
                    mi.cbSize = Marshal.SizeOf(mi);
                   
                    if (GetMonitorInfo(hMonitor, ref mi))
                    {
                      //  if ((mi.dwFlags & 1) == 0) // 1 is MONITORINFOF_PRIMARY
                       // {
                            pt.x =  mi.rcWork.left;
                            pt.y = mi.rcWork.top;
                        // width = mi.rcWork.right;
                        height = 60;
                        SetWindowPos(hWnd, IntPtr.Zero, pt.x, pt.y, width, height, SWP_NOZORDER | SWP_NOACTIVATE);
                       
                         return false;
                        // }
                    }
                    return false; // Stop enumeration
                }
                return true;
            }, IntPtr.Zero);
        }
       
       */
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
                        Debug.WriteLine("get Monitor right*****" + (windowRect.right-100));
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





using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace AppAppBar3
{
    internal class MonitorHelper
    {
        [DllImport("user32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);
         
        [DllImport("user32.dll")]
         static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

        [DllImport("SHCore.dll", SetLastError = true)]
        public static extern int GetDpiForMonitor(IntPtr hmonitor, MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY);


        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(POINT pt, int dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern uint GetDpiForWindow(IntPtr hWnd);

        [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        public enum MONITOR_DPI_TYPE
        {
            MDT_EFFECTIVE_DPI = 0,
            MDT_ANGULAR_DPI = 1,
            MDT_RAW_DPI = 2,
            MDT_DEFAULT = MDT_EFFECTIVE_DPI
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        public const uint SWP_SHOWWINDOW = 0x0040;

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
        public static RECT GetWorkArea()
        {
            RECT rect = new RECT();
            SystemParametersInfo(SPI_GETWORKAREA, 0, ref rect, 0);
            
            return rect;
        }
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
                return true; // Continue enumeration
            };

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
            return monitorNames;
        }
        const uint SWP_NOZORDER = 0x0004;
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
                   // hMonitor = MonitorFromPoint(pt, mi.dwFlags);
                   
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
          /*  IntPtr hMonitor = MonitorFromPoint(pt, monitorIndex);
            if (hMonitor != IntPtr.Zero)
            {
                foundMonitors++;
                if (foundMonitors == monitorIndex)
                {
                    MONITORINFOEX mi = new MONITORINFOEX();
                    mi.cbSize = Marshal.SizeOf(mi);
                    if (GetMonitorInfo(hMonitor, ref mi))
                    {
                        pt.x = mi.rcWork.left + x;
                        pt.y = mi.rcWork.top + y;
                        width = mi.rcWork.right;
                        SetWindowPos(hWnd, IntPtr.Zero, pt.x, pt.y, width, height, SWP_NOZORDER);
                    }
                }
            }*/
        }

       
        public static RECT getMonitorRect(string monitor)
        {
            RECT windowRect = new RECT();
            
           // int foundMonitors = -1;

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


                        // Scale coordinates and size based on DPI

                        uint dpiX = 0;
                        uint dpiY = 0;
                        //GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_RAW_DPI, out dpiX, out dpiY);
                        var monitorRect = mi.rcMonitor;
                       //windowRect.left = (int)(monitorRect.left *dpiX/96.0f);
                        windowRect.left = monitorRect.left;
                        windowRect.top = monitorRect.top;
                        // windowRect.right = (int)(monitorRect.right *dpiY/96.0f);
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





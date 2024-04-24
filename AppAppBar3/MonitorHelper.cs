﻿using System;
using System.Collections.Generic;
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
    }
}




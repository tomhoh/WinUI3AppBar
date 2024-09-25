using Microsoft.UI;
using Microsoft.UI.Windowing;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Timers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AppAppBar3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WindowDetect : WinUIEx.WindowEx
    {
        System.Timers.Timer aTimer;

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private AppWindow dWindow;
        public const int GWL_STYLE = -16;
        public const int WS_CAPTION = 0x00C00000;
        public const int WS_THICKFRAME = 0x00040000;
        public WindowDetect(string Text)
        {
            this.InitializeComponent();
            SetTimer();
            DisplayText.Text = Text;
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
           
            //remove corner radius by removing border and caption
            IntPtr style = GetWindowLong(hwnd, GWL_STYLE);
            style = (IntPtr)(style.ToInt64() & ~(WS_CAPTION | WS_THICKFRAME));
            SetWindowLong(hwnd, GWL_STYLE, style);
        }

        private void SetTimer()
        {
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(4000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = false;
            aTimer.Enabled = true;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            try
            {
                aTimer.Enabled=false;
                aTimer.Dispose();
                DispatcherQueue.TryEnqueue(() => { this.Close(); });

                
            }
            catch (Exception err)
            {
                Debug.WriteLine(err);
                throw;
            }
            
           
            
            
        }

       
    }
}

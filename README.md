## WINUI 3 Appbar
Win32 Appbar (taskbar) created from a WINUI 3 window. Similar to WPF and WinForms Appbars.
var scale = (this.Content as FrameworkElement).XamlRoot.RasterizationScale;
this.AppWindow.Resize(new((int)width*scale, (int)height*scale))
## WINUI 3 Appbar
Win32 Appbar (taskbar) created from a WINUI 3 window. Similar to WPF and WinForms Appbars.

 var scale = (this.Content as FrameworkElement).XamlRoot.RasterizationScale;
this.AppWindow.Resize(new((int)width*scale, (int)height*scale))

[Link](https://www.codeproject.com/Tips/5360135/Getting-Display-Information-in-Windows-UI-3)
````
private async Task SizeWindow( AppWindow appWindow )
{
    var displayList = await DeviceInformation.FindAllAsync
                      ( DisplayMonitor.GetDeviceSelector() );

    if( !displayList.Any() )
        return;

    var monitorInfo = await DisplayMonitor.FromInterfaceIdAsync( displayList[ 0 ].Id );

    var winSize = new SizeInt32();

    if( monitorInfo == null )
    {
        winSize.Width = 800;
        winSize.Height = 1200;
    }
    else
    {
        winSize.Height = monitorInfo.NativeResolutionInRawPixels.Height;
        winSize.Width = monitorInfo.NativeResolutionInRawPixels.Width;

        var widthInInches = Convert.ToInt32( 8 * monitorInfo.RawDpiX ); 
        var heightInInches = Convert.ToInt32( 12 * monitorInfo.RawDpiY );

        winSize.Height = winSize.Height > heightInInches? 
                         heightInInches: winSize.Height;
        winSize.Width = winSize.Width > widthInInches ? widthInInches: winSize.Width;
    }

    appWindow.Resize( winSize );
}
````

[Link](https://stackoverflow.com/questions/76631011/appwindow-moveandresize-dpi-wrong-window-size-with-multple-displays-and-differe)

````
Windows.Graphics.RectInt32 rect = new()
{
    X = (displayArea.WorkArea.Width / 2) - (_wndWidth / 2),
    Y = (displayArea.WorkArea.Height / 3) - (_wndHeight / 2),
    Height = _wndHeight,
    Width = _wndWidth
};

_appWindow.MoveAndResize(rect, displayArea);
_appWindow.Show();
_appWindow.MoveInZOrderAtTop();
````
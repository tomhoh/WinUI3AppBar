## WINUI 3 Appbar
Implementation of a Desktop AppBar (taskbar) in WINUI 3. Similar to WPF and WinForms Appbars.  
Includes an example webview (webview2) window that is docked to the appbar when open.

![AppApbar3](https://github.com/user-attachments/assets/4b9c7b84-c161-4bde-a1c3-b916f0bee4cf)

### Requirements
- Visual Studio
- Windows App SDK
- Windows App SDK (For VS 2022 C#) : https://aka.ms/windowsappsdk/stable-vsix-2022-cs or Windows App SDK (For VS 2019 C#) : https://aka.ms/windowsappsdk/stable-vsix-2019-cs
- Webview2 Evergreen Runtime (https://developer.microsoft.com/en-us/microsoft-edge/webview2/?form=MA13LH) or simply remove (comment out) the webview from code 
- WinUIex.  : https://github.com/dotMorten/WinUIEx?tab=readme-ov-file

You can find more information in here: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/set-up-your-development-environment?tabs=vs-2022

Because the included example docking window uses webview 2 you will also need to install Webview2 Evergreen Runtime (https://developer.microsoft.com/en-us/microsoft-edge/webview2/?form=MA13LH) or simply remove (comment out) the webview from code 



### Capabilities
- Allow docking to any side of the screen
- Multiple monitor support
- Handles per-monitor DPI scaling
- Does not show in switcher
- Excluded from aero peek
- Follows current Windows desktop theme
- Handles Drag and drop shortcut with deletion, autosaving and auto restore on startup
- Option to run at login (autostart).
## To do
- Build into Library
- code refractoring
- Bug fixes
- allow for custom theme (currenlty only supports current desktop theme)



### License
MIT License


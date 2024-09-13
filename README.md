## WINUI 3 Appbar
Implementation of an AppBar (taskbar) in WINUI 3. Similar to WPF and WinForms Appbars.  
Includes an example webview (webview2) window that is docked to the appbar when open.

![AppApbar3](https://private-user-images.githubusercontent.com/5827145/326842281-78c96d16-e361-43c4-a49b-0a73cacbdb71.png?jwt=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3MTQ0OTE1NTAsIm5iZiI6MTcxNDQ5MTI1MCwicGF0aCI6Ii81ODI3MTQ1LzMyNjg0MjI4MS03OGM5NmQxNi1lMzYxLTQzYzQtYTQ5Yi0wYTczY2FjYmRiNzEucG5nP1gtQW16LUFsZ29yaXRobT1BV1M0LUhNQUMtU0hBMjU2JlgtQW16LUNyZWRlbnRpYWw9QUtJQVZDT0RZTFNBNTNQUUs0WkElMkYyMDI0MDQzMCUyRnVzLWVhc3QtMSUyRnMzJTJGYXdzNF9yZXF1ZXN0JlgtQW16LURhdGU9MjAyNDA0MzBUMTUzNDEwWiZYLUFtei1FeHBpcmVzPTMwMCZYLUFtei1TaWduYXR1cmU9MmQzMjc4ZWE5ZWUzNTBhMDkzMjc2ZmNkYTg3YjAyNDVlZGYwM2RiOWI4ZGFmMDc0YzVjN2RmMzZjNzNjMTE1NCZYLUFtei1TaWduZWRIZWFkZXJzPWhvc3QmYWN0b3JfaWQ9MCZrZXlfaWQ9MCZyZXBvX2lkPTAifQ.8Iihrsx4I7LwFMK3RfoIUJxLz4zbeehf6GgrtKf4WWI)

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

## To do
- Build into Library
- Drop and drag
- Bug fixes
- watch for other appbars comming and going to automatically adjust.



### License
MIT License


# StickyMouse

StickyMouse is a C# application that constrains your mouse movement to linear axes and 3-point arcs using customizable key and mouse bindings.

## Features
- **Mouse Constraint Mode**: Hold a key (default: `Shift`) to constrain the mouse movement to up/down/left/right/diagonal axes.
- **Arc Edit Mode**: Hold a key (default: `Tab`) to enable a dynamically adjustable 3-point arc.
  - First point: Position when the key is first pressed.
  - Third point: Current mouse position.
  - Second point: Adjusted dynamically using `Arc Adjust +` and `Arc Adjust -` (default: mouse wheel up/down).
- **Settings UI**: Customize key/mouse bindings, threshold value, and threshold factor.
- **Notification Icon**: Manage settings and functionality from the system tray.

## Demos
https://github.com/user-attachments/assets/0cc389d1-df39-4d5f-b2ca-2dea091fc808
https://github.com/user-attachments/assets/325274b0-344f-49da-b661-9b961830520d
https://github.com/user-attachments/assets/715e58bf-d3c1-47a5-bdb6-8bc984faac1e
https://github.com/user-attachments/assets/c4294eee-07d2-48c8-801c-d773a253d1c5
https://github.com/user-attachments/assets/424daf24-fad7-4d06-9e7d-d94ecf851964
https://github.com/user-attachments/assets/dd9a65dd-5a03-44aa-af9d-dea484edda0b

## Installation
Download the latest release from the [Releases](https://github.com/Tophness/StickyMouse/releases) page and run `StickyMouse.exe`.

## Compilation
### Using Visual Studio
1. Open Visual Studio.
2. Create a new C# Console Application.
3. Replace `Program.cs` with the StickyMouse source code.
4. Build the project to generate `StickyMouse.exe` and `StickyMouse.dll`.

### Using .NET Core CLI
1. Install the [.NET SDK](https://dotnet.microsoft.com/download/dotnet).
2. Open a terminal and navigate to the project directory.
3. Run the following command:
   ```sh
   dotnet build --configuration Release
   ```
4. The compiled files will be in the `bin/Release/netX.0/win-x64/` folder (`X.0` depends on your .NET version).

## Usage
1. Run `StickyMouse.exe`.
2. Hold the configured constraint key (default: `Shift`) to lock mouse movement along axes.
3. Hold the Arc Edit key (default: `Tab`) to enable arc mode.
4. Adjust the arc midpoint using the configured binds (default: mouse wheel up/down).
5. Customize settings via the UI accessible from the system tray icon.

## License
[MIT License](LICENSE)

## Contributing
Pull requests and feature suggestions are welcome. Open an issue for any bugs or improvements.

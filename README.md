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

## Demo

![StickyMouse Demo](assets/stickymouse.mp4)

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
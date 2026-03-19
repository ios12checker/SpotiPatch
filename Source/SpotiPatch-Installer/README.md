# SpotiPatch Installer - C# WPF Edition

A professional, modern installer for Spicetify built with C# and WPF.

## Features

- **Modern Spotify-inspired UI** - Dark theme with rounded corners and smooth animations
- **Professional look** - Borderless window, pill-shaped buttons, card-based layout
- **Embedded scripts** - No external downloads, everything is self-contained
- **Async installation** - Non-blocking UI with real progress updates
- **Auto-patching** - Automatically handles all prompts without user intervention
- **Single-file executable** - Can be distributed as one `.exe` file
- **Proper error handling** - Try-catch with meaningful error messages

## Requirements

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10 or later

## Building

### Quick Build (Development)

```bash
cd SpotiPatch-Installer
dotnet build
```

### Build Single-File Executable (Release)

```bash
cd SpotiPatch-Installer
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

The output will be in:
```
bin\Release\net8.0-windows\win-x64\publish\SpotiPatch-Installer.exe
```

### Build with Compression (Smaller File)

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true
```

## Project Structure

```
SpotiPatch-Installer/
├── SpotiPatch-Installer.csproj    # Project file
├── App.xaml                        # App resources & styles
├── App.xaml.cs                     # App code-behind
├── MainWindow.xaml                 # Main UI (XAML)
├── MainWindow.xaml.cs              # Main logic (C#)
├── AssemblyInfo.cs                 # Assembly metadata
├── Scripts/                        # Embedded PowerShell scripts
│   ├── spicetify-cli-install.ps1
│   └── spicetify-marketplace-install.ps1
└── README.md                       # This file
```

## How It Works

1. **UI loads** - User sees a modern, Spotify-inspired interface
2. **Click Install** - Shows confirmation dialog
3. **Script patching** - Embedded PowerShell scripts are auto-patched to:
   - Replace `Read-Host` prompts with auto-`"Y"` responses
   - Replace `PromptForChoice` with auto-select first option (0)
   - Remove `FlushInputBuffer` calls that can hang GUI apps
4. **Async execution** - Scripts run in background with real-time log output
5. **Progress tracking** - Progress bar updates as each step completes
6. **Spotify detection** - Automatically finds Spotify installation
7. **Apply** - Runs `spicetify backup apply` to activate

## Customization

### Changing Colors

Edit `App.xaml` and modify the color resources:

```xml
<Color x:Key="SpotifyGreen">#1DB954</Color>
<Color x:Key="BackgroundDark">#121212</Color>
<!-- etc -->
```

### Adding a Window Icon

Add a `spotify-icon.ico` file to the project root and it will be included automatically.

## License

MIT License - Feel free to use and modify!

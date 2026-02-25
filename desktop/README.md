# Face Recognition – Desktop & Mobile App (.NET MAUI)

Cross-platform .NET MAUI 8 application that connects to the ASP.NET backend to identify faces in photos.

## Platforms

| Target framework | Platform |
|-----------------|----------|
| `net8.0-windows10.0.19041.0` | Windows 10 / 11 desktop |
| `net8.0-android` | Android 5.0+ |
| `net8.0-ios` | iOS 14.2+ |
| `net8.0-maccatalyst` | macOS 14+ |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) with **MAUI workload**

```bash
dotnet workload install maui
```

- Android: Android SDK (via Visual Studio or standalone)
- iOS / macOS: Xcode on macOS

## Build & Run

```bash
# Windows desktop
dotnet run -f net8.0-windows10.0.19041.0

# Android (emulator or device connected via ADB)
dotnet run -f net8.0-android

# iOS (requires macOS + Xcode)
dotnet run -f net8.0-ios
```

## Configuration

On first launch, open the **Settings** tab and enter the backend URL:

| Scenario | URL |
|----------|-----|
| Windows desktop (backend on same machine) | `http://localhost:5233` |
| Android emulator (backend on host machine) | `http://10.0.2.2:5233` |
| Real device on LAN | `http://192.168.1.x:5233` |

The URL is saved in device Preferences and persists across restarts.

## Features

- **Select File** – picks a JPEG/PNG/BMP from disk (desktop) or gallery (mobile)
- **Take Photo** – opens device camera (mobile only; button hidden on desktop)
- Live photo preview before recognition
- Sends the photo to `POST /api/faces/recognize` on the backend
- Displays name, confidence percentage and status message
- Settings page for configuring the backend API URL

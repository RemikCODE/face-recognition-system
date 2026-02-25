# Mobile App (Android & iOS)

The mobile application is implemented as part of the **cross-platform .NET MAUI project** located in [`../desktop/FaceRecognitionApp/`](../desktop/FaceRecognitionApp/).

MAUI targets all client platforms from a single codebase:

| Target | Platform |
|--------|----------|
| `net8.0-android` | Android phone / tablet |
| `net8.0-ios` | iPhone / iPad |
| `net8.0-windows10.0.19041.0` | Windows desktop |
| `net8.0-maccatalyst` | macOS |

## Building for Android

```bash
cd desktop/FaceRecognitionApp
dotnet build -f net8.0-android
```

## Building for iOS (requires macOS + Xcode)

```bash
dotnet build -f net8.0-ios
```

## Features (mobile-specific)

- **Take Photo** button uses `MediaPicker.CapturePhotoAsync()` to launch the device camera
- **Select File** picks from the photo gallery via `FilePicker`
- The API base URL is configured per-device in the **Settings** tab (use the host machine's LAN IP)

## Permissions required

- `android.permission.CAMERA` – for taking photos
- `android.permission.INTERNET` – for reaching the backend
- `NSCameraUsageDescription` / `NSPhotoLibraryUsageDescription` (iOS)

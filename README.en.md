# VolKey

[Русский](README.md) · **English**

<p align="center">
  <b>Control your music with gestures on the volume keys.</b><br>
  For Windows 10 and Windows 11.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/platform-Windows_10%20%7C%2011-0078D4?style=for-the-badge&logo=windows&logoColor=white" alt="Windows 10 and 11">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8">
  <img src="https://img.shields.io/github/v/release/Xyberov/VolKey?style=for-the-badge" alt="Latest release">
</p>

## What it is

**VolKey** is a small Windows app that turns short combinations of volume key presses into player commands: next track, play/pause, mute. It runs in the background and lives in the system tray.

It works with headphones, keyboards and any other device that sends the standard `Volume Up` and `Volume Down` keys to Windows.

## Download

- [**Installer**](https://github.com/Xyberov/VolKey/releases/latest/download/VolKeySetup.exe) — shortcuts, autostart, and uninstall from Windows Settings → Apps.
- [**Portable**](https://github.com/Xyberov/VolKey/releases/latest/download/VolKey-portable.exe) — a single exe that runs without installation.

No .NET installation needed. All versions are on the [Releases](../../releases) page.

## Gestures

Default combinations:

| Presses | Action |
| --- | --- |
| `Volume +` → `Volume −` → `Volume +` | Next track ⏭ |
| `Volume −` → `Volume +` → `Volume −` | Previous track ⏮ |

You can change them and add your own. Available actions:

- next track;
- previous track;
- play / pause;
- mute / unmute.

Pick a combination from the list or record it by pressing the real volume keys (the **●** button). Recording stops after a pause or on the fourth press; **Esc** cancels it.

Combination rules:

- 2 to 4 presses, using both keys — so regular volume adjustment (`+ + +`) never turns into a gesture;
- the pause between neighbouring presses must not exceed the one set in the settings;
- if one combination starts another (for example, `+ −` and `+ − +`), the short one fires only after a pause, so the long one can still be entered.

Single presses change the volume as usual, and holding a key counts as one press. By default, after a gesture VolKey restores the volume and mute state to what they were before the first press.

## Features

- Runs in the background — no need to keep the window open.
- Works with any player that understands media keys: Spotify, browsers, Yandex Music, VK Music and more.
- Custom combinations and actions, recorded straight from the volume keys.
- Tray icon: click to open the settings; the menu toggles gestures and quits. The icon turns grey when gestures are off.
- Starts with Windows quietly, without showing the window.
- Launching it again doesn't start a second copy — it opens the window of the running one.
- Russian and English interface.

## Settings

| Setting | What it does |
| --- | --- |
| **Enable gestures** | Temporarily turns gesture handling on or off. |
| **Start with Windows** | Launches VolKey when you sign in. |
| **Restore volume after a gesture** | Undoes the volume change caused by the gesture's presses. |
| **Show a notification for each action** | A short notification such as "Next track" or "Sound muted". |
| **Max pause between presses** | How long you can wait between neighbouring presses: 250–1500 ms, 550 ms by default. |
| **Language** | System default, Russian or English. Switches instantly. |
| **Gestures** | A "combination → action" table: add, record with the keys, edit, remove or restore defaults. |

Settings are stored in `%LOCALAPPDATA%\VolKey\settings.json`.

> Closing the window doesn't quit VolKey — it keeps running in the background. To quit completely, choose **Quit** in the tray icon menu or **Quit VolKey** in the window.

## Releasing

1. Bump `Version` in `VolKey.csproj` and commit.
2. Create a tag and push it to GitHub:

   ```powershell
   git tag v1.3.0
   git push origin v1.3.0
   ```

GitHub Actions runs the tests, builds the portable exe and the installer, and publishes the release. The app version comes from the tag.

## Project structure

```text
VolKey/
├── Program.cs                  # Entry point, single-instance guard
├── VolKeyApplicationContext.cs # Wires services and the window, runs gesture actions
├── MainForm.cs                 # Settings window and tray icon
├── Strings.cs                  # Russian and English UI text
├── Services/
│   ├── KeyboardHook.cs          # Global volume key hook
│   ├── Gestures.cs              # Combinations, actions and validation
│   ├── GestureMatcher.cs        # Matching presses to gestures
│   ├── GestureRecognizer.cs     # Press buffer and pause timer
│   ├── GestureRecorder.cs       # Recording a combination from the volume keys
│   ├── MediaController.cs       # Sending media keys to the player
│   ├── AudioVolumeService.cs    # Reading and restoring volume and mute
│   ├── AppSettings.cs           # Settings and their validation
│   ├── SettingsService.cs       # Reading and saving settings.json
│   ├── AutoStartService.cs      # Windows autostart
│   ├── ShowWindowSignal.cs      # Opening the running copy's window on relaunch
│   └── AppIcons.cs              # Window and tray icons
├── tests/VolKey.Tests/          # Tests (xUnit)
├── Assets/VolKey.ico            # Icon
├── tools/make-icon.ps1          # Icon generator
├── Properties/PublishProfiles/  # Single-exe publish profile
├── VolKeySetup.iss              # Inno Setup installer script
└── .github/workflows/           # Build, tests and releases on GitHub Actions
```

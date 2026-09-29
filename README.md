# CATIA GPU Tuner

Small Windows utility that automatically checks an NVIDIA CAD workstation and, when ready, loads one verified CATIA performance profile. It asks the user to select nothing.

## What happens when it opens

1. Confirms NVIDIA Control Panel is installed.
2. Detects the installed NVIDIA GPU and driver through NVIDIA's local driver API.
3. Accepts only professional CAD GPU families: **NVIDIA RTX A series**, **NVIDIA RTX PRO**, and **NVIDIA Quadro RTX**.
4. Locates installed `CNEXT.exe` or `3DEXPERIENCE.exe` under Dassault Systèmes program folders and resolves it to NVIDIA's existing CATIA/3DEXPERIENCE profile.
5. Stops without changing anything if any check fails, CATIA is open, or the system is not in scope.
6. Otherwise, backs up, applies and reads back the performance settings.

The app does not support GeForce RTX. It reports this clearly and leaves all settings unchanged.

## Fixed CATIA performance profile

| NVIDIA application-profile setting | Applied value |
| --- | --- |
| Power management mode | Prefer maximum performance |
| Frame rate limiter | Off |
| Vertical sync | Off |

This profile favors viewport speed and can cause screen tearing. The application preserves global settings and the rest of NVIDIA's CATIA profile, including the manufacturer settings for antialiasing, threading, OpenGL GPU selection, shader cache, ECC and presentation method.

The NVIDIA Control Panel preference you chose — **Use the advanced 3D image settings** — is never changed or reset by this app.

## Safety and backup

Before it writes, the app exports the complete NVIDIA DRS profile database and stores the original values for the three managed settings. It writes only those settings to the resolved CATIA profile, then opens a fresh driver session and verifies all values. A failed write triggers rollback. Backups are kept at `%LOCALAPPDATA%\\CatiaGpuTuner\\Backups`.

No driver installation, power-plan change, registry import, thermal control, overclock, voltage change or telemetry is included.

## Use at your own responsibility

The app changes three NVIDIA **application-profile** settings only after its first-use confirmation. It creates a backup and verifies the written values, but every workstation, driver and CATIA/3DEXPERIENCE release can behave differently. Validate the outcome in your environment before production use. If you do not trust the tool or do not accept these changes, do not confirm the first-use dialog and no settings will be applied.

## Driver readiness versus certification

The readiness check means the local driver, NVIDIA Control Panel, supported GPU and CATIA profile are usable. It does **not** claim that the driver is Dassault-certified. Certification depends on the exact workstation, GPU, Windows and CATIA/3DEXPERIENCE release; check the [official Dassault certification catalog](https://www.3ds.com/support/hardware-and-software).

## Run

Download the release ZIP, extract it, and start `CATIA-GPU-Tuner.exe`. The application requests administrator elevation because it writes NVIDIA driver settings. Close CATIA first. The EXE is unsigned.

## Build and test

The project needs Windows and the built-in .NET Framework x64 C# compiler. No third-party dependency download is required.

```powershell
.\\Test.ps1
.\\Build.ps1
```

The automated tests cover a successful apply, selective restore, failed save, failed verification, rollback, cross-driver refusal and damaged backups. The project was also exercised on an RTX A5500 Laptop GPU with NVIDIA driver 596.71: readiness detection, CATIA profile resolution, application, read-back verification and restoration all passed.

## License

MIT. CATIA, 3DEXPERIENCE and NVIDIA RTX are trademarks of their respective owners. This project is independent and is not endorsed by Dassault Systèmes or NVIDIA.

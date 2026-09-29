# 1.1.0 — Automatic CAD workstation profile

The app is now a compact, no-selection launcher.

- No EXE path, profile, mode or preview checkbox is shown to the user.
- Automatically checks NVIDIA Control Panel, the NVIDIA driver API, GPU family and an installed CATIA/3DEXPERIENCE profile.
- Supports only NVIDIA RTX A series, RTX PRO and Quadro RTX workstation GPUs.
- Refuses to apply settings on unsupported GPUs, missing Control Panel, absent CATIA profile or an open CATIA process.
- Loads one fixed performance profile: maximum-performance power mode, no FPS cap, V-Sync off.
- Keeps the user-selected NVIDIA Control Panel advanced-3D-settings preference untouched.
- Backs up, applies and reads back all managed settings.
- Compact welcome screen with a clear ready/warning result and help.

Validation: 12 automated driver-transaction tests passed. Live readiness checks passed for NVIDIA RTX A5500 Laptop GPU / 596.71, NVIDIA Control Panel and installed CNEXT.exe → Dassault Systemes CATIA profile. A real apply/verify/restore round trip previously preserved the original driver settings.

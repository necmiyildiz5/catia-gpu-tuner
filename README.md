# CATIA GPU Tuner

Independent Windows utility for **NVIDIA RTX driver readiness checks and CATIA application-profile tuning**. Turkish interface, change preview, verified writes, automatic backups and selective rollback.

**Certification is not automatically verified.** A working NVIDIA API or a newer driver is not proof of Dassault certification. Check your workstation, GPU, operating system and CATIA release together in the [official Dassault catalog](https://www.3ds.com/support/hardware-and-software).

## Download and run

Use the ZIP under [Releases](https://github.com/necmiyildiz5/catia-gpu-tuner/releases). Extract and run `CATIA-GPU-Tuner.exe`. Windows x64, .NET Framework 4.8 and an installed NVIDIA RTX driver are required. Inspection runs without elevation; applying/restoring settings requests administrator rights. The executable is unsigned.

## Türkçe kullanım

1. NVIDIA Control Panel → **Adjust image settings with preview** → **Use the advanced 3D image settings** → Apply. Araç bu seçimi değiştirmez.
2. Başlatıcı yerine gerçek CATIA EXE dosyasını seçin: genellikle kurulumun `win_b64/code/bin` klasöründeki `CNEXT.exe` veya `3DEXPERIENCE.exe`.
3. **Sistemi incele**: GPU, sürücü, uygulama profili ve mevcut/önerilen değerleri kontrol edin. Tanınmayan uygulamaya yazma engellenir.
4. CATIA'yı kapatın. **Yedekle ve uygula** ile değişiklikleri gözden geçirip onaylayın.
5. Sonuç sürücüden tekrar okunarak doğrulanır. **Yedekten geri al**, önceki özel değerleri veya miras alınan ayar davranışını geri getirir.

## Profiles

| Setting | Balanced CAD | Smooth viewport |
|---|---|---|
| Power management | Prefer maximum performance | Prefer maximum performance |
| Frame rate limiter | Off | Off |
| Vertical sync | Application controlled | Off; possible tearing |

Only these three settings in the NVIDIA-resolved CATIA application profile are changed. Existing global settings, advanced preview choice, NVIDIA workstation presets, threading, antialiasing and GPU selection remain intact. Several CATIA executable names may share the same driver profile and therefore be affected together.

The app deliberately preserves vendor-specific settings that have no universal fastest value. The Help tab explains power management, V-Sync, threading, antialiasing, GPU selection, Vulkan/OpenGL presentation, shader cache, texture filtering and ECC. No overclocking, driver installation, thermal changes, registry import or automatic power-plan changes.

## Driver checks and limits

- Detects RTX GPUs and installed driver version using the system NVIDIA API.
- Checks live DRS access and resolves the selected executable to an existing CATIA/3DEXPERIENCE profile.
- Clearly reports **certification not verified** and links to primary references.
- Does not download a driver or claim the installed release is certified, latest, or fastest.
- This is not a benchmark. Improvements depend on the model, display mode, temperature, CPU and other bottlenecks. Compare the same model before and after.
- Windows DPI compatibility scaling is used for legible layouts on scaled displays.

## Backup design

Before each operation: full DRS binary export plus a JSON record of the three original setting values and whether each was a local override. JSON is checked against a SHA-256 sidecar (integrity check, not a signature). Backups stay in `%LOCALAPPDATA%/CatiaGpuTuner/Backups` and may contain application paths.

Restore only touches the three recorded settings. Inherited/default settings are restored by removing the added override. Cross-driver or cross-GPU restore is refused. Failed writes trigger an attempted rollback; rollback failure is reported explicitly. Do not run another profile editor during an operation. A hard process/OS crash cannot run automatic rollback; use the prepared backup afterward.

## Build and test

No third-party package restore required. On Windows, from PowerShell:

```powershell
.\Build.ps1
.\Test.ps1
```

Uses the Windows .NET Framework x64 C# compiler. `dist/CATIA-GPU-Tuner.exe` is the output. Tests use a fake driver and do not modify GPU settings. They cover successful apply, selective restore, failed save, verification failure, rollback, cross-driver rejection and damaged/unknown backups.

Read-only hardware check:

```powershell
.\dist\CATIA-GPU-Tuner.exe --check "$PWD\readiness.txt"
```

## References and license

- [NVIDIA DRS API](https://docs.nvidia.com/nvapi/group__drsapi.html)
- [NVIDIA Control Panel setting definitions](https://www.nvidia.com/content/Control-Panel-Help/vLatest/en-us/mergedProjects/nv3d/Manage_3D_Settings_(reference).htm)
- [Dassault certified hardware and software](https://www.3ds.com/support/hardware-and-software)

MIT license. CATIA, 3DEXPERIENCE and NVIDIA RTX are trademarks of their respective owners. This project is not affiliated with or endorsed by Dassault Systèmes or NVIDIA. No telemetry; the app only opens external links when requested.

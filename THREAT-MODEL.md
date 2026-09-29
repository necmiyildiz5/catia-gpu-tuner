# Threat model and safeguards

| Risk | Safeguard | Limit |
| --- | --- | --- |
| Modified release ZIP or EXE | Per-release SHA-256 manifest and local verification script | A hash only proves integrity when obtained from the official release page over HTTPS. |
| Compromised release publisher | GitHub MFA, protected default branch and future Authenticode signing | Repository/account controls must be enabled by the owner. |
| Unexpected driver changes | Fixed three-setting allowlist, CATIA-profile resolution, full backup, read-back verification and rollback | NVIDIA driver behavior remains outside this project's control. |
| Unsupported system | NVIDIA Control Panel, professional GPU, CAD profile and open-process checks | This is readiness validation, not Dassault certification. |
| Excess privilege | Administrator elevation only for NVIDIA DRS writes; no network, installers, registry import, overclocking or power-plan changes | Windows administrator access is inherently sensitive. |

The application has no network client and does not download, update, execute or inject code into other processes.

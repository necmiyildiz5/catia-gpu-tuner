# Security policy

## Supported versions

Only the latest GitHub Release is supported. Development snapshots and files from sources other than this repository's Releases page are unsupported.

## Trusted download path

Download only from the repository's GitHub Releases page. Before running the ZIP, verify its SHA-256 value with the accompanying `SHA256SUMS.txt` file using `Verify-Release.ps1`. The current executable is **not Authenticode-signed**; Windows SmartScreen may therefore show an unknown-publisher warning. Do not bypass a warning for a file whose hash does not match.

## Report a vulnerability

Do not open a public issue for a suspected security vulnerability. Use GitHub's private vulnerability-reporting channel when it is enabled for this repository. If it is unavailable, contact the repository owner privately from the GitHub profile and include: affected version, reproduction steps, impact and any logs with personal paths removed.

## Scope and response

Reports involving release integrity, privilege use, backup handling or unintended NVIDIA settings are in scope. The maintainer aims to acknowledge reports within 7 days and publish a fix or mitigation as soon as practical.

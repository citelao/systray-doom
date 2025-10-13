# Changelog

## CURRENT
* META: Add AGENTS.md for guiding agents.
* META: Add basic PR validation.
* META: Adopt Nerdbank.GitVersioning with shared configuration and remove hard-coded package version.
* META: Document versioning flow, including the `dev` prerelease suffix and future automation follow-up.
* META: Document how shallow-clone agents should set `NBGV_GitEngine=Disabled` instead of unshallowing during builds.

## v0.1.3
* BUG: Fixed an issue where construction failures in `TrayIcon` crashed the host HWND.

## v0.1.2
* FEATURE: Mark library as AOT-compatible & added tools to validate that assumption.

## v0.1.1
* BUG: Update logging to use an ILogger, instead of spamming Console.WriteLine

## v0.1.0 & v0.1.0.1

* Initial release

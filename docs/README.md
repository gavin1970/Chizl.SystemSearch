# Chizl.SystemSearch

High-performance, cross-platform .NET library for scanning drives and searching files and folders — includes a fully usable Windows demo UI.

---

## Project Information

- **Type**
  - Multi-platform class library

- **Written In**
  - Visual Studio 2022

- **Target Frameworks**
  - .NET Standard 2.0
  - .NET Standard 2.1
  - .NET 8
  - .NET 10

---

## Overview

Chizl.SystemSearch provides fast, in-memory searching of files and folders across attached drives.
Instead of relying on disk-based indexing, the library caches full file paths in memory while the application is running,
allowing searches to complete in seconds after the initial scan.  Live drive monitoring is supported via FileSystemWatcher, 
ensuring the in-memory cache stays up-to-date with changes.

A Windows demo UI is included to showcase real-world performance and usage.

---

## Target Frameworks and Compatibility

### Library

- netstandard2.0
- netstandard2.1
- net8.0
- net10.0

### Demo UI (Windows)

- .NET Framework 4.8.1
- Fully usable demo for Windows drive scans and searches

---

## Demo UI Behavior and Performance

- Automatically detects all attached drives
- Uses FileSystemWatcher to track add / remove / rename events
- Maintains an in-memory ConcurrentDictionary of full file paths
- No indexing files are written to disk

---

## Performance (real-world)

This library is designed for speed: it caches **full file paths in memory** (ConcurrentDictionary) while the UI is running,
so searches remain fast after the initial scan.

**Laptop+External 2TB drive example**
- CPU affinity max (8 cores).
- ~1TB of combined file data
- ~1.2M files across 2 drives
- ~330k folders
- Initial scan: **< 1 minute**
- Warm scans: **< 30 seconds**
- Memory usage: **~500 MB**
- Searches after scan: typically **< 2 seconds**

**VDI example (restricted CPU)**
- Running in VDI with CPU affinity limited (1 core)
- Full scan: ~**1.7 minutes**
- Warm scans: **< 1 minute**
- Searches after scan: typically **< 2 seconds**

> Scan time depends on hardware, drive type, antivirus, and CPU limits — but search speed stays consistently fast because it runs against the in-memory path cache.

---

## Search Syntax and Tokens

Searches are case-insensitive by default and may include literal text, wildcards, and search tokens.

### Literal Search

Matches words and spaces as-is.

Example:
`Google Gemini`

### Wildcards

Use `*` to match intervening text.

Example:
`Google*Gemini`

---

### Search Tokens

Search tokens are structured filters enclosed in square brackets.

Format:
`[label:value1|value2|...]`

Supported token labels:

- includes / include / inc
- excludes / exclude / exc
- extensions / extension / ext

---

### NOEXT Behavior

- Includes: matches files without extensions
- Excludes: removes files without extensions
- Extensions: treats NOEXT as a valid extension token

---

## Thread Safety

Uses Interlocked and Volatile for cross-platform atomic operations.
A custom Bool type replaces legacy integer flags.

---

## Versioning

Semantic Versioning is used for releases.

---

## Contributing

Issues and pull requests are welcome.

# Chizl.SystemSearch

High-performance, cross-platform .NET library for scanning drives and searching files and folders — includes a fully usable Windows demo UI.

---

## Project Information

- **Type**
  - Multi-platform class library

- **Written In**
  - Visual Studio 2022/2026

- **Tested with Target Frameworks**
  - .NET Standard 2.0
  - .NET Standard 2.1
  - .NET 8
  - .NET 10

---

## Overview

Chizl.SystemSearch provides fast, in-memory searching of files and folders across attached drives.

Instead of relying on disk-based indexing, the library caches full file paths in memory while the application is running, allowing searches to complete in seconds after the initial scan.  Live drive monitoring is supported via FileSystemWatcher, ensuring the in-memory cache stays up-to-date with changes, the library reads files on-demand to avoid unnecessary disk I/O.  

When content searching is used, the search for include, extension, and exclusions are also applied to ensure results are accurate and fast.  **NOTE**: Before Content searching occurs it validates that files are not binary and that they are not larger than:

> 250 Megabyte (MB): Strictly $2^{28}$ ($268,435,456$) bytes

Content searching will be slower than a file and or path searching, but still 10x faster than traditional disk-based content searches.  Results will show up in the grid of count of content matches per file.  On mouseover the count, the user can see the actual content matches in a tooltip.  The tooltip will show the count and the text around the match request.  Right click on the content count in the grid to copy the content matches to the clipboard for further use.

A Windows demo UI is included to showcase real-world performance and usage.  The UI has been setup to run in the system tray and automatically detects attached drives, allowing users to quickly scan and search their files without needing to write any code.

---

## Target Frameworks and Compatibility

### Library

> The library can build with all the other following targeted frameworks, but doesn't require more than one to build.

- netstandard2.0
- netstandard2.1
- net8.0
- net10.0

### Demo UI (Windows)

> The UI is fully functioning, but any developer can replace it and call the library directly.

- .NET Framework 4.8.1
- Fully usable demo for Windows drive scans and searches

---

## Demo UI Behavior and Performance

- Automatically detects all attached drives
- Uses FileSystemWatcher to track add / remove / rename events
- Maintains an in-memory ConcurrentDictionary of full file paths
- No indexing files are written to disk.
- Memory is used to cache only file paths in memory.  
  - On response from library, the UI uses FileInfo object, using the full file path, to provide file information during search

---

## Performance (real-world)

This library is designed for speed: it caches **full file paths in memory** (ConcurrentDictionary) while the UI is running,
so searches remain fast after the initial scan.

### Laptop+External 2TB drive example

- CPU affinity max (8 cores).
- ~1TB of combined file data
- ~1.2M files across 2 drives
- ~330k folders
- Initial scan: **< 1 minute**
- Warm scans: **< 30 seconds**
- Memory usage: **~500 MB**
- Searches after scan: typically **< 2 seconds**

### VDI example (restricted CPU)

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

Example: `Google Gemini`

### Wildcards

Use `*` to match intervening text.

Example: `Google*Gemini`

---

### Search Tokens

Search tokens are structured filters enclosed in square brackets.

Format:
`[label:value1|value2|...]`

Supported token labels (first 3 letters, singular, and plural are all supported as the same token label):

- includes / include / inc
  - Examples: — includes files or paths containing "report" or "summary"
    1. `[inc:report|summary]`
    1. `[include:report|summary]`
    1. `[includes:report|summary]`
- excludes / exclude / exc
  - Examples: — excludes files or paths containing "backup" or "old"
    1. `[exc:backup|old]`
    1. `[exclude:backup|old]`
    1. `[excludes:backup|old]`
- extensions / extension / ext
  - Examples: — includes files with extensions of .LOG, .TXT, or .MD
    1. `[ext:log|txt|md]`
    1. `[extension:log|txt|md]`
    1. `[extensions:log|txt|md]`
- contents / content / con
  - Examples: — includes files with contents containing specific text like "my house" or "my home".
    1. `[con:my house|my home]`
    1. `[content:my house|my home]`
    1. `[contents:my house|my home]`

### Full Search Example

`Google*Gemini [inc:report|summary] [exc:backup|old] [ext:log|txt|md] [con:my house|my home]`

---

### NOEXT Behavior

1. Includes: matches files without extensions
    - Example: `[inc:NOEXT]` — includes files without an extension in results.
1. Excludes: removes files without extensions
    - Example: `[exc:NOEXT]` — removes files without an extension from results.
1. Extensions: treats NOEXT as a valid extension token
    - Example: `[ext:NOEXT|txt]` — includes only files with no extension or with a .txt extension.

### Query Order Behavior

- Search tokens are not processed in the order they appear in the query.
- Instead, the library processes them in a specific sequence to ensure consistent results:
  1. **Search Text**: First, the library applies the literal search text to filter the results.
  1. **Includes**: Next, the library applies all include tokens to filter the results down to only those that match the specified criteria.
  1. **Extensions**: Next, it applies extension tokens to further narrow down the results based on file extensions.
  1. **Excludes**: Applies exclude tokens to remove any results that match the specified exclusion criteria.
  1. **Contents**: Finally, if contents token is used, it will open ALL Non-Binary filtered files based on previous tokens and search for all tokens separated by `|`.  

```txt
Tokens can use spaces, example: `[contents:Google Gemini|Microsoft Corp]`
Binary files are not based on file extensions.  Each filtered file is opened and check to ensure it's ASCII before token search.  This validation supports BOM Ascii as non-binary.
```

### Example of this behavior

`Google + [extensions:py|pdf|cs|noext] + [excludes:c:|noext] + [contents:google gemini]`

> Exclusions are applied last for full file path and filename search, so even if NOEXT is included in the extensions token, it will be excluded from results because of the excludes token. If contents token is used, exclusions occur before files are opened and content is found.

---

## Thread Safety

Uses Interlocked and Volatile for cross-platform atomic operations.
A custom Bool type replaces legacy integer flags.

---

## Versioning

Semantic Versioning is used for releases.

- Major.Minor.Patch.Build (Year-2020.month.day.GMT)

---

## Contributing

Issues and pull requests are welcome.

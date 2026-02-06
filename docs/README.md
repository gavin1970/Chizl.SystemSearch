# Chizl.SystemSearch Library /w Usable Demo UI

## Project Information
- What Is It: 
	- ![Class Library](https:// img.shields.io/badge/Multi_Platform-Class_Library-orange)
- Written In: 
	- ![Visual Studio](https:// badgen.net/badge/icon/VisualStudio?color=blue&icon=visualstudio&label)![v2022](https:// badgen.net/badge/visualstudio/2022/red?labelColor=blue&color=red&label)
- Target Frameworks: 
	- ![NetStandard](https:// img.shields.io/badge/.NET_Standard-gray)![](https:// img.shields.io/badge/v2.0-red)![](https:// img.shields.io/badge/v2.1-blue)
	- ![NET](https:// img.shields.io/badge/.NET8-red)
- Short Description:
	- `Cross platform library for drive scan file/folder searches with fully functioning Windows .NET Framework 4.8.1 demo UI.`

## Target Frameworks and Compatibility

### Library:
* Is compatible with a wide range of .NET implementations, from older versions to the latest.  The library has been built under net6.0 without issues for those that need to downgrade.
  * **netstandard2.0**
  * **netstandard2.1**
  * **net8.0**
  * **net10.0**

### UI:
* Is UI is build in Framework 4.8.1 as a demo, but fully usable for Window scans and searches.
* It's configured to auto pick up all Drives attached.
* On startup, library sets FileWatcher up on all drives and auto update cached files when the system add, removes, or renames files/folders. A new scan is not required until UI is closed and reopened.
* With 1.2 Terabytes and over 1 1/2 million files across 2 drives, a full scan takes less than one minute during first scan.  An hour later the same full scan takes less than 30 seconds.  This is done by storing ONLY full paths of each file in a ConcurrentDictionary and only held there while the UI is running.  No files are stored to disc and with 1 1/2 million files paths in memory, takes up 400MB in memory.  Still less than most applications running normally.
* After a full scan is done and before you close the UI, any search looks at the dictionary and returns in seconds.
<img width="1642" height="953" alt="image" src="https://github.com/user-attachments/assets/da2e70af-b722-4b6e-9ee6-88008e150b15" /><br/>
<img width="1642" height="953" alt="image" src="https://github.com/user-attachments/assets/196f601e-4fac-4d8a-8b9b-e37d06e53e57" />

### Search Understanding with Search Tokens:
* Literal Search.
	- All searches are not case sensitive.
	- Configuration next to the `Find` button will allow `Folder` and/or `File` search criteria and allow include/exclude of special folders.  e.g. [RecycleBin, Temp, Internet Cache, Profile, Windows]
	- If File and Folder Results are configured to return, a search of `Google Gemini` searches for the full string, including the space on all folders and files.
	- Search, `Google*Gemini` searches for `Google` first, in folder or file, then `Gemini` must follow somewhere in the full path.  e.g. `c:\mycode\google\ai\gemini.config`
		- If Folder results is turned off, `c:\mycode\google\ai\gemini.config` would not return.  `google` and `gemini` must be contained in the filename.
* Search Tokens
	- All Search Tokens require square brackets around the whole search token. None of these search tokens are not limited to how many token values they can have.
	- A Search Token group starts after the open bracket and must be labeled and followed by a colon.  e.g. `ext:`, `includes:`, or `excludes:`.
	- Includes, full path must contain one of the token values. e.g. `[ includes : \users\ | c:\windows ]`
	- Exclude, full path can not contain any of the token value. e.g. `[ excludes : \users\ | windows | d: ]`
	- For multiple values in a search token require a separation with `|` and can have spaces for visual appearance, but spaces will be auto removed if exists.
	- Periods `.` are not required, but can exists for file extension. e.g. `[ext:pdf|doc|docx]`, `[ext:.pdf|.doc|.docx]` or `[ ext : pdf | doc | docx ]`
	- Multiple search tokens can be used and can be separated with ` ` or  ` + `.  It's your personal preference.
* Search Example and Expected Results
  - Ex1: `microsoft + [excludes:\windows\] + [ext:pdf|docx|xls|xlsx|txt] + [includes:\downloads\|drive]`
  - Ex2: `win*system + [excludes: D:] + [ext:pdf|docx|xls|xlsx|txt] + [includes:\downloads\|drive]`
  - Ex3: `version + [includes:git|nuget] + [excludes:\godaddy\|win|d:] + [ext:xml|txt]`
		- `version` - If File and Folder Results are checked in the UI, the folder or filename must contain `version`.
		- `[includes:git|nuget]` - The full path **must** contain `git` **or** `nuget`.  This is applied even if you have Folder Results set to off.
		- `[excludes:\godaddy\|win|d:]` - The full path can **not** contain `\godaddy\` **or** `win` **or** `D:` drive.  This would include `C:\Windows` and all files on your D: drive.
		- `[ext:xml|txt]` - The file **must** have a `.xml` **or** `.txt` as a file extension.

		- Accepted Examples:
		```
		C:\Users\<Profile>\.nuget\packages\microsoft.netcore.app.runtime.mono.iossimulator-x64\7.0.11\Microsoft.NETCore.App.versions.txt
		C:\Users\<Profile>\.nuget\packages\microsoft.netcore.app.runtime.osx-x64\7.0.10\Microsoft.NETCore.App.versions.txt
		C:\Program Files\Git\etc\package-versions.txt
		C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\etc\package-versions.txt
		C:\Users\<Profile>\AppData\Local\GitHubDesktop\app-3.5.0\resources\app\git\etc\package-versions.txt
		C:\Users\<Profile>\.nuget\packages\microsoft.netcore.app.ref\3.1.0\ref\netcoreapp3.1\System.Diagnostics.FileVersionInfo.xml
		C:\Users\<Profile>\.nuget\packages\microsoft.netcore.app.ref\8.0.12\ref\net8.0\System.Diagnostics.FileVersionInfo.xml
		C:\Users\<Profile>\.nuget\packages\microsoft.netcore.app.ref\8.0.13\ref\net8.0\System.Diagnostics.FileVersionInfo.xml
		C:\Program Files (x86)\Microsoft SDKs\NuGetPackages\system.reflection.emit\4.7.0\version.txt
		C:\Program Files (x86)\Microsoft SDKs\NuGetPackages\system.reflection.metadata\1.6.0\version.txt
		C:\Program Files (x86)\Microsoft SDKs\UWPNuGetPackages\microsoft.netcore.platforms\2.1.0\version.txt
		C:\Users\<Profile>\.nuget\packages\microsoft.bcl.asyncinterfaces\1.0.0\version.txt
		C:\Users\<Profile>\.nuget\packages\microsoft.bcl.asyncinterfaces\1.1.0\version.txt
		C:\Users\<Profile>\.nuget\packages\microsoft.bcl.asyncinterfaces\1.1.1\version.txt
		```			

## Atomic Operations and Thread Safety
#### Chizl.SystemSearch library utilizes modern atomic operations for thread safety. 
This library employs Volatile.Read&lt;T&gt; and Interlocked.Exchange&lt;T&gt; for atomic read and write operations, ensuring thread safety across supported platforms.

#### Deprecation of Legacy Patterns
Legacy patterns using int flags for boolean values have been deprecated in favor of production ready custom "Bool" class used like bool types, with atomic operations, aligning with modern .NET practices.

## Platform Assumptions
This library assumes the use of supported .NET runtimes. Platforms that are no longer supported by Microsoft are not considered for compatibility.

## Versioning and Updates
Versioning follows Semantic Versioning principles. Updates are made to maintain compatibility with supported .NET runtimes and to incorporate modern .NET features.

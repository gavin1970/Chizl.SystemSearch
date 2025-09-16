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
* Is compatible with a wide range of .NET implementations, from older versions to the latest.
  * <b>netstandard2.0</b>
  [![Build Status](https:// travis-ci.org/username/repo-name.svg?branch=master)](https:// travis-ci.org/username/repo-name)
  * netstandard2.1
  * net8.0

### UI:
* Is UI is build in Framework 4.8.1 as a demo, but fully usable for Window scans and searches.

## Atomic Operations and Thread Safety
#### Chizl.SystemSearch library utilizes modern atomic operations for thread safety. 
This library employs Volatile.Read<T> and Interlocked.Exchange<T> for atomic read and write operations, ensuring thread safety across supported platforms.

#### Deprecation of Legacy Patterns
Legacy patterns using int flags for boolean values have been deprecated in favor of production ready custom "Bool" class used like bool types, with atomic operations, aligning with modern .NET practices.

## Platform Assumptions
This library assumes the use of supported .NET runtimes. Platforms that are no longer supported by Microsoft are not considered for compatibility.

## Versioning and Updates
Versioning follows Semantic Versioning principles. Updates are made to maintain compatibility with supported .NET runtimes and to incorporate modern .NET features.

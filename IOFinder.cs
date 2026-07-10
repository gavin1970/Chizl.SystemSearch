using Chizl.ThreadSupport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chizl.SystemSearch
{
    public class IOFinder : IDisposable
    {
        private const long SEND_MSG_NEXT_STEP = 100;
        private const int BYTE_SIZE_READS = 4096;
        private const int MAX_FIND_RESPONSE  = 10000;                           // max response content to UI
        private const int FILE_BUFFER_READ_SIZE = 1024 * 1024;                  // 1MB
        private const long MAX_FILE_SIZE_CONTENT_SEARCH = 1024 * 1024 * 250;    // 250MB: (262,144,000) bytes
        private static readonly string _separator = new string('⸏', 25);        // ⟷  ⟚   ⸏
        private static readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
        private static CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();
        private static long _fileScanned = 0;
        private static long _nextFileAlert = 0;

        // since there will be a lot of threading going on, these status need to be seen by all threads.
        private static bool disposedValue;
        //private static long _fileContentCount = 0;

        private static ConcurrentQueue<(WatcherChangeTypes, string, string)> _systemUpdates = new ConcurrentQueue<(WatcherChangeTypes, string, string)>();
        private static ABool _queProcessing = ABool.False;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    StopScan();

                Scanner.Dispose();
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public IOFinder()
        {
            SearchMessage.EventMessaging += SearchMessage_EventMessaging;
        }

        #region Private
        // we want a new instance of the scanner each time, to ensure we have a clean
        // cache, and to prevent any threading issues with multiple searches going on at once.
        internal SystemScan Scanner => new SystemScan();
        //public bool AllowBinaryContentSearch { get { return _allowBinaryContentSearch; } set { _allowBinaryContentSearch = value; } }
        // we want to run the scan async, but we want the search to
        // wait for the scan to complete before it starts searching the cache.
        private Task Scan(string[] drives, bool sendMsg, bool isRescan) => Scanner.ScanDrives(drives, sendMsg, isRescan);
        // we want to run the search async, but we want it to wait for
        // the scan to complete before it starts searching the cache.
        private Task Search(string[] drives, string searchCriteria, bool isRescan)
        {
            var searchTask = Task.Run(async () =>
            {
                GlobalSettings.Startup(LookupStatus.Running);
                _cancelTokenSource = new CancellationTokenSource();

                if (!GlobalSettings.FullScanCompleted)
                {
                    SearchMessage.SendMsg(SearchMessageType.UpdateInProgress, $"Full scan hasn't been completed. Please wait...");
                    await Scan(drives, false, isRescan);
                }
            })
            .ContinueWith(previousTask =>
            {
                // synchronous
                SearchCache(drives, searchCriteria);
                return previousTask;
            });

            return searchTask;
        }
        private void StopWatchers()
        {
            if (_watchers.Count > 0)
            {
                foreach (var watcher in _watchers)
                {
                    watcher.Created -= OnCreated;
                    watcher.Deleted -= OnDeleted;
                    watcher.Renamed -= OnRenamed;
                }
                _watchers.Clear();
            }
        }
        /// <summary>
        /// Searches the cache for the given search criteria.  If the cache is not fully built, it will wait for the 
        /// scan to complete before searching.  Event messages will be sent to subscribers with results and status updates.
        /// </summary>
        /// <param name="drives"></param>
        /// <param name="searchCriteria"></param>
        /// <param name="sendMsg"></param>
        /// <returns></returns>
        private bool SearchCache(string[] drives, string searchCriteria, bool sendMsg = true)
        {
            var retVal = false;

            if (string.IsNullOrWhiteSpace(searchCriteria))
            {
                SearchMessage.SendMsg(SearchMessageType.ScanComplete, $"Cached: [{SystemScan.ScannedFolders.FormatByComma()}] Folders, [{SystemScan.ScannedFiles.FormatByComma()}] Files.");
                return retVal;
            }

            try
            {
                // build the search criteria commands, this will help with the
                // search, and prevent us from having to parse the criteria multiple times.
                var buildSearchCriteria = new BuildSearchCmd(ref searchCriteria);

                if (sendMsg)
                {
                    SearchMessage.SendMsg(SearchMessageType.UpdateInProgress, $"Search cache for '{searchCriteria}'.");
                    SearchMessage.SendMsg(SearchMessageType.SearchQueryUsed, searchCriteria);
                }

                // search the cache for the criteria, this will return a list of files that match the
                // criteria, but we still need to verify them against the criteria, as some of the
                // criteria is not able to be pre-filtered in the cache.
                retVal = DeepDive(drives, buildSearchCriteria);

                // send total count message, to ensure accuratness
                SearchMessage.SendMsg(SearchMessageType.ScanComplete, $"Cached: [{SystemScan.ScannedFolders.FormatByComma()}] Folders, [{SystemScan.ScannedFiles.FormatByComma()}] Files.");
            }
            finally
            {
                GlobalSettings.CurrentStatus = LookupStatus.Completed;
            }

            return retVal;
        }
        /// <summary>
        /// IsBinary(): Has BOM detection for UTF encoding UTF-8 BOM, UTF-16 LE/BE and future UTF-32 LE/BE ASCII<br/>
        /// </summary>
        /// <param name="fullPath">Path of file</param>
        /// <param name="errorMsg">if method returns false, errorMsg will have reason.</param>
        /// <param name="bytesToCheck">How many bytes to check within the header.  If the file is smaller than bytesToCheck, it will only pull what.</param>
        /// <returns>true - if binary found.  false if </returns>
        private bool IsBinary(string fullPath, out string errorMsg, int bytesToCheck = 1024)
        {
            errorMsg = string.Empty;

            try
                {
                using (FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BYTE_SIZE_READS, FileOptions.Asynchronous))
                {
                    byte[] buffer = new byte[bytesToCheck];
                    int bytesRead = fs.Read(buffer, 0, bytesToCheck);

                    //UTF-16 Check
                    if (bytesRead >= 2)
                    {
                        if (buffer[0] == 0xFF && buffer[1] == 0xFE) return false; // UTF-16 LE
                        if (buffer[0] == 0xFE && buffer[1] == 0xFF) return false; // UTF-16 BE
                    }

                    //UTF-8 Check
                    if (bytesRead >= 3)
                    {
                        //BOM detection for UTF encoding
                        if (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF) return false; // UTF-8 BOM
                    }

                    //UTF-32 Check
                    if (bytesRead >= 4)
                    {
                        if (buffer[0] == 0xFF && buffer[1] == 0xFE && buffer[2] == 0x00 && buffer[3] == 0x00) return false; // UTF-32 LE
                        if (buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0xFE && buffer[3] == 0xFF) return false; // UTF-32 BE
                    }

                    for (int i = 0; i < bytesRead; i++)
                    {
                        // if no BOM found above, but null terminators
                        // are still found, we will flag as binary.
                        if (buffer[i] == 0x00)
                            return true;        // definitely binary
                    }
                }
            }
            catch (Exception ex)
            {
                errorMsg = $"IsBinary('{fullPath}') Exception:{Environment.NewLine}{ex.Message}";
            }

            // BOM already checked.
            // No other string terminators '0x00' found.
            // Not binary.
            return false;
        }
        /// <summary>
        /// Count's finding, and return's line, line #, and char position for each.
        /// </summary>
        /// <param name="path">File to search for content</param>
        /// <param name="searchTextArr">text to look for in content</param>
        /// <param name="comparison">case sensitivy setting</param>
        private IEnumerable<SearchHit> SearchFile(string path, string[] searchTextArr, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            var fi = new FileInfo(path);
            if (!fi.Exists || fi.Length > MAX_FILE_SIZE_CONTENT_SEARCH)
                yield break;

            var enmIsBinary = IsFileBinary.NotVerified;

            if (!this.AllowBinaryContentSearch)
            {
                enmIsBinary = IsBinary(path, out _) ? IsFileBinary.Yes : IsFileBinary.No;
                if (enmIsBinary.Equals(IsFileBinary.Yes))
                    yield break;
            }

            if (_cancelTokenSource.IsCancellationRequested || GlobalSettings.HasShutdown)
                yield break;

            List<string> searchTextList = searchTextArr.Where(w => !string.IsNullOrWhiteSpace(w)).ToList();

            if (!searchTextList.Any())
                yield break;

            using var fileStream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                FILE_BUFFER_READ_SIZE,
                FileOptions.SequentialScan);

            using var reader = new StreamReader(
                fileStream,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: FILE_BUFFER_READ_SIZE);

            string line = string.Empty;
            long lineNumber = 0;
            var lowerPath = path.ToLower();

            while ((line = reader.ReadLine()) != null)
            {
                if (_cancelTokenSource.IsCancellationRequested || GlobalSettings.HasShutdown)
                    yield break;

                lineNumber++;
                var cleanLine = line.Replace('\0', '.');

                foreach (var searchText in searchTextList)
                {
                    if (_cancelTokenSource.IsCancellationRequested || GlobalSettings.HasShutdown)
                        yield break;

                    int startIndex = 0;

                    while (true)
                    {
                        if (_cancelTokenSource.IsCancellationRequested || GlobalSettings.HasShutdown)
                            yield break;

                        int index = cleanLine.IndexOf(searchText, startIndex, comparison);

                        if (index < 0)
                            break;

                        var len = searchText.Length > 50 ? 50 : 50 - searchText.Length;
                        var snippet = string.Empty;
                        var snipStart = 0;
                        var snipLength = 0;
                        try
                        {
                            snipStart = index - searchText.Length < 0 ? 0 : index - searchText.Length;
                            snipLength = snipStart + len > cleanLine.Length ? cleanLine.Length - snipStart : len;
                            // covers files from windows \r\n, linux \n, and old mac \r
                            snippet = cleanLine.Substring(snipStart, snipLength).Replace("\n", ".").Replace("\r", ".").Trim();  
                        }
                        catch (Exception ex)
                        {
                            SearchMessage.SendMsg(ex, $"SearchFile('{path}')");
                            yield break;
                        }

                        yield return new SearchHit(
                                searchText,
                                lineNumber,
                                index + 1,  // 0 based - char loc in line
                                line,
                                snippet,
                                enmIsBinary);

                        startIndex = index + searchText.Length;
                    }
                }
            }
        }
        /// <summary>
        /// Async file read and builds snippets around findings.
        /// </summary>
        /// <param name="filePath">file to search</param>
        /// <param name="contentList">list of content to search for</param>
        /// <returns></returns>
        private Task<bool> FindContent(string filePath, List<SearchCommand> contentList)
        {
            return Task.Run(() =>
            {
                var snippet = string.Empty;
                var lineText = string.Empty;
                var findings = new List<SearchHit>();

                try
                {
                    findings.AddRange(SearchFile(filePath, contentList.Select(s => s.Search).ToArray()));
                    int totalSnipCount = 0;

                    List<string> snips = new List<string>();
                    var lastSearched = string.Empty;

                    foreach (var finding in findings)
                    {
                        if (_cancelTokenSource.IsCancellationRequested || GlobalSettings.HasShutdown)
                            throw new TaskCanceledException();

                        // check if we can get a max of 50 bytes total for the snippet
                        var snipit = finding.Snippet.Length > 50 ? finding.Snippet.Substring(0, 50) : finding.Snippet;
                        // check if we trim down to a max of 50 bytes total displaying the one single content token being searched
                        var searched = finding.Searched.Length > 50 ? $"{finding.Searched.Substring(0, 50)}..." : finding.Searched;

                        var newItem = false;
                        // if the last found token is different than this token, we need to set newItem for HR in the mouseover Tip 
                        if (!searched.Equals(lastSearched)) {
                            lastSearched = searched;
                            newItem = true;
                        }

                        if (totalSnipCount < 100)
                            snips.Add($"{(newItem ? $"{_separator}\r{searched}\r" : "")}@ ln:{finding.LineNumber}:pos:{finding.CharPosition}\r   - {snipit.MakeReadable()}");
                        else
                            Debug.WriteLine($"(A) {totalSnipCount}: {filePath}");

                        totalSnipCount += 1;
                    }

                    if (totalSnipCount > 0)
                    {
                        // This is the same thread for all snips for each single file.
                        // So there is no reason to worry about prevValue being different
                        // by the time TryUpdate is called.
                        if (FileContentFinds.TryGetValue(filePath, out (string cnt, string[] snips) prevValue))
                        {
                            // append new conten finds 
                            var newSnipsList = prevValue.snips.ToList();
                            var newTotal = prevValue.snips.Length + snips.Count;

                            if (newTotal > 100 && prevValue.snips.Length < 100)
                            {
                                var takeCnt = 100 - prevValue.snips.Length;
                                if (takeCnt > 0)
                                    newSnipsList.AddRange(snips.Take(takeCnt));
                            }
                            else
                                newSnipsList.AddRange(snips);

                            FileContentFinds.TryUpdate(filePath, ($"{{{(totalSnipCount == newSnipsList.Count ? "" : $"{newSnipsList.Count}/")}{totalSnipCount}}}", newSnipsList.ToArray()), prevValue);
                        }
                        else
                        {
                            if (totalSnipCount > 100)
                                Debug.WriteLine($"(B) {totalSnipCount}: {filePath}");

                            FileContentFinds.TryAdd(filePath, ($"{{{(totalSnipCount == snips.Count ? "" : $"{snips.Count}/")}{totalSnipCount}}}", snips.ToArray()));
                        }

                        return true;
                    }
                }
                catch (OperationCanceledException)
                {
                    //ignore, it's standard for CancelTokenSource.Cancel
                }
                catch (Exception ex)
                {
                    // if we can't read the file, we just skip it, but log the error.
                    //Debug.WriteLine($"{ex.Message} -- Findings: {findings.Count}, LineText: {lineText}, Start: {start}, Length: {len}, Snippet: {snippet}");
                    SearchMessage.SendMsg(ex, $"Error reading file: {filePath}");
                }
                return false;
            });
        }
        private bool DeepDive(string[] drives, BuildSearchCmd searchCriteria)
        {
            _fileScanned = 0;
            _nextFileAlert = 0;

            var startTime = DateTime.UtcNow;
            var totalTime = string.Empty;
            var contentSearchCnt = 0;

            // start over
            FileContentFinds.Clear();

            var retVal = false;
            var fullFileList = Scanner.FileDictionary;
            var fullFileListLen = fullFileList.Count();
            // Moved from List<string> to ConcurrentDictionary to prevent Duplication.
            // TryAdd() is faster than List.Contains() + List.Add().
            ConcurrentDictionary<string, bool> findingsDic = new ConcurrentDictionary<string, bool>();
            var filters = new List<(string Path, bool HasExt)>();

            // we want to loop through the search criteria, and filter down the list of files based on the criteria.
            var pathList = searchCriteria.Commands.Where(w => w.CommandType == CommandType.Includes).ToList();
            // we can pre-filter the list based on the extensions, this will help with the search, and prevent us from having to parse the criteria multiple times.
            var extList = searchCriteria.Commands.Where(w => w.CommandType == CommandType.Extensions).ToList();
            // we can pre-filter the list based on the excludes, this will help with the search, and prevent us from having to parse the criteria multiple times.
            var filterList = searchCriteria.Commands.Where(w => w.CommandType == CommandType.Excludes).ToList();
            // we can pre-filter the list based on the excludes, this will help with the search, and prevent us from having to parse the criteria multiple times.
            var contentList = searchCriteria.Commands.Where(w => w.CommandType == CommandType.Contents).ToList();

            // we want to loop through the search criteria, and filter down the list of files based on the criteria.
            for (int i = 0; i < searchCriteria.SearchCriteria.Length; i++)
            {
                if (_cancelTokenSource.IsCancellationRequested || GlobalSettings.HasShutdown)
                    break;

                var wc = searchCriteria.SearchCriteria[i];
                var prevDicCount = findingsDic.Count();
                var hasData = findingsDic != null && findingsDic.Count > 0;

                if (wc.Length == 1 && wc.Equals(Seps.cContentsPos.ToString()) && contentList.Count() > 0)
                {
                    // not looking for more if the first parts don't exist.
                    if (!hasData)
                        continue;

                    List<string> removePaths = new List<string>();
                    ConcurrentDictionary<string, bool> contentFindings = new ConcurrentDictionary<string, bool>();

                    retVal = true;
                    // [includes:D:\] + [extensions:log|md] + [contents:gavin]
                    List<(string file, bool noext)> dicList = findingsDic.Select(s => (s.Key, s.Value)).Cast<(string, bool)>().ToList();
                    // for later
                    contentSearchCnt = dicList.Count;
                    // reset
                    findingsDic.Clear();
                    // thread monitor
                    var searchTask = new List<Task>();
                    // set in file status files found that will be searched.
                    SearchMessage.SendMsg(SearchMessageType.FileScanStatus, $"Found: [{contentSearchCnt.FormatByComma()}] files to scan for content.");

                    var semaphore = new SemaphoreSlim(1024);
                    // [includes:d:\code\|c:\code\] + [extensions:cs|md|log] + [excludes:\3rdparty\|\Unity\|SVG] + [contents:gavin|landon]
                    // [includes:d:\code\] + [extensions:cs|md|log] + [excludes:\3rdparty\|\Unity\|SVG] + [contents:gavin|landon]

                    foreach ((string file, bool noext) in dicList)  //loadList
                    {
                        semaphore.WaitAsync();
                        try
                        {
                            if (_cancelTokenSource.IsCancellationRequested || GlobalSettings.HasShutdown)
                                break;

                            // we need to verify the criteria again, as some of the criteria is not able to be pre-filtered
                            // in the cache, and we don't want to waste the time trying to read the file if it doesn't
                            // meet the criteria.
                            if (VerifyCriteria(drives, file, searchCriteria))
                            {
                                if (_cancelTokenSource.IsCancellationRequested || GlobalSettings.HasShutdown)
                                    break;

                                // create task for each content search, this will help with performance, as we
                                // can search for multiple content criteria at the same time, and it will also
                                // help with large files, as we can read them in parallel.
                                // FindContent verifies the file is not binary and less than 1 GB before
                                // reading, so we don't have to worry about that here.
                                searchTask.Add(FindContent(file, contentList).ContinueWith(t =>
                                {
                                    if (t.Result)
                                        findingsDic.TryAdd(file, noext);

                                    if (Interlocked.Increment(ref _fileScanned) > _nextFileAlert)
                                    {
                                        Interlocked.Exchange(ref _nextFileAlert, _nextFileAlert += (_fileScanned == 1 ? SEND_MSG_NEXT_STEP - 1 : SEND_MSG_NEXT_STEP));
                                        SearchMessage.SendMsg(SearchMessageType.StatusMessage, $"File criteria match: [{contentSearchCnt.FormatByComma()}].\nFound content in [{findingsDic.Count().FormatByComma()}] files out of [{_fileScanned.FormatByComma()}] already scanned.");
                                    }
                                }));
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }

                    try
                    {
                        Task.WaitAll(searchTask.ToArray(), _cancelTokenSource.Token);
                        totalTime = DateTime.UtcNow.Subtract(startTime).FormatAsTimer();
                    }
                    catch (OperationCanceledException)
                    {
                        SearchMessage.SendMsg(SearchMessageType.StatusMessage, $"User canceled: [{findingsDic.Count().FormatByComma()}] files were matched before cancel occured.");
                        break;
                    }

                    var updateMsg = $"Found: [{findingsDic.Count().FormatByComma()}] files matching content.\n" +
                                                                                $"Took [{totalTime}] to " +
                                                                                $"scan inside [{contentSearchCnt.FormatByComma()}] files.";
                    // if there was a content search, even if no files found.
                    SearchMessage.SendMsg(SearchMessageType.UpdateInProgress, updateMsg);
                }
                // if the criteria is a path include, we want to filter the list based on the path, this
                // will help with the search, and prevent us from having to parse the criteria multiple times.
                else if (wc.Length == 1 && wc.Equals(Seps.cIncludesPos.ToString()) && pathList.Count() > 0)
                {
                    searchCriteria.SearchCriteria[i] = "";
                    filters.Clear();
                    
                    // not looking for more if the first parts don't exist.
                    if (!hasData && i > 0)
                        continue;

                    // if we have content, we now need to filter down for each criteria.
                    foreach (var p in pathList)
                    {
                        // if data doesn't exists at the start, then we want to only add
                        // extensions, not filter out after the first extension is found.
                        if (hasData)
                        {
                            if (p.Search.ToLower() == Seps.sNOEXT.ToLower())
                                filters.AddRange(findingsDic.Where(w => !w.Value).Select(s => (s.Key, s.Value)));
                            else
                                filters.AddRange(findingsDic.Where(w => w.Key.ToLower().Contains(p.Search.ToLower())).Select(s => (s.Key, s.Value)));
                        }
                        else
                        {
                            if (p.Search.ToLower() == Seps.sNOEXT.ToLower())
                                foreach (var item in fullFileList.Where(w => !w.Value))
                                    findingsDic.TryAdd(item.Key, item.Value);
                            else
                                foreach (var item in fullFileList.Where(w => w.Key.ToLower().Contains(p.Search.ToLower())))
                                    findingsDic.TryAdd(item.Key, item.Value);
                        }
                    }

                    // if we had content before, we need to clear it out, and add the new filtered content
                    if (prevDicCount != 0)
                        findingsDic.Clear();

                    // if we have filters, we need to add them to the findings, this will be the new list of
                    // findings for the next criteria.
                    if (filters.Count() > 0)
                    {
                        foreach (var item in filters.ToList())
                            findingsDic.TryAdd(item.Path, item.HasExt);
                    }
                }
                // if the criteria is an extension, we want to filter the list based on the extension, this
                // will help with the search, and prevent us from having to parse the criteria multiple times.
                else if (wc.Length == 1 && wc.Equals(Seps.cExtPos.ToString()) && extList.Count() > 0)
                {
                    searchCriteria.SearchCriteria[i] = "";
                    filters.Clear();

                    // not looking for more if the first parts don't exist.
                    if (!hasData && i > 0)
                        continue;

                    // if we have content, we now need to filter down for each extension.
                    foreach (var e in extList)
                    {
                        // if data doesn't exists at the start, then we want to only add
                        // extensions, not filter out after the first extension is found.
                        if (hasData)
                        {
                            if(e.Search==Seps.cNOEXT.ToString())
                                filters.AddRange(findingsDic.Where(w => !w.Value).Select(s => (s.Key, s.Value)));
                            else
                                filters.AddRange(findingsDic.Where(w => w.Key.ToLower().EndsWith(e.Search.ToLower())).Select(s => (s.Key, s.Value)));
                        }
                        else
                        {
                            if (e.Search == Seps.cNOEXT.ToString())
                                foreach (var item in fullFileList.Where(w => !w.Value))
                                    findingsDic.TryAdd(item.Key, item.Value);
                            else
                                foreach (var item in fullFileList.Where(w => w.Key.ToLower().EndsWith(e.Search.ToLower())))
                                    findingsDic.TryAdd(item.Key, item.Value);
                        }
                    }

                    // if we had content before, we need to clear it out, and add the new filtered content
                    if (prevDicCount != 0)
                        findingsDic.Clear();

                    if (filters.Count() > 0)
                    {
                        foreach (var item in filters.ToList())
                            findingsDic.TryAdd(item.Path, item.HasExt);
                    }
                }
                // if the criteria is a path exclude, we want to filter the list based on the path, this
                // will help with the search, and prevent us from having to parse the criteria multiple times.
                else if (wc.Length == 1 && wc.Equals(Seps.cFilterPos.ToString()) && filterList.Count() > 0)
                {
                    searchCriteria.SearchCriteria[i] = "";
                    filters.Clear();

                    // not looking for more if the first parts don't exist.
                    if (!hasData && i > 0)
                        continue;

                    foreach (var f in filterList)
                    {
                        // if we have content, we now need to filter down for each exclusion.
                        // If we don't have content, we have nothing to exclude, so we skip.
                        if (prevDicCount > 0)
                        {
                            if (f.Search.ToLower() == Seps.sNOEXT.ToLower())
                                filters.AddRange(findingsDic.Where(w => w.Value).Select(s => (s.Key, s.Value)));
                            else
                                filters.AddRange(findingsDic.Where(w => !w.Key.ToLower().Contains(f.Search.ToLower())).Select(s => (s.Key, s.Value)));

                            if (filters.Count() > 0)
                            {
                                findingsDic.Clear();
                                foreach (var item in filters.ToList())
                                    findingsDic.TryAdd(item.Path, item.HasExt);
                                filters.Clear();
                            }
                        }
                        else
                        {
                            if (f.Search.ToLower() == Seps.sNOEXT.ToLower())
                            {
                                foreach (var item in fullFileList.Where(w => w.Value).Select(s => (s.Key, s.Value)).ToList())
                                    findingsDic.TryAdd(item.Key, item.Value);
                            }
                            else
                            {
                                foreach (var item in fullFileList.Where(w => !w.Key.ToLower().Contains(f.Search.ToLower())).ToList())
                                    findingsDic.TryAdd(item.Key, item.Value);
                            }
                        }
                    }
                }
                // if anything is earched, this is the general search, we want to filter the list
                // based on the search, this will help with the search, and prevent us from having
                // to parse the criteria multiple times.
                else
                {
                    filters.Clear();

                    // if we have content, we now need to filter down for each criteria.
                    if (prevDicCount > 0)
                    {
                        filters.AddRange(findingsDic.Where(w => w.Key.ToLower().Contains(wc.ToLower())).Select(s => (s.Key, s.Value)));
                        findingsDic.Clear();
                        if (filters.Count() > 0)
                        {
                            foreach (var item in filters.ToList())
                                findingsDic.TryAdd(item.Path, item.HasExt);
                        }
                    }
                    else
                    {
                        foreach (var item in fullFileList.Where(w => w.Key.ToLower().Contains(wc.ToLower())).ToList())
                            findingsDic.TryAdd(item.Key, item.Value);
                    }
                }

                // at any time, we haven't found anything,
                // then we haven't met the criteria
                if (findingsDic.Count.Equals(0))
                    break;
            }

            var verifiedFiles = 0;
            var fileList = new List<string>();
            if (findingsDic != null && findingsDic.Count > 0)
            {
                retVal = true;
                foreach (var file in findingsDic.Keys)
                {
                    if (_cancelTokenSource.IsCancellationRequested || GlobalSettings.HasShutdown)
                        break;

                    if (VerifyCriteria(drives, file, searchCriteria))
                    {
                        if (verifiedFiles < MAX_FIND_RESPONSE)
                            fileList.Add(file);
                        verifiedFiles++;
                    }
                }

                // Bulk send of all findings by split of '\n', instant...  Balances Windows with Linux strings.
                var arrData = string.Join("\n", fileList);
                // send event of all related files paths
                SearchMessage.SendMsg(SearchMessageType.SearchResults, arrData);
                // send status event message
                SearchMessage.SendMsg(SearchMessageType.SearchStatus, $"Filtered: {fileList.Count().FormatByComma()}, Total Found: {verifiedFiles.FormatByComma()}");
            }

            return retVal;
        }
        private bool VerifyCriteria(string[] drives, string file, BuildSearchCmd searchCriteria)
        {
            var fileDrive = file.Substring(0, 2).ToLower();
            var fileName = Path.GetFileName(file).ToLower();
            var folderName = Path.GetDirectoryName(file).ToLower();
            var fileExt = Path.GetExtension(file).ToLower();
            var findCount = 0;
            var loc = 0;
            var prevLoc = 0;

            if (drives.Where(w => w.Substring(0, 2).Equals(file.Substring(0, 2), StringComparison.CurrentCultureIgnoreCase)).Count().Equals(0))
                return false;

            if (Criteria.SearchDirectory)
            {
                foreach (var sArr in searchCriteria.SearchCriteria)
                {
                    if (string.IsNullOrWhiteSpace(sArr))
                        continue;
                    if (sArr.Equals(Seps.cContentsPos.ToString()))
                        continue;

                    // met criteria, get out.
                    if (findCount >= searchCriteria.SearchCriteria.Length)
                        break;
                    var clnUp = sArr.EndsWith("\\") ? sArr.Substring(0, sArr.Length - 1) : sArr;
                    loc = folderName.IndexOf(sArr, prevLoc, StringComparison.CurrentCultureIgnoreCase);
                    if (loc.Equals(-1) && folderName.EndsWith(clnUp))
                        loc = folderName.IndexOf(clnUp, prevLoc, StringComparison.CurrentCultureIgnoreCase);

                    if (loc < prevLoc)
                    {
                        if (Criteria.SearchFilename)
                            break;

                        return false;
                    }
                    else
                    {
                        prevLoc = loc + 1;
                        findCount++;
                    }
                }
            }
            if (Criteria.SearchFilename && findCount < searchCriteria.SearchCriteria.Length)
            {
                prevLoc = 0;
                foreach (var sArr in searchCriteria.SearchCriteria.Skip(findCount))
                {
                    if (string.IsNullOrWhiteSpace(sArr))
                        continue;
                    if (sArr.Equals(Seps.cContentsPos.ToString()))
                        continue;

                    // met criteria, get out.
                    if (findCount >= searchCriteria.SearchCriteria.Length)
                        break;

                    loc = fileName.IndexOf(sArr, prevLoc, StringComparison.CurrentCultureIgnoreCase);
                    if (loc < prevLoc)
                        return false;
                    else
                    {
                        prevLoc = loc + 1;
                        findCount++;
                    }
                }
            }

            return true;
        }
        private void SearchMessage_EventMessaging(object sender, SearchEventArgs e) => EventMessaging?.Invoke(this, e);
        #endregion

        #region Private Event FileWatcher
        public void SetupWatcher(DriveInfo[] drives)
        {
            StopWatchers();
            foreach (var drive in drives.Select(s => (s.Name.EndsWith("\\") ? s.Name : $"{s.Name}\\")))
            {
                var watcher = new FileSystemWatcher(drive)
                {
                    NotifyFilter = NotifyFilters.DirectoryName |
                                   NotifyFilters.FileName |
                                   NotifyFilters.Attributes
                };

                watcher.Created += OnCreated;
                watcher.Deleted += OnDeleted;
                watcher.Renamed += OnRenamed;

                watcher.Filter = "*.*";
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;

                _watchers.Add(watcher);
            }
        }
        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Created)
                return;

            _systemUpdates.Enqueue((WatcherChangeTypes.Created, e.FullPath, string.Empty));
            ProcessSystemUpdatesQueue();
        }
        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Deleted)
                return;

            _systemUpdates.Enqueue((WatcherChangeTypes.Deleted, e.FullPath, string.Empty));
            ProcessSystemUpdatesQueue();
        }
        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Renamed)
                return;

            _systemUpdates.Enqueue((WatcherChangeTypes.Renamed, e.FullPath, e.OldFullPath));
            ProcessSystemUpdatesQueue();
        }
        private async void ProcessSystemUpdatesQueue()
        {
            if (!_queProcessing.TrySetTrue())
                return;

            try
            {
                await Task.Delay(1).ContinueWith(t =>
                {
                    List<Task> taskList = new List<Task>();

                    while (_systemUpdates.TryDequeue(out (WatcherChangeTypes changeType, string newPath, string oldPath) sys))
                    {
                        if (!GlobalSettings.ScanSettings.AllowDir(sys.newPath))
                            return;

                        // we found if a folder is being added, files are missed, so lets sleep a sec and see if that helps.
                        switch (sys.changeType)
                        {
                            case WatcherChangeTypes.Deleted:
                                taskList.AddRange(ProcessDelete(sys.newPath));
                                break;
                            case WatcherChangeTypes.Renamed:
                                taskList.AddRange(ProcessRename(sys.newPath, sys.oldPath));
                                break;
                            case WatcherChangeTypes.Created:
                            default:
                                taskList.AddRange(ProcessCreated(sys.newPath));
                                break;
                        }
                    }

                    Task.WaitAll(taskList.ToArray());
                    SearchMessage.SendMsg(SearchMessageType.FileScanStatus, $"Cached: [{SystemScan.ScannedFolders.FormatByComma()}] Folders, [{SystemScan.ScannedFiles.FormatByComma()}] Files.");
                });
            }
            finally
            {
                _queProcessing.SetFalse();
                if (_systemUpdates.Count > 0)
                    ProcessSystemUpdatesQueue();
            }
        }
        private List<Task> ProcessCreated(string newFile)
        {
            List<Task> taskList = new List<Task>();

            if (File.Exists(newFile))
            {
                taskList.Add(Task.Run(() => { Scanner.AddFile(newFile); return Task.CompletedTask; }));
                SearchMessage.SendMsg(SearchMessageType.Info, $"Added: [{newFile}] to cache.");
            }
            else if (Directory.Exists(newFile))
            {
                taskList.AddRange(Scanner.ScanSubFolders(new string[] { newFile }, false));
                if (taskList.Count > 0)
                    SearchMessage.SendMsg(SearchMessageType.Info, $"Adding: folder [{newFile}] to cache.");
            }

            return taskList;
        }
        private List<Task> ProcessDelete(string delFile)
        {
            List<Task> taskList = new List<Task>();

            var dirWithSlash = delFile.EndsWith("\\") ? delFile : $"{delFile}\\";
            var isDir = false;
            var isFile = false;
            if (Scanner.IsDirectory(dirWithSlash))
                isDir = true;
            else
                isFile = true;

            if (isDir)
                taskList.AddRange(Scanner.RemoveRootFolder(dirWithSlash));
            else if (isFile)
                taskList.Add(Task.Run(() => { Scanner.RemoveFile(delFile); return Task.CompletedTask; }));

            return taskList;
        }
        private List<Task> ProcessRename(string newFile, string oldFile)
        {
            var fileGoodToGo = false;
            var removedFolders = 0;
            var addFolders = 0;

            var isDir = Scanner.IsDirectory(oldFile);
            List<Task> taskList = new List<Task>();

            if (isDir)
                taskList.AddRange(Scanner.RemoveRootFolder(oldFile, false));
            else
                taskList.Add(Task.Run(() => { Scanner.RemoveFile(oldFile, false); return Task.CompletedTask; }));

            removedFolders = taskList.Count;
            fileGoodToGo = removedFolders > 0;

            if (isDir)
                taskList.AddRange(Scanner.ScanSubFolders(new string[] { newFile }, false));
            else
                taskList.Add(Task.Run(() => { Scanner.AddFile(newFile); return Task.CompletedTask; }));

            addFolders = taskList.Count - removedFolders;
            fileGoodToGo = fileGoodToGo || addFolders > 0;

            if (fileGoodToGo)
            {
                var info = $"[{oldFile}]  ->  [{newFile}]";
                var vInfo = info.SplitByStr("->");
                if (newFile.Length > 100 && vInfo.Length == 2)
                {
                    SearchMessage.SendMsg(SearchMessageType.Info, $"Renamed: {vInfo[0].Trim()}");
                    SearchMessage.SendMsg(SearchMessageType.Info, $"To   ->: {vInfo[1].Trim()}");
                }
                else
                    SearchMessage.SendMsg(SearchMessageType.Info, $"Renamed: {info}");
            }

            return taskList;
        }
        #endregion  Event FileWatcher

        #region Public Properties
        /// <summary>
        /// Scan Property Settings
        /// </summary>
        public ScanProperties Criteria => GlobalSettings.ScanSettings;
        /// <summary>
        /// All findings, count, and snippets.
        /// </summary>
        public static ConcurrentDictionary<string, (string, string[])> FileContentFinds { get; } = new ConcurrentDictionary<string, (string, string[])>();
        /// <summary>
        /// If true, during token label 'contents', e.g. [ contents: microsoft | google | gemini | chatgpt ], all binary e.g. [ ext: dll | exe | etc ] files will also be searched through.<br/>
        /// Default: false
        /// </summary>
        public bool AllowBinaryContentSearch { get; set; } = false;
        /// <summary>
        /// Event Response for all status
        /// </summary>
        public event SearchEventHandler EventMessaging;
        /// <summary>
        /// Readonly Current Search Status
        /// </summary>
        public LookupStatus CurrentStatus => GlobalSettings.CurrentStatus;
        /// <summary>
        /// true = of scan has been fully processed.<br/>
        /// false if stopped or never started.
        /// </summary>
        public bool FullScanCompleted => GlobalSettings.FullScanCompleted;
        #endregion

        #region Public Methods
        /// <summary>
        /// Scans all drives based on allow filters set in options.  Event messages will be sent to subscribers.
        /// </summary>
        /// <param name="reScan">true: Will clear the cache and start over.  false: will leave, but rescan for any new files to add to the cache.</param>
        public Task ScanToCache(bool isRescan = false) => Scan(GlobalSettings.DriveList, true, isRescan);
        public Task ScanToCache(DriveInfo drive, bool isRescan = false) => Scan(new string[] { drive.Name }, true, isRescan);
        public Task ScanToCache(DriveInfo[] drives, bool isRescan = false) => Scan(drives.Select(w => w.Name).ToArray(), true, isRescan);
        public Task CheckCache(string[] fullPath, bool dirCheck = false) => Scanner.CheckCache(fullPath, dirCheck);

        //public Task AddDrive(DriveInfo drive) => Task.Run(() => GlobalSettings.AddRemove($"{drive.Name}{(drive.Name.EndsWith("\\") ? "" : "\\")}", true));
        //public Task RemoveDrive(DriveInfo drive) => Task.Run(() => GlobalSettings.AddRemove($"{drive.Name}{(drive.Name.EndsWith("\\") ? "" : "\\")}", false));
        public Task AddDrive(DriveInfo drive) => ScanToCache(drive, false);
        public Task RemoveDrive(DriveInfo drive) => Task.Run(() => GlobalSettings.AddRemove($"{drive.Name}{(drive.Name.EndsWith("\\") ? "" : "\\")}", false));
        public Task<bool> AddScanExclusion(string pathOrFileContains) => Task.Run(() => {
                GlobalSettings.AddRemove($"{pathOrFileContains}", false); 
                return GlobalSettings.CustomExclusions.TryAdd(pathOrFileContains, true); 
            });
        public Task<string[]> GetScanExclusions() => Task.Run(() => { return GlobalSettings.CustomExclusions.Select(s=>s.Key).ToArray(); });
        public Task<bool> RemoveScanExclusion(string pathOrFileContains) => Task.Run(() => { return GlobalSettings.CustomExclusions.TryRemove(pathOrFileContains, out _); });

        /// <summary>
        /// Starts search in cache if exists.
        /// </summary>
        /// <param name="drive">1 Drive to search</param>
        /// <param name="searchCriteria">Query with search extensions accepted</param>
        /// <returns>Task for wait results, all results are sent as Events</returns>
        public Task Search(DriveInfo drive, string searchCriteria) => Search(new string[] { drive.Name }, searchCriteria, false);
        /// <summary>
        /// Starts search in cache if exists.
        /// </summary>
        /// <param name="drives">All drives to search through</param>
        /// <param name="searchCriteria">Query with search extensions accepted</param>
        /// <returns>Task for wait results, all results are sent as Events</returns>
        public Task Search(DriveInfo[] drives, string searchCriteria) => Search(drives.Select(w => w.Name).ToArray(), searchCriteria, false);
        public void ResetCache() => Scanner.ResetCache();
        public void StopScan()
        {
            StopWatchers();
            GlobalSettings.Shutdown();
            _cancelTokenSource.Cancel();
        }
        #endregion
    }
}

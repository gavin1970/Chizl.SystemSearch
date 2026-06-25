using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chizl.SystemSearch
{
    public class IOFinder : IDisposable
    {
        private const int _maxFindingsCount = 10000;
        private static List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();

        // since there will be a lot of threading going on, these status need to be seen by all threads.
        private static bool disposedValue;

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
        public ScanProperties Criteria => GlobalSettings.ScanSettings;
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

            return retVal;
        }
        /// <summary>
        /// IsBinary(): Has BOM detection for UTF encoding UTF-8 BOM, UTF-16 LE/BE and future UTF-32 LE/BE ASCII<br/>
        /// </summary>
        /// <param name="fullPath">Path of file</param>
        /// <param name="errorMsg">if method returns false, errorMsg will have reason.</param>
        /// <param name="bytesToCheck">How many bytes to check within the header.  If the file is smaller than bytesToCheck, it will only pull what.</param>
        /// <returns>true - if binary found.  false if </returns>
        const int BYTE_SIZE_READS = 4096;
        const int MAX_BYTE_SIZE_CHECK = 1024 * 1024 * 5;  // 5 MB = 5,242,880 bytes.

        public static bool IsBinary(string fullPath, out string errorMsg, int bytesToCheck = 1024)
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
        
        private static long _fileContentCount = 0;

        private Task<bool> FindContent(string filePath, string searchTerm)
        {
            return Task.Run(() =>
            {
                try
                {
                    var fi = new FileInfo(filePath);
                    // if the file is larger than 1 GB, we will skip it, as it is likely a binary file, and we don't want to waste the time trying to read it.
                    if (fi.Exists && !IsBinary(filePath, out _) && fi.Length < (1024 * 1024 * 1024))
                    {
                        try
                        {
                            // limit the number of files being read at the same time to prevent
                            // out of memory issues, if we have more than 100 files being read,
                            // we will wait for some of them to finish before starting to read more.
                            while (Volatile.Read(ref _fileContentCount) > 100 && !_cancelTokenSource.IsCancellationRequested && !GlobalSettings.HasShutdown)
                                Task.Delay(100).Wait();


                            if (_cancelTokenSource.IsCancellationRequested || GlobalSettings.HasShutdown)
                                return false;

                            Interlocked.Increment(ref _fileContentCount);
                            List<string> snips = new List<string>();

                            var content = File.ReadAllText(fi.FullName);
                            var cLen = content.Length;
                            var sLen = searchTerm.Length;
                            //var sHLen = sLen / 2;
                            var start = 0;
                            var snipE = 50;

                            var find = content.IndexOf(searchTerm, StringComparison.CurrentCultureIgnoreCase);
                            while (find > -1 && !_cancelTokenSource.IsCancellationRequested && !GlobalSettings.HasShutdown)
                            {
                                var snipS = find - sLen;

                                if (snipS < 0)
                                    snipS = 0;

                                if (snipS + snipE > cLen)
                                    snipE = snipS - (cLen - snipS);

                                var snippet = content.Substring(snipS, snipE);
                                snips.Add($"{snippet.Replace("\r", ".").Replace("\n", "_").Trim()}");

                                start = find + sLen;
                                if (start > cLen)
                                    break;

                                find = content.IndexOf(searchTerm, start, StringComparison.CurrentCultureIgnoreCase);
                            }

                            if (snips.Count > 0)
                            {
                                FileContentFinds.TryAdd(filePath, ($"{{{snips.Count} found}}", snips.ToArray()));
                                return true;
                            }
                        }
                        finally 
                        {
                            Interlocked.Decrement(ref _fileContentCount);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // if we can't read the file, we just skip it, but log the error.
                    SearchMessage.SendMsg(ex, $"Error reading file: {filePath}");
                }
                return false;
            });
        }

        // static, cancel all threads
        private static CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();

        public static ConcurrentDictionary<string, (string, string[])> FileContentFinds { get; } = new ConcurrentDictionary<string, (string, string[])>();
        //[extensions:noext] + [excludes:\Atlassian\|\3rdparty\|\themes\|\dp_web] + [contents:gavin]

        private bool DeepDive(string[] drives, BuildSearchCmd searchCriteria)
        {
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
                    var keys = findingsDic.Keys.ToList();
                    findingsDic.Clear();
                    List<Task> searchTask = new List<Task>();

                    SearchMessage.SendMsg(SearchMessageType.FileScanStatus, $"Found: [{keys.Count().FormatByComma()}] files to scan for content.");

                    // c:\code + [extensions:cs] + [contents:action]
                    foreach (var file in keys)
                    {
                        if (_cancelTokenSource.IsCancellationRequested || GlobalSettings.HasShutdown)
                            break;

                        // we need to verify the criteria again, as some of the criteria is not able to be pre-filtered
                        // in the cache, and we don't want to waste the time trying to read the file if it doesn't
                        // meet the criteria.
                        if (VerifyCriteria(drives, file, searchCriteria))
                        {
                            //search for each content criteria, if any of them are met, then we
                            //add it to the findings, if not, we don't add it.
                            foreach (var c in contentList)
                            {
                                if (_cancelTokenSource.IsCancellationRequested || GlobalSettings.HasShutdown)
                                    break;
                                // create task for each content search, this will help with performance, as we
                                // can search for multiple content criteria at the same time, and it will also
                                // help with large files, as we can read them in parallel.
                                // FindContent verifies the file is not binary and less than 1 GB before
                                // reading, so we don't have to worry about that here.
                                searchTask.Add(FindContent(file, c.Search).ContinueWith(t =>
                                {
                                    if (t.Result)
                                    {
                                        findingsDic.TryAdd(file, string.IsNullOrWhiteSpace(Path.GetExtension(file)));
                                        SearchMessage.SendMsg(SearchMessageType.FileScanStatus, $"Found: [{findingsDic.Count().FormatByComma()}] files matching content.");
                                    }
                                }));
                            }
                        }
                    }

                    try
                    {
                        Task.WaitAll(searchTask.ToArray(), _cancelTokenSource.Token);
                        SearchMessage.SendMsg(SearchMessageType.FileScanStatus, $"Found: [{findingsDic.Count().FormatByComma()}] files matching content.");
                    }
                    catch (OperationCanceledException) 
                    {
                        SearchMessage.SendMsg(SearchMessageType.FileScanStatus, $"User canceled: [{findingsDic.Count().FormatByComma()}] files were matched before cancel occured.");
                    }
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
                        filters.AddRange(findingsDic.Where(w => w.Key.ToLower().Contains(wc.ToLower())).Select(s => (s.Key, s.Value)));
                    else
                    {
                        foreach (var item in fullFileList.Where(w => w.Key.ToLower().Contains(wc.ToLower())).ToList())
                            findingsDic.TryAdd(item.Key, item.Value);
                    }

                    if (filters.Count() > 0)
                    {
                        foreach (var item in filters.ToList())
                            findingsDic.TryAdd(item.Path, item.HasExt);
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
                        if (verifiedFiles < _maxFindingsCount)
                            fileList.Add(file);
                        verifiedFiles++;
                    }
                }

                // Bulk send of all findings by split of '\n', instant...  Balances Windows with Linux strings.
                var arrData = string.Join("\n", fileList);
                SearchMessage.SendMsg(SearchMessageType.SearchResults, arrData);
                // send found message
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
        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Created)
                return;

            if (!GlobalSettings.ScanSettings.AllowDir(e.FullPath))
                return;

            List<Task> createList = new List<Task>();
            // we found if a folder is being added, files are missed, so lets sleep a sec and see if that helps.
            Tools.Sleep(100, SleepType.Milliseconds);

            if (File.Exists(e.FullPath))
            {
                createList.Add(Task.Run(() => { Scanner.AddFile(e.FullPath); return Task.CompletedTask; }));
                SearchMessage.SendMsg(SearchMessageType.Info, $"Added: [{e.FullPath}] to cache.");
            }
            else if (Directory.Exists(e.FullPath))
            {
                createList.AddRange(Scanner.ScanSubFolders(new string[] { e.FullPath }, false));
                if (createList.Count > 0)
                    SearchMessage.SendMsg(SearchMessageType.Info, $"Adding: [{createList.Count}] files to cache.");
            }

            Task.WaitAll(createList.ToArray());

            SearchMessage.SendMsg(SearchMessageType.FileScanStatus, $"Cached: [{SystemScan.ScannedFolders.FormatByComma()}] Folders, [{SystemScan.ScannedFiles.FormatByComma()}] Files.");
        }
        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Deleted)
                return;

            if (!GlobalSettings.ScanSettings.AllowDir(e.FullPath))
                return;

            List<Task> removeList = new List<Task>();
            // we found if a folder is being added, files are missed, so lets sleep a sec and see if that helps.
            Tools.Sleep(100, SleepType.Milliseconds);

            var dirWithSlash = e.FullPath.EndsWith("\\") ? e.FullPath : $"{e.FullPath}\\";
            var isDir = false;
            var isFile = false;
            if (Scanner.IsDirectory(dirWithSlash))
                isDir = true;
            else
                isFile = true;

            if (isDir)
                removeList.AddRange(Scanner.RemoveRootFolder(dirWithSlash));
            else if (isFile)
                removeList.Add(Task.Run(() => { Scanner.RemoveFile(e.FullPath); return Task.CompletedTask; }));

            Task.WaitAll(removeList.ToArray());

            SearchMessage.SendMsg(SearchMessageType.FileScanStatus, $"Cached: [{SystemScan.ScannedFolders.FormatByComma()}] Folders, [{SystemScan.ScannedFiles.FormatByComma()}] Files.");
        }
        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Renamed)
                return;

            if (!GlobalSettings.ScanSettings.AllowDir(e.OldFullPath))
                return;

            var fileGoodToGo = false;
            var removedFolders = 0;
            var addFolders = 0;

            Tools.Sleep(100, SleepType.Milliseconds);
            var isDir = Scanner.IsDirectory(e.OldFullPath);
            List<Task> renameList = new List<Task>();

            if (isDir)
                renameList.AddRange(Scanner.RemoveRootFolder(e.OldFullPath, false));
            else
                renameList.Add(Task.Run(() => { Scanner.RemoveFile(e.OldFullPath, false); return Task.CompletedTask; }));

            removedFolders = renameList.Count;
            fileGoodToGo = removedFolders > 0;

            if (isDir)
                renameList.AddRange(Scanner.ScanSubFolders(new string[] { e.FullPath }, false));
            else
                renameList.Add(Task.Run(() => { Scanner.AddFile(e.FullPath); return Task.CompletedTask; }));

            addFolders = renameList.Count - removedFolders;
            fileGoodToGo = fileGoodToGo || addFolders > 0;

            if (fileGoodToGo)
            {
                var info = $"[{e.OldFullPath}]  ->  [{e.FullPath}]";
                var vInfo = info.SplitByStr("->");
                if (e.FullPath.Length > 100 && vInfo.Length == 2)
                {
                    SearchMessage.SendMsg(SearchMessageType.Info, $"Renamed: {vInfo[0].Trim()}");
                    SearchMessage.SendMsg(SearchMessageType.Info, $"To   ->: {vInfo[1].Trim()}");
                }
                else
                    SearchMessage.SendMsg(SearchMessageType.Info, $"Renamed: {info}");

                Tools.Sleep(1);
            }

            Task.WaitAll(renameList.ToArray());
            SearchMessage.SendMsg(SearchMessageType.FileScanStatus, $"Cached: [{SystemScan.ScannedFolders.FormatByComma()}] Folders, [{SystemScan.ScannedFiles.FormatByComma()}] Files");
        }
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
        #endregion

        #region Public Properties
        public event SearchEventHandler EventMessaging;
        public LookupStatus CurrentStatus => GlobalSettings.CurrentStatus;
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

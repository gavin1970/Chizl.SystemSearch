using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        internal SystemScan Scanner => new SystemScan();
        public ScanProperties Criteria => GlobalSettings.ScanSettings;
        private Task Scan(string[] drives, bool sendMsg, bool isRescan) => Scanner.ScanDrives(drives, sendMsg, isRescan);
        private Task Search(string[] drives, string searchCriteria, bool isRescan)
        {
            var searchTask = Task.Run(async () =>
            {
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
        private bool SearchCache(string[] drives, string searchCriteria, bool sendMsg = true)
        {
            var retVal = false;

            if (string.IsNullOrWhiteSpace(searchCriteria))
            {
                SearchMessage.SendMsg(SearchMessageType.ScanComplete, $"Cached: [{SystemScan.ScannedFolders.FormatByComma()}] Folders, [{SystemScan.ScannedFiles.FormatByComma()}] Files.");
                return retVal;
            }

             var buildSearchCriteria = new BuildSearchCmd(ref searchCriteria);

            if (sendMsg)
            {
                SearchMessage.SendMsg(SearchMessageType.UpdateInProgress, $"Search cache for '{searchCriteria}'.");
                SearchMessage.SendMsg(SearchMessageType.SearchQueryUsed, searchCriteria);
            }

            retVal = DeepDive(drives, buildSearchCriteria);

            // send total count message, to ensure accuratness
            SearchMessage.SendMsg(SearchMessageType.ScanComplete, $"Cached: [{SystemScan.ScannedFolders.FormatByComma()}] Folders, [{SystemScan.ScannedFiles.FormatByComma()}] Files.");

            return retVal;
        }
        private bool DeepDive(string[] drives, BuildSearchCmd searchCriteria)
        {
            var retVal = false;
            var fullFileList = Scanner.GetFileList;
            var fullFileListLen = fullFileList.Length;
            var findings = new List<string>();
            var filters = new List<string>();

            var pathList = searchCriteria.Commands.Where(w => w.CommandType == CommandType.path).ToList();
            var extList = searchCriteria.Commands.Where(w => w.CommandType == CommandType.ext).ToList();
            var filterList = searchCriteria.Commands.Where(w => w.CommandType == CommandType.filter).ToList();
            filterList.AddRange(searchCriteria.Commands.Where(w => w.CommandType == CommandType.exclude).ToList());

            for (int i = 0; i < searchCriteria.SearchCriteria.Length; i++)
            {
                if (GlobalSettings.HasShutdown)
                    break;

                var wc = searchCriteria.SearchCriteria[i];

                if (wc.Length == 1 && wc.Equals(Seps.cPathPos.ToString()) && pathList.Count() > 0)
                {
                    searchCriteria.SearchCriteria[i] = "";
                    filters.Clear();

                    foreach (var p in pathList)
                    {
                        if (findings.Count > 0)
                            filters.AddRange(findings.Where(w => w.ToLower().Contains(p.Search.ToLower())).ToList());
                        else
                            findings.AddRange(fullFileList.Where(w => w.ToLower().Contains(p.Search.ToLower())).ToList());
                    }

                    findings.Clear();
                    if (filters.Count() > 0)
                        findings.AddRange(filters);
                }
                else if (wc.Length == 1 && wc.Equals(Seps.cExtPos.ToString()) && extList.Count() > 0)
                {
                    searchCriteria.SearchCriteria[i] = "";
                    filters.Clear();

                    foreach (var e in extList)
                    {
                        if (findings.Count > 0)
                            filters.AddRange(findings.Where(w => w.ToLower().EndsWith(e.Search.ToLower())).ToList());
                        else
                            findings.AddRange(fullFileList.Where(w => w.ToLower().EndsWith(e.Search.ToLower())).ToList());
                    }

                    findings.Clear();
                    if (filters.Count() > 0)
                        findings.AddRange(filters);
                }
                else if (wc.Length == 1 && wc.Equals(Seps.cFilterPos.ToString()) && filterList.Count() > 0)
                {
                    searchCriteria.SearchCriteria[i] = "";
                    filters.Clear();

                    foreach (var f in filterList)
                    {
                        if (findings.Count > 0)
                        {
                            filters.AddRange(findings.Where(w => !w.ToLower().Contains(f.Search.ToLower())).ToList());
                            if (filters.Count() > 0)
                            {
                                findings.Clear();
                                findings.AddRange(filters);
                                filters.Clear();
                            }
                        }
                        else
                            findings.AddRange(fullFileList.Where(w => !w.ToLower().Contains(f.Search.ToLower())).ToList());
                    }
                }
                else
                {
                    filters.Clear();

                    // if we have content, we now need to filter down for each criteria.
                    if (findings.Count > 0)
                        filters.AddRange(findings.Where(w => w.ToLower().Contains(wc.ToLower())).ToList());
                    else
                        findings.AddRange(fullFileList.Where(w => w.ToLower().Contains(wc.ToLower())).ToList());

                    if (filters.Count() > 0)
                    {
                        findings.Clear();
                        findings.AddRange(filters);
                    }
                }

                // at any time, we haven't found anything,
                // then we haven't met the criteria
                if (findings.Count.Equals(0))
                    break;
            }

            var verifiedFiles = 0;
            var fileList = new List<string>();
            if (findings != null && findings.Count > 0)
            {
                retVal = true;
                foreach (var file in findings)
                {
                    if (GlobalSettings.HasShutdown)
                        break;

                    if (VerifyCriteria(drives, file, searchCriteria))
                    {
                        if (verifiedFiles < _maxFindingsCount)
                            fileList.Add(file);
                        verifiedFiles++;
                    }
                }
                // I found sending a message for each file found became really slow, so,
                // SearchMessage.SendMsg(SearchMessageType.SearchResults, file);

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
        public Task<bool> AddScanExclusion(string pathOrFileContains) => Task.Run(() => { return GlobalSettings.CustomExclusions.TryAdd(pathOrFileContains, true); });
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
        }
        #endregion
    }
}

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Chizl.SystemSearch
{
    public class IOFinder : IDisposable
    {
        const int _maxFindingsCount = 1000;
        private static List<FileSystemWatcher> _watcher = new List<FileSystemWatcher>();

        //since there will be a lot of threading going on, these status need to be seen by all threads.
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
            SetupWatcher();
        }

        #region Private
        internal SystemScan Scanner => new SystemScan();
        public ScanProperties Criteria => GlobalSettings.ScanSettings;
        //public ScanPaths ScanPaths
        //{
        //    get
        //    {
        //        //try
        //        //{
        //            //var md5 = Tools.CreateMD5("Gavin");
        //            //md5 = Tools.CreateMD5("if (!GlobalSettings.IgnoreChange && !GlobalSettings.RefreshFolder.IsEmpty)");
        //            //global static settings.
        //            return GlobalSettings.ScanPaths;
        //        //}
        //        //finally
        //        //{
        //        //    if (!GlobalSettings.IgnoreChange && !GlobalSettings.RefreshFolder.IsEmpty)
        //        //    {
        //        //        //options change, then lets check to see
        //        //        //if we need to add or removed from cache.
        //        //        Task.Run(async () =>
        //        //        {
        //        //            CheckRefresh().Wait();
        //        //            await Tools.Delay(1, SleepType.Milliseconds);
        //        //            return;
        //        //        });
        //        //    }
        //        //}
        //    }
        //}

        private Task Scan(string[] drives, bool sendMsg = true) => Scanner.ScanDrives(drives, sendMsg);
        private Task Search(string[] drives, string searchCriteria)
        {
            var searchTask = Task.Run(async () =>
            {
                if (!GlobalSettings.FullScanCompleted)
                {
                    SearchMessage.SendMsg(SearchMessageType.UpdateInprogress, $"Full scan hasn't been completed. Please wait...");
                    await Scan(drives, false);
                }
            })
            .ContinueWith(previousTask =>
            {
                //synchronous
                SearchCache(drives, searchCriteria);
            });

            return searchTask;
        }
        private bool SearchCache(string[] drives, string searchCriteria, bool sendMsg = true)
        {
            var retVal = false;

            if(string.IsNullOrWhiteSpace(searchCriteria))
            {
                SearchMessage.SendMsg(SearchMessageType.ScanComplete, $"Cached: [{SystemScan.ScannedFolders.FormatByComma()}] Folders, [{SystemScan.ScannedFiles.FormatByComma()}] Files.");
                return retVal;
            }

            if (sendMsg)
                SearchMessage.SendMsg(SearchMessageType.UpdateInprogress, $"Search cache for '{searchCriteria}'.");

            var wcSearch = searchCriteria.Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> findings = new List<string>();

            for (int i = 0; i < wcSearch.Length; i++)
            {
                if (GlobalSettings.HasShutdown)
                    break;

                var wc = wcSearch[i];

                //if we have content, we now need to filter down for each criteria.
                if (findings.Count > 0)
                    findings = findings.Where(w => w.ToLower().Contains(wc.ToLower())).ToList();
                else
                    findings = Scanner.GetFileList.Where(w => w.ToLower().Contains(wc.ToLower())).ToList();

                //at any time, we haven't found anything,
                //then we haven't met the criteria
                if (findings.Count.Equals(0))
                    break;
            }

            var verifiedFiles = 0;
            if (findings != null && findings.Count > 0)
            {
                var fileList = new List<string>();
                retVal = true;
                foreach (var file in findings)
                {
                    if (GlobalSettings.HasShutdown)
                        break;

                    if (VerifyCriteria(drives, file, wcSearch))
                    {
                        if (verifiedFiles <= _maxFindingsCount)
                            fileList.Add(file);
                        verifiedFiles++;
                    }
                }
                //I found sending a message for each file found became really slow, so,
                //SearchMessage.SendMsg(SearchMessageType.SearchResults, file);

                //Bulk send of all findings by split of '\n', instant...  Balances Windows with Linux strings.
                var arrData = string.Join("\n", fileList);
                SearchMessage.SendMsg(SearchMessageType.SearchResults, arrData);
            }

            var showing = verifiedFiles > _maxFindingsCount ? _maxFindingsCount : verifiedFiles;
            //send found message
            SearchMessage.SendMsg(SearchMessageType.SearchStatus, $"Filtered: {showing.FormatByComma()}, Total Found: {verifiedFiles.FormatByComma()}");
            //send total count message, to ensure accuratness
            SearchMessage.SendMsg(SearchMessageType.ScanComplete, $"Cached: [{SystemScan.ScannedFolders.FormatByComma()}] Folders, [{SystemScan.ScannedFiles.FormatByComma()}] Files.");

            return retVal;
        }
        private bool VerifyCriteria(string[] drives, string file, string[] wcSearch)
        {
            var fileName = Path.GetFileName(file).ToLower();
            var folderName = Path.GetDirectoryName(file).ToLower();
            var findCount = 0;
            var loc = 0;
            var prevLoc = 0;

            if (drives.Where(w => w.Substring(0, 2).Equals(file.Substring(0, 2), StringComparison.CurrentCultureIgnoreCase)).Count().Equals(0))
                return false;

            if (Criteria.SearchDirectory)
            {
                foreach (var sArr in wcSearch)
                {
                    //met criteria, get out.
                    if (findCount >= wcSearch.Length)
                        break;

                    loc = folderName.IndexOf(sArr, prevLoc, StringComparison.CurrentCultureIgnoreCase);
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
            if (Criteria.SearchFilename && findCount < wcSearch.Length)
            {
                prevLoc = 0;
                foreach (var sArr in wcSearch.Skip(findCount))
                {
                    //met criteria, get out.
                    if (findCount >= wcSearch.Length)
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
            //we found if a folder is being added, files are missed, so lets sleep a sec and see if that helps.
            Tools.Sleep(100, SleepType.Milliseconds);

            if (File.Exists(e.FullPath))
            {
                createList.Add(Task.Run(() => { Scanner.AddFile(e.FullPath); return Task.CompletedTask; }));
                SearchMessage.SendMsg(SearchMessageType.Info, $"Added: [{e.FullPath}] to cache.");
            }
            else if (Directory.Exists(e.FullPath))
            {
                createList.AddRange(Scanner.ScanSubFolders(new string[] { e.FullPath }));
                if(createList.Count>0)
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
            //we found if a folder is being added, files are missed, so lets sleep a sec and see if that helps.
            Tools.Sleep(100, SleepType.Milliseconds);

            var dirWithSlash = e.FullPath.EndsWith("\\") ? e.FullPath : $"{e.FullPath}\\";
            var isDir = Scanner.IsDirectory(dirWithSlash);
            var isFile = Scanner.IsFile(e.FullPath);

            if (!isDir && !isFile)
                return;
           
            if (isDir)
                removeList.AddRange(Scanner.RemoveRootFolder(dirWithSlash));
            else if (isFile)
                removeList.Add(Task.Run(() => { Scanner.RemoveFile(e.FullPath); return Task.CompletedTask; }));

            if (removeList.Count > 0)
                SearchMessage.SendMsg(SearchMessageType.Info, $"Removing: [{removeList.Count.FormatByComma()}] Files and/or Folders");

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
                renameList.AddRange(Scanner.RemoveRootFolder(e.OldFullPath));
            else
                renameList.Add(Task.Run(() => { Scanner.RemoveFile(e.OldFullPath); return Task.CompletedTask; }));
            
            removedFolders = renameList.Count;
            fileGoodToGo = removedFolders > 0;

            if (isDir)
                renameList.AddRange(Scanner.ScanSubFolders(new string[] { e.FullPath }));
            else
                renameList.Add(Task.Run(() => { Scanner.AddFile(e.FullPath); return Task.CompletedTask; }));

            addFolders = renameList.Count - removedFolders;
            fileGoodToGo = fileGoodToGo || addFolders > 0;

            if (fileGoodToGo)
            {
                var info = removedFolders == 1 ? $"[{e.OldFullPath}]  ->  [{e.FullPath}]" : $"[{removedFolders.FormatByComma()}] Files";
                var vInfo = info.SplitByStr("->");
                if (info.Length > 100 && vInfo.Length == 2)
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
        private void SetupWatcher()
        {
            foreach (var drive in GlobalSettings.DriveList)
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

                _watcher.Add(watcher);
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
        public Task ScanToCache() => Scan(GlobalSettings.DriveList);
        public Task ScanToCache(DriveInfo drive) => Scan(new string[] { drive.Name });
        public Task ScanToCache(DriveInfo[] drives) => Scan(drives.Select(w => w.Name).ToArray());

        /// <summary>
        /// Starts search in cache if exists. Defaults drives to all drives.
        /// </summary>
        /// <param name="searchCriteria"></param>
        /// <returns></returns>
        public Task Search(string searchCriteria) => Search(GlobalSettings.DriveList, searchCriteria);
        public Task Search(DriveInfo drive, string searchCriteria) => Search(new string[] { drive.Name }, searchCriteria);
        public Task Search(DriveInfo[] drives, string searchCriteria) => Search(drives.Select(w => w.Name).ToArray(), searchCriteria);

        public void StopScan()
        {
            GlobalSettings.Shutdown();
        }
        #endregion
    }
}

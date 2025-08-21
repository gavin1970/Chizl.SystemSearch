using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.Diagnostics;

namespace Chizl.SystemSearch
{
    internal class SystemScan : IDisposable
    {
        private static long _scannedFolders;
        private static long _fastPullScannedFolders;
        private static long _scannedFiles;
        private static long _fastPullScannedFiles;

        private static readonly ConcurrentDictionary<string, string> _fileDictionary = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, byte> _folderDictionary = new ConcurrentDictionary<string, byte>();
        private static readonly ConcurrentDictionary<string, byte> _deniedDictionary = new ConcurrentDictionary<string, byte>();
        private static readonly ConcurrentQueue<string> _fileInfoQueue = new ConcurrentQueue<string>();
        private static Thread _fileInfoThread;

        private static readonly object _countLock = new object();

        #region Deconstructor
        internal SystemScan() { }
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    Internals.AutoEvents[AutoEvent.Shutdown].Set();

                disposedValue = true;
            }
        }
        ~SystemScan() => Dispose(disposing: false);
        void IDisposable.Dispose() => this.Dispose();
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        internal static long ScannedFolders 
        {
            get
            {
                lock (_countLock)
                {
                    if (!_scannedFolders.Equals(_fastPullScannedFolders))
                    {
                        _scannedFolders = _folderDictionary.Count;
                        _fastPullScannedFolders = _scannedFolders;
                    }
                }

                return _scannedFolders;
            }
            //only used for overwriting a count and wnat to update both vars.
            set
            {
                lock (_countLock)
                {
                    _scannedFolders = value;
                    _fastPullScannedFolders = _scannedFolders;
                }
            }
        }
        internal static long ScannedFiles
        {
            get 
            {
                lock (_countLock)
                {
                    if (!_scannedFiles.Equals(_fastPullScannedFiles))
                    {
                        _scannedFiles = _fileDictionary.Count;   //slower call, than just using the static int var.
                        _fastPullScannedFiles = _scannedFiles;
                    }
                }

                return _scannedFiles;
            }
            //only used for overwriting a count and wnat to update both vars.
            set
            {
                lock (_countLock)
                {
                    _scannedFiles = value;
                    _fastPullScannedFiles = _scannedFiles;
                }
            }
        }
        internal static void ThreadEventMonitor()
        {
            var waitTimer = TimeSpan.FromSeconds(50);
            var iEvent = -1;
            while (!GlobalSettings.HasShutdown)
            {
                iEvent = WaitHandle.WaitAny(Internals.AutoEvents, waitTimer);
                if (iEvent.Equals(AutoEvent.FileInfoQueue))
                {
                    while(!_fileInfoQueue.IsEmpty)
                    {
                        //insurance
                        if (GlobalSettings.HasShutdown)
                            break;

                        if (_fileInfoQueue.TryDequeue(out string fileName))
                        {
                            //if(_fileDictionary.TryGetValue(fileName, out string md5Hash))
                            //{
                                //attempt to build a quicker db lookup id, by using Int instead of string.
                                //TODO, get file info
                                //using (var fd = new FileDetails(fileName, md5Hash))
                                //    fd.SaveToFile();
                                //Thread.Sleep(1);
                            //}
                        }
                    }
                }
                else if (iEvent != AutoEvent.Shutdown && iEvent != WaitHandle.WaitTimeout)
                {
                    if (!File.Exists(".\\que_err.log"))
                    {
                        File.WriteAllText(".\\que_err.log", "");
                        Thread.Sleep(1000);
                    }

                    File.AppendAllText(".\\que_err.log", $"{DateTime.Now:YYYY/MM/dd HH:mm:ss.ffff}: Looking for iEvent = FileInfoQueue or Shutdown, but instead got: {iEvent}\n");
                }
            }
        }
        internal string[] GetFileList => _fileDictionary.Keys.ToArray();
        internal ConcurrentDictionary<string, string> FileDictionary => _fileDictionary;
        internal ConcurrentDictionary<string, byte> FolderDictionary => _folderDictionary;
        internal bool IsDirectory(string path) => FolderDictionary.Where(w => w.Key.Equals(path)).Any();
        internal bool IsFile(string path) => FileDictionary.Where(w => w.Equals(path)).Any();

        internal Task ScanDrives(string[] driveLetter, bool sendMsg)
        {
            GlobalSettings.Startup();

            var taskList = new List<Task>();
            var taskFolderList = new List<Task>();

            //reset folders, because they will be rescanned, but files
            //are based on Dictionary Size and will not need to be reset.
            ScannedFolders = 0;
            ScannedFiles = 0;

            _fileDictionary.Clear();
            _folderDictionary.Clear();

            //setup status for UI
            SetStatus(LookupStatus.Running);

            return Task.Run(() =>
            {
                if (sendMsg)
                {
                    SearchMessage.SendMsg(SearchMessageType.UpdateInprogress, $"Preparing for full scan. Please wait...");
                    Tools.Sleep(1, SleepType.Milliseconds);
                }

                foreach (var drive in driveLetter)
                {
                    try
                    {
                        //get root files, skip subfolders.
                        taskList.Add(ScanFolder(drive, true));

                        //foreach (var folder in Directory.EnumerateDirectories(drive))
                        //    taskList.Add(ScanFolder(drive));

                        //get all subfolders with each subfolder in their own thread/task
                        taskList.AddRange(ScanSubFolders(Directory.GetDirectories(drive)));
                    }
                    catch (Exception ex) 
                    {
                        SearchMessage.SendMsg(ex);
                        continue;
                    }
                }

                var drivesStr = string.Join(", ", driveLetter);
                SearchMessage.SendMsg(SearchMessageType.StatusMessage, $"Scanning: ({drivesStr}) - Please wait.");

                //asynchronously waits for all tasks
                Task.WaitAll(taskList.ToArray());

                var msgType = GlobalSettings.HasShutdown ? SearchMessageType.ScanAborted : SearchMessageType.ScanComplete;
                var statusType = GlobalSettings.HasShutdown ? LookupStatus.Aborted : LookupStatus.Completed;

                SetStatus(statusType, true);

                SearchMessage.SendMsg(msgType, $"Cached: ({ScannedFolders.FormatByComma()}) Folders, ({ScannedFiles.FormatByComma()}) Files");
            });
        }
        internal List<Task> ScanSubFolders(string[] folderList)
        {
            List<Task> taskList = new List<Task>();

            //only holds root folders of each drive.
            foreach (var subfolder in folderList)
            {
                try
                {
                    if (GlobalSettings.ScanSettings.AllowDir(subfolder))
                    {
                        //scan each root folder asynchronously.
                        taskList.Add(Task.Run(() =>
                        {
                            //ScanFolder(subfolder);
                            ScanFolder(subfolder);
                            //Tools.Sleep(1, SleepType.Milliseconds);
                        }));
                    }
                }
                catch (Exception ex)
                {
                    if (_deniedDictionary.TryAdd(subfolder, 0))
                        SearchMessage.SendMsg(ex);
                    continue;
                }
            }

            return taskList;
        }
        internal List<Task> RemoveRootFolder(string folder, bool sendInfoMsg = true)
        {
            folder = folder.Trim().ToLower();

            if (!folder.EndsWith(@"\"))
                folder += "\\";

            List<Task> removeTaskList = new List<Task>();
            var fileKeys = _fileDictionary.Keys.Where(w => w.ToLower().StartsWith(folder)).ToList();
            var folderKeys = _folderDictionary.Keys.Where(w => w.ToLower().StartsWith(folder)).ToList();

            SearchMessage.SendMsg(SearchMessageType.StatusMessage, $"Deleting '{fileKeys.Count}' file entries and '{folderKeys.Count}' folder entries from cache. - Please wait.");

            foreach (var key in fileKeys)
            {
                //remove each file asynchronously.
                removeTaskList.Add(
                    Task.Run(() =>
                    {
                        if (_fileDictionary.TryRemove(key, out _))
                        {
                            Interlocked.Decrement(ref _scannedFiles);
                            if (sendInfoMsg)
                                SearchMessage.SendMsg(SearchMessageType.Info, $"Removed '{key}' from cache.");
                        }
                    })
                );
            }

            foreach (var key in folderKeys)
            {
                //remove each root folder asynchronously.
                removeTaskList.Add(
                    Task.Run(() =>
                    {
                        if (_folderDictionary.TryRemove(key, out _))
                        {
                            Interlocked.Decrement(ref _scannedFolders);
                            SearchMessage.SendMsg(SearchMessageType.Info, $"Removed '{key}' from cache.");
                        }
                    })
                );
            }

            return removeTaskList;
        }
        internal Task ScanFolder(string folder, bool skipSubfolders = false)
        {
            var retVal = Task.CompletedTask;

            if (!folder.EndsWith(@"\"))
                folder += "\\";

            //ignoring temp paths and recycle bins
            if (GlobalSettings.HasShutdown)
                return retVal;

            if (!GlobalSettings.ScanSettings.AllowDir(folder))
            {
                SearchMessage.SendMsg(SearchMessageType.Info, $"Skipping Optional Folder: '{folder}");
                return retVal;
            }

            try
            {
                //This filters out files looking for and if all, the search string is not needed.
                foreach (var file in Directory.EnumerateFiles(folder))
                {
                    if (GlobalSettings.HasShutdown)
                        return retVal;

                    try
                    {
                        // The '0' is a dummy value, we only care about the value, only the key.
                        // This adds the path and TryAdd return false if already exists.
                        AddFile(file);
                        //if (_fileDictionary.TryAdd(file, 0))
                        //    Interlocked.Increment(ref _scannedFiles);
                    }
                    catch (Exception ex)
                    {
                        SearchMessage.SendMsg(ex);
                        continue;
                    }
                }
            }
            catch (IOException ioex)
            {
                SearchMessage.SendMsg(ioex);
                return retVal;
            }
            catch (Exception ex)
            {
                //when you don't have access to the folder, this will provide an IO
                //error, so instead of trying to access it's subdirectories, lets exit.
                //if already exists in dictionary, then we don't want to send the message again.
                if (_deniedDictionary.TryAdd(folder, 0))
                    SearchMessage.SendMsg(ex);

                return retVal;
            }

            if (skipSubfolders)
                return retVal;

            try
            {
                if (_folderDictionary.TryAdd(folder, 0))
                {
                    Interlocked.Increment(ref _scannedFolders);
                    SearchMessage.SendMsg(SearchMessageType.FileScanStatus, $"Scanning: ({_scannedFolders}) Folders, ({_scannedFiles}) Files");
                }

                //lets look at all subfolders within this folder.
                foreach (var subFolder in Directory.EnumerateDirectories(folder))
                {
                    try
                    {
                        if (GlobalSettings.HasShutdown)
                            return retVal;

                        //lets look at all files within this subfolder.
                        ScanFolder(subFolder);
                    }
                    catch (Exception ex)
                    {
                        SearchMessage.SendMsg(ex);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                SearchMessage.SendMsg(ex);
            }

            return retVal;
        }

        internal bool AddFile(string fileName, bool addMD5Hash = false)
        {
            var md5Hash = addMD5Hash ? Tools.CreateMD5(fileName, ReturnCase.Lower) : "";

            //verifies root folder of file and determines if it should continue.
            if (_fileDictionary.TryAdd(fileName, md5Hash))
            {
                Interlocked.Increment(ref _scannedFiles);
                if (addMD5Hash)
                {
                    _fileInfoQueue.Enqueue(fileName);   //not unique
                    if (_fileInfoThread == null || !_fileInfoThread.IsAlive)
                    {
                        _fileInfoThread = new Thread(() => { ThreadEventMonitor(); });
                        _fileInfoThread.Start();
                    }

                    Internals.AutoEvents[AutoEvent.FileInfoQueue].Set();
                }
                return true;
            }
            else
                return false;
        }
        internal bool RemoveFile(string fileName, bool sendInfoMsg = true)
        {
            //verifies root folder of file and determines if it should continue.
            if (_fileDictionary.TryRemove(fileName, out _))
            {
                Interlocked.Decrement(ref _scannedFiles);
                if (sendInfoMsg)
                    SearchMessage.SendMsg(SearchMessageType.Info, $"Removed '{fileName}' from cache.");
                return true;
            }
            else
                return false;
        }
        internal void SetStatus(LookupStatus status, bool andFullStatus = false)
        {
            GlobalSettings.CurrentStatus = status;
            if (andFullStatus)
                GlobalSettings.FullScanCompleted = !GlobalSettings.HasShutdown;
        }
        internal void SetStatus(bool success) => SetStatus((success ? LookupStatus.Completed : LookupStatus.Aborted));
    }
}

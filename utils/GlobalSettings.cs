using Chizl.ThreadSupport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chizl.SystemSearch
{
    internal static class GlobalSettings
    {
        internal static readonly object _lockPath = new object();

        private static long _scanStarted = 0;
        private static long _shutDown = 0;
        private static long _fullScanCompleted = 0;
        private static SystemScan _scanner = new SystemScan();
        private static Bool IsRefreshing = new Bool();

        static GlobalSettings() { }
        public static ScanProperties ScanSettings { get; } = new ScanProperties();

        /// <summary>
        /// Scan options to be set from the parent application or use defaults.
        /// </summary>
        public static ConcurrentDictionary<string, bool> RefreshFolder { get; } = new ConcurrentDictionary<string, bool>();
        public static ConcurrentDictionary<string, bool> CustomExclusions { get; } = new ConcurrentDictionary<string, bool>();

        #region Shortcut Methods
        public static bool AllowDir(string path) => ScanSettings.AllowDir(path);
        public static void AddRemove(string dir, bool add)
        {
            if (!IgnoreChange)
            {
                if (RefreshFolder.TryAdd(dir, add))
                {
                    SearchMessage.SendMsg(SearchMessageType.UpdateInProgress, $"Updating cache by '{(add ? "Adding" : "Removing")}' entries associated to '{dir}'.  Please wait...");
                    // set to true, if previous was false, lets Task.Run().
                    if (!IsRefreshing.SetVal(true))
                    {
                        Task.Run(() =>
                        {
                            CheckRefresh();
                        })
                        .ContinueWith(previousTask =>
                        {
                            IsRefreshing.SetFalse();
                        }).Wait();
                    }
                }
            }
        }
        #endregion

        // Volatile.Read vs Interlocked.Read (deeper and more precise)

        #region Shortcut Properties
        /// <summary>
        /// Thread safe boolean.
        /// </summary>
        public static bool FullScanCompleted
        {
            get => Interlocked.Read(ref _fullScanCompleted) == 1;
            set => Interlocked.Exchange(ref _fullScanCompleted, (value ? 1 : 0));
        }
        public static LookupStatus CurrentStatus { get; set; } = LookupStatus.NotStarted;
        public static string[] DriveList { get; } = DriveInfo.GetDrives().Select(s => CheckDriveName(s.Name)).ToArray();
        public static void Startup(LookupStatus status = LookupStatus.NotStarted)
        {
            HasShutdown = false;
            CurrentStatus = status;
            Interlocked.Increment(ref _scanStarted);
        }
        private static bool EndScanCount()
        {
            if (Interlocked.Read(ref _scanStarted) > 0)
                Interlocked.Decrement(ref _scanStarted);
            return Interlocked.Read(ref _scanStarted).Equals(0);
        }
        public static void Ended(LookupStatus status = LookupStatus.Completed)
        {
            if (EndScanCount())
                CurrentStatus = status;
        }

        public static void Shutdown()
        {
            HasShutdown = true;
            Ended(LookupStatus.Aborted);
            Internals.AutoEvents[AutoEvent.Shutdown].Set();
        }
        public static bool HasShutdown
        {
            get => Interlocked.Read(ref _shutDown) == 1;
            set => Interlocked.Exchange(ref _shutDown, (value ? 1 : 0));
        }
        public static bool IgnoreChange => ScanSettings.IgnoreChange;
        private static string CheckDriveName(string driveName) => driveName.EndsWith("\\") ? driveName : $"{driveName}\\";
        #endregion

        internal static Task CheckRefresh()
        {
            if (IgnoreChange || RefreshFolder.IsEmpty)
            {
                IsRefreshing.SetFalse();
                return Task.CompletedTask;
            }

            Startup(LookupStatus.Running);
            List<Task> queTasks = new List<Task>();

            return Task.Run(async () =>
            {
                try
                {
                    var addList = RefreshFolder.Where(w => w.Value).Select(s => s.Key).ToArray();
                    var deleteList = RefreshFolder.Where(w => !w.Value).Select(s => s.Key).ToArray();

                    foreach (var actionFolder in addList)
                    {
                        RefreshFolder.TryRemove(actionFolder, out _);
                        if (Directory.Exists(actionFolder))
                        {
                            // add that specific folder, without subfolders.
                            queTasks.Add(_scanner.ScanFolder(actionFolder, true));
                            // add all subfolders with each in their own thread task.
                            queTasks.AddRange(_scanner.ScanSubFolders(Directory.GetDirectories(actionFolder), false));
                        }
                        else
                            SearchMessage.SendMsg(SearchMessageType.Warning, $"Refresh folder '{actionFolder}' doesn't exist.");
                    }

                    foreach (var actionFolder in deleteList)
                    {
                        RefreshFolder.TryRemove(actionFolder, out _);
                        queTasks.AddRange(_scanner.RemoveRootFolder(actionFolder));
                    }

                    // wait for all thread/tasks to complete.
                    Task.WaitAll(queTasks.ToArray());
                    //clear array
                    queTasks.Clear();
                    // send complete message to UI
                    SearchMessage.SendMsg(SearchMessageType.FileScanStatus, $"Cached: [{SystemScan.ScannedFolders.FormatByComma()}] Folders, [{SystemScan.ScannedFiles.FormatByComma()}] Files.");
                    // wait one hundredth of a sec
                    await Tools.Delay(100, SleepType.Milliseconds);

                    SearchMessage.SendMsg(SearchMessageType.ScanComplete, $"Cached: [{SystemScan.ScannedFolders.FormatByComma()}] Folders, [{SystemScan.ScannedFiles.FormatByComma()}] Files.");
                }
                catch (Exception ex)
                {
                    SearchMessage.SendMsg(SearchMessageType.Exception, $"Exception during folder refresh: '{ex.Message}'");
                }
                finally
                {
                    Ended();
                    IsRefreshing.SetFalse();
                }
            });
        }
    }
}

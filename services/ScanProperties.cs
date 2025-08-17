using Chizl.ThreadSupport;
using System;
using System.Threading;

namespace Chizl.SystemSearch
{
    public sealed class ScanProperties
    {
        private Bool _file = new Bool(true);
        private Bool _dir = new Bool(false);

        private long _ignoreChange = 0;
        private long _allowCachedJson = 0;
        private long _allowWinRoot = 1;
        private long _allowSysRoot = 1;
        private long _allowUser = 1;
        private long _allowTemp = 0;
        private long _allowInternetCache = 0;
        private long _allowRecycleBin = 0;

        //prevent public constructor of class.
        internal ScanProperties() { }
        //private SearchCriterias Criteria { get; } = new SearchCriterias();

        #region Public Methods
        /// <summary>
        /// Used to validate folders to ensure they meet the criteria.
        /// </summary>
        /// <param name="path">Path that will be validated with each property that is set to false.</param>
        /// <returns>true is returned if the property isn't part of the evaluation or if found and the property is allowed.</returns>
        public bool AllowDir(string path)
        {
            if (!AllowCachedJson && path.StartsWith(ScanPaths.CachedJson))
                return false;

            if (!AllowWindows && path.StartsWith(ScanPaths.WindowsDir))
                return false;

            if (!AllowSystem && path.StartsWith(ScanPaths.SystemDir))
                return false;

            if (!AllowUser && path.StartsWith(ScanPaths.UserDir))
                return false;

            if (!AllowTemp && path.StartsWith(ScanPaths.TempDir))
                return false;

            if (!AllowInternetCache && path.StartsWith(ScanPaths.InternetCache))
                return false;

            if (!AllowRecycleBin && path.StartsWith(ScanPaths.RecycleBinDir))
                return false;

            return true;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// ** Thread Safe **<br/>
        /// If you loading configuration for the first time and you don't want to auto 
        /// scan or delete from cache instantly, then this should be set to true.<br/>
        /// Remember to set it back to false or no auto cache sync will occur.
        /// </summary>
        public bool IgnoreChange
        {
            get => Interlocked.Read(ref _ignoreChange) == 1;
            set => Interlocked.Exchange(ref _ignoreChange, (value ? 1 : 0));
        }
        /// <summary>
        /// Always false
        /// </summary>
        public bool AllowCachedJson => _allowCachedJson == 1;
        /// <summary>
        /// ** Thread Safe **<br/>
        /// Allow Windows Root path to be scanned.<br/>
        /// Usually: C:\Windows\<br/>
        /// Only if WindowsDir and SystemDir are the same, AllowSystem 
        /// will be overridden to the same value as AllowWindows.
        /// </summary>
        public bool AllowWindows
        {
            get => Interlocked.Read(ref _allowWinRoot) == 1;
            set
            {
                var existing = AllowWindows;
                Interlocked.Exchange(ref _allowWinRoot, (value ? 1 : 0));
                if (existing != AllowWindows)
                    GlobalSettings.AddRemove(WindowsDir, AllowWindows);

                CheckWinSys();
            }
        }
        /// <summary>
        /// ** Thread Safe **<br/>
        /// Allow System Root path to be scanned.<br/>
        /// Usually same as Windows, but could be different.<br/>
        /// Only if WindowsDir and SystemDir are the same, AllowSystem 
        /// will be overridden to the same value as AllowWindows.
        /// </summary>
        public bool AllowSystem
        {
            get => Interlocked.Read(ref _allowSysRoot) == 1;
            set
            {
                var existing = AllowSystem;
                Interlocked.Exchange(ref _allowSysRoot, (value ? 1 : 0));
                if (existing != AllowSystem)
                    GlobalSettings.AddRemove(SystemDir, AllowSystem);

                CheckWinSys();
            }
        }
        /// <summary>
        /// ** Thread Safe **<br/>
        /// Allow YourProfile path to be scanned.<br/>
        /// Usually: C:\Users\YourProfile\
        /// </summary>
        public bool AllowUser
        {
            get => Interlocked.Read(ref _allowUser) == 1;
            set
            {
                var existing = AllowUser;
                Interlocked.Exchange(ref _allowUser, (value ? 1 : 0));
                if (existing != AllowUser)
                    GlobalSettings.AddRemove(UserDir, AllowUser);
            }
        }
        /// <summary>
        /// ** Thread Safe **<br/>
        /// Allow Temp path to be scanned.<br/>
        /// Usually: C:\Users\YourProfile\AppData\Local\Temp\
        /// </summary>
        public bool AllowTemp
        {
            get => Interlocked.Read(ref _allowTemp) == 1;
            set
            {
                var existing = AllowTemp;
                Interlocked.Exchange(ref _allowTemp, (value ? 1 : 0));
                if (existing != AllowTemp)
                    GlobalSettings.AddRemove(TempDir, AllowTemp);
            }
        }
        /// <summary>
        /// ** Thread Safe **<br/>
        /// Allow Temp Internet Cached path to be scanned.<br/>
        /// Usually: C:\Users\YourProfile\AppData\Local\Microsoft\Windows\INetCache\
        /// </summary>
        public bool AllowInternetCache
        {
            get => Interlocked.Read(ref _allowInternetCache) == 1;
            set
            {
                var existing = AllowInternetCache;
                Interlocked.Exchange(ref _allowInternetCache, (value ? 1 : 0));
                if (existing != AllowInternetCache)
                    GlobalSettings.AddRemove(InternetCache, AllowInternetCache);
            }
        }
        /// <summary>
        /// ** Thread Safe **<br/>
        /// Allow RecycleBin path to be scanned.<br/>
        /// Usually: C:\\$Recycle.Bin
        /// </summary>
        public bool AllowRecycleBin
        {
            get => Interlocked.Read(ref _allowRecycleBin) == 1;
            set
            {
                var existing = AllowRecycleBin;
                Interlocked.Exchange(ref _allowRecycleBin, (value ? 1 : 0));
                if (existing != AllowRecycleBin)
                    GlobalSettings.AddRemove(RecycleBinDir, AllowRecycleBin);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public bool SearchFilename
        {
            get
            {
                return _file;
            }
            set
            {
                _file.SetVal(value);
                if (!_file && !_dir)
                    _dir.SetVal(true);
            }
        }
        public bool SearchDirectory
        {
            get
            {
                return _dir;
            }
            set
            {
                _dir.SetVal(value);
                if (!_dir && !_file)
                    _file.SetVal(true);
            }
        }
        public string WindowsDir => ScanPaths.WindowsDir;
        public string SystemDir => ScanPaths.SystemDir;
        public string UserDir => ScanPaths.UserDir;
        public string TempDir => ScanPaths.TempDir;
        public string InternetCache => ScanPaths.InternetCache;
        public string RecycleBinDir => ScanPaths.RecycleBinDir;
        #endregion

        private void CheckWinSys()
        {
            if (ScanPaths.SystemDir.Equals(ScanPaths.WindowsDir))
                _allowSysRoot = _allowWinRoot;
        }
    }
}

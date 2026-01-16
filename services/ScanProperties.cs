using Chizl.ThreadSupport;
using System.Linq;

namespace Chizl.SystemSearch
{
    public sealed class ScanProperties
    {
        // Thread safe boolean
        private Bool _file = Bool.True;
        private Bool _dir = Bool.False;

        private Bool _ignoreChange = Bool.True;
        private Bool _allowWinRoot = Bool.True;
        private Bool _allowSysRoot = Bool.True;
        private Bool _allowUser = Bool.True;
        private Bool _allowTemp = Bool.False;
        private Bool _allowInternetCache = Bool.False;
        private Bool _allowRecycleBin = Bool.False;

        // prevent public constructor of class.
        internal ScanProperties() { }

        #region Public Methods
        /// <summary>
        /// Used to validate folders to ensure they meet the criteria.
        /// </summary>
        /// <param name="path">Path that will be validated with each property that is set to false.</param>
        /// <returns>true is returned if the property isn't part of the evaluation or if found and the property is allowed.</returns>
        public bool AllowDir(string path)
        {
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

            return GlobalSettings.CustomExclusions.Where(w => path.ToLower().Contains(w.Key.ToLower())).Count() == 0;
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
            get => _ignoreChange;
            set => _ignoreChange.TrySetValue(value);
        }
        /// <summary>
        /// ** Thread Safe **<br/>
        /// Allow Windows Root path to be scanned.<br/>
        /// Usually: C:\Windows\<br/>
        /// Only if WindowsDir and SystemDir are the same, AllowSystem 
        /// will be overridden to the same value as AllowWindows.
        /// </summary>
        public bool AllowWindows
        {
            get => _allowWinRoot;
            set
            {
                if (_allowWinRoot.TrySetValue(value))  //set only if there is a difference
                    GlobalSettings.AddRemove(WindowsDir, _allowWinRoot);

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
            get => _allowSysRoot;
            set
            {
                if (_allowSysRoot.TrySetValue(value))  //set only if there is a difference
                    GlobalSettings.AddRemove(SystemDir, _allowSysRoot);

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
            get => _allowUser;
            set
            {
                if (_allowUser.TrySetValue(value))  //set only if there is a difference
                    GlobalSettings.AddRemove(UserDir, _allowUser);
            }
        }
        /// <summary>
        /// ** Thread Safe **<br/>
        /// Allow Temp path to be scanned.<br/>
        /// Usually: C:\Users\YourProfile\AppData\Local\Temp\
        /// </summary>
        public bool AllowTemp
        {
            get => _allowTemp;
            set
            {
                if (_allowTemp.TrySetValue(value))  //set only if there is a difference
                    GlobalSettings.AddRemove(TempDir, _allowTemp);
            }
        }
        /// <summary>
        /// ** Thread Safe **<br/>
        /// Allow Temp Internet Cached path to be scanned.<br/>
        /// Usually: C:\Users\YourProfile\AppData\Local\Microsoft\Windows\INetCache\
        /// </summary>
        public bool AllowInternetCache
        {
            get => _allowInternetCache;
            set
            {
                if (_allowInternetCache.TrySetValue(value))  //set only if there is a difference
                    GlobalSettings.AddRemove(InternetCache, _allowInternetCache);
            }
        }
        /// <summary>
        /// ** Thread Safe **<br/>
        /// Allow RecycleBin path to be scanned.<br/>
        /// Usually: C:\\$Recycle.Bin
        /// </summary>
        public bool AllowRecycleBin
        {
            get => _allowRecycleBin;
            set
            {
                if (_allowRecycleBin.TrySetValue(value))  //set only if there is a difference
                    GlobalSettings.AddRemove(RecycleBinDir, _allowRecycleBin);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public bool SearchFilename
        {
            get => _file;
            set
            {
                _file.SetVal(value);
                if (!_file && !_dir)
                    _dir.SetVal(true);
            }
        }
        public bool SearchDirectory
        {
            get => _dir;
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

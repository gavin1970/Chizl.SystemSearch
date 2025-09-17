using System;
using System.IO;
using System.Linq;

namespace Chizl.SystemSearch
{
    public static class ScanPaths
    {
        private static string _winRoot = string.Empty;
        private static string _sysRoot = string.Empty;
        private static string _userRoot = string.Empty;
        private static string _tempRoot = string.Empty;
        private static string _internetCache = string.Empty;
        private static string _recycleBin = "C:\\$Recycle.Bin";

        static ScanPaths()
        {
            _tempRoot = ProperCase(Path.GetTempPath());
            _sysRoot = ProperCase(Environment.GetEnvironmentVariable("SystemRoot"));
            _winRoot = ProperCase(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
            _internetCache = ProperCase(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache));
            _userRoot = ProperCase(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

            if (!Directory.Exists(_recycleBin))
                _recycleBin = string.Empty;
            else
                _recycleBin = ProperCase(_recycleBin);
        }

        public static string WindowsDir => _winRoot;
        public static string SystemDir => _sysRoot;
        public static string UserDir => _userRoot;
        public static string TempDir => _tempRoot;
        public static string InternetCache => _internetCache;
        public static string RecycleBinDir => _recycleBin;

        #region Private Helper Methods
        /// <summary>
        /// Only have to Propercase 1 time, to match with what exists.  This is so, 
        /// we don't have to IgnoreCase for every folder and slow the process down.
        /// C:\WINDOWS is the biggest issue.
        /// </summary>
        private static string ProperCase(string path)
        {
            if (path.EndsWith("\\"))
                path = path.Substring(0, path.Length - 1);

            var root = Directory.GetParent(path).FullName;
            var rootList = Directory.GetDirectories(root).ToList();
            var newPath = rootList.FirstOrDefault(f => f.StartsWith(path, StringComparison.CurrentCultureIgnoreCase));

            // This path: C:\User\You\AppData\Local\Temporary Internet Files
            // contains: C:\User\You\AppData\Local\Temp
            if (!newPath.EndsWith("\\"))
                newPath += "\\";

            return newPath;
        }
        #endregion
    }
}

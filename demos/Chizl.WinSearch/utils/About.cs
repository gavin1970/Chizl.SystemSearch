using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Chizl.Applications
{
    internal static class About
    {
        private static readonly object _assemblyLock = new object();

        private static bool _loaded = false;
        private static string _fileVersion = null;
        private static string _productVersion = null;
        private static string _title = null;
        private static string _company = null;
        private static string _productName = null;
        private static string _copyright = null;
        private static string _description = null;
        private static string _trademark = null;
        private static string _appFullPath = null;
        private static string _appRootDir = null;
        private static string _appFileName = null;

        static About() => LoadAppInfo();
        public static string FileVersion => _fileVersion;
        public static string ProductVersion => _productVersion;
        public static string Title => _title;
        public static string TitleWithFileVersion => $"{_title} v{_fileVersion}";
        public static string Company => _company;
        public static string ProductName => _productName;
        public static string Copyright => _copyright;
        public static string Description => _description;
        public static string Trademark => _trademark;
        public static string AppFullPath => _appFullPath;
        public static string AppRootDir => _appRootDir;
        public static string AppFileName => _appFileName;

        private static void LoadAppInfo()
        {
            lock (_assemblyLock)
            {
                if (!_loaded)
                {
                    FileVersionInfo fi = FileVersionInfo.GetVersionInfo(Application.ExecutablePath);
                    _fileVersion = fi.FileVersion;
                    _productVersion = fi.ProductVersion;
                    _title = fi.FileDescription.Trim();
                    _company = fi.CompanyName.Trim();
                    _productName = fi.ProductName.Trim();
                    _copyright = fi.LegalCopyright.Trim();
                    _description = fi.Comments.Trim();
                    _trademark = fi.LegalTrademarks.Trim();
                    _appFullPath = fi.FileName.Trim();
                    _appRootDir = Path.GetDirectoryName(_appFullPath);
                    _appFileName = fi.OriginalFilename.Trim();
                    _loaded = true;
                }
            }
        }
    }
}

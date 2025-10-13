using Chizl.SystemSearch;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Chizl.SearchSystemUI
{
    public enum FilterType
    {
        Unknown,
        NoExtension,
        Extension,
        Drive,
        Contains,
    }

    public enum SearchAttributes
    {
        Filename_N = 0,
        Folder_and_Filename_N,
        Date_and_Time_Y,
        File_Size_in_bytes_Y,
        File_Size_Y,
    }

    public enum SearchDirecction
    {
        Greater_Than = 0,
        Greater_Or_Equal,
        Equal,
        Less_Than,
        Less_Or_Equal,
    }

    internal static class GlobalSetup
    {
        private static IOFinder _finder;
        private static DriveInfo[] _drives;
        public static DriveInfo[] DriveList
        {
            get { return _drives; }
            set
            {
                _drives = value;
                if (_finder != null)
                    _finder.SetupWatcher(_drives);
            }
        }
        public static IOFinder Finder
        {
            get
            {
                if (_finder == null)
                {
                    _finder = new IOFinder();
                    if (_drives != null)
                        _finder.SetupWatcher(_drives);
                }
                return _finder;
            }
        }
        public static ListViewItem[] GetFileInfo(string[] unfiltList)
        {
            var listViewItems = new List<ListViewItem>();
            foreach (var filePath in unfiltList)
            {
                try
                {
                    var fi = new FileInfo(filePath);
                    var liv = new ListViewItem(fi.Name);
                    liv.SubItems.Add(fi.Length.ToString());
                    liv.SubItems.Add($"{fi.Length.FormatByteSize()}");
                    liv.SubItems.Add(fi.CreationTime.ToString("MM/dd/yyyy HH:mm:ss"));
                    liv.SubItems.Add(fi.LastWriteTime.ToString("MM/dd/yyyy HH:mm:ss"));
                    liv.SubItems.Add(fi.FullName);

                    listViewItems.Add(liv);
                }
                catch { continue; }
            }
            return listViewItems.ToArray();
        }
        public static ListViewItem[] GetFilterInfo(SubFilterExclusion[] unfiltList)
        {
            var listViewItems = new List<ListViewItem>();
            foreach (var filter in unfiltList)
            {
                var liv = new ListViewItem(filter.FilterRaw);
                liv.SubItems.Add(filter.Type.ToString());
                listViewItems.Add(liv);
            }
            return listViewItems.ToArray();
        }
    }
}

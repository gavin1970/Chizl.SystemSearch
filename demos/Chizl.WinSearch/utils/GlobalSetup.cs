using Chizl.SystemSearch;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        const string _customExclusions = @".\custExc.dat";
        public static string WindowTitlebarText { get; set; }

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
        public static ListViewItem[] GetFileInfo(string[] unfiltList, ConcurrentDictionary<string, (string, string[])> contentList)
        {
            var listViewItems = new List<ListViewItem>();
            var rescanFiles = new List<string>();

            var useContent = contentList?.Keys.Count() > 0;
            foreach (var filePath in unfiltList)
            {
                try
                {
                    var fi = new FileInfo(filePath);
                    if (fi.Exists)
                    {
                        var liv = new ListViewItem(fi.Name);
                        liv.SubItems.Add(fi.Length.ToString());
                        liv.SubItems.Add($"{fi.Length.FormatByteSize()}");
                        liv.SubItems.Add(fi.CreationTime.ToString("MM/dd/yyyy HH:mm:ss"));
                        liv.SubItems.Add(fi.LastWriteTime.ToString("MM/dd/yyyy HH:mm:ss"));
                        liv.SubItems.Add(fi.Extension);
                        if (useContent)
                        {
                            if (contentList.TryGetValue(filePath, out var content))
                            {
                                (string count, string[] list) = content;
                                var tag = liv.SubItems.Add(count);
                                tag.Tag = string.Join("\n", list);//.Replace("\r", "\n");
                            }
                            else
                                liv.SubItems.Add("...");
                        }
                        else
                            liv.SubItems.Add("---");
                        liv.SubItems.Add(fi.FullName);

                        listViewItems.Add(liv);
                    }
                    else
                        rescanFiles.Add(fi.FullName);
                }
                catch { continue; }
            }

            if (rescanFiles.Any())
                _ = Finder.CheckCache(rescanFiles.ToArray());

            return listViewItems.ToArray();
        }
        public static ListViewItem[] GetFilterInfo(SubFilterExclusion[] unfiltList) => GetFilterInfo(unfiltList, Color.Empty);
        public static ListViewItem[] GetFilterInfo(SubFilterExclusion[] unfiltList, Color bgColor)
        {
            var listViewItems = new List<ListViewItem>();
            foreach (var filter in unfiltList)
            {
                var liv = new ListViewItem(filter.FilterRaw);
                if (!bgColor.IsEmpty)
                    liv.BackColor = bgColor;
                liv.SubItems.Add(filter.Type.ToString());
                listViewItems.Add(liv);
            }
            return listViewItems.ToArray();
        }
        public static Task<string[]> GetScanExclusions() => Task.Run(() => 
        {
            var lst = Finder.GetScanExclusions().Result.ToArray();
            if (lst.Length > 0)
                return lst; //return all existing

            if (!File.Exists(_customExclusions))
                return lst; //return empty
            
            var lines = File.ReadAllLines(_customExclusions);
            foreach (var confLine in lines)
            {
                if (!string.IsNullOrWhiteSpace(confLine))
                    Finder.AddScanExclusion(confLine);
            }

            return Finder.GetScanExclusions().Result.ToArray(); 
        });
        public static Task<bool> AddScanExclusion(string pathOrFileContains) => Task.Run(() => 
        {
            if (!File.Exists(_customExclusions))
                File.Create(_customExclusions).Close();

            if (Finder.AddScanExclusion(pathOrFileContains).Result)
            {
                File.AppendAllText(_customExclusions, $"{pathOrFileContains}\n");
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        });
        public static Task<bool> RemoveScanExclusion(string pathOrFileContains) => Task.Run(() => 
        {
            if (!File.Exists(_customExclusions))
                File.Create(_customExclusions).Close();

            if (Finder.RemoveScanExclusion(pathOrFileContains).Result)
            {
                var sb = new StringBuilder();
                var list = Finder.GetScanExclusions().Result;
                foreach(var item in list)
                    sb.AppendLine(item);

                if (list.Length > 0)
                    sb.AppendLine();

                File.WriteAllText(_customExclusions, sb.ToString());
                return Task.FromResult(true);
            }
            
            return Task.FromResult(false);
        });
    }
}

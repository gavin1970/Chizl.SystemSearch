using Chizl.SystemSearch;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Chizl.SearchSystemUI
{
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
        public static IOFinder Finder = new IOFinder();
        public static List<bool> UseDirection = new List<bool> { };

        public static string CleanEnumName(string name, bool addDirection)
        {
            var subSize = name.EndsWith("_Y") || name.EndsWith("_N") ? 1 : 0;
            var addDir = name.EndsWith("_Y");

            name = name.Replace("_and_", " & ").Replace("_", " ");
            var retVal = name.Substring(0, name.Length - subSize).Trim();

            if(addDirection)
                UseDirection.Add(addDir);

            return retVal;
        }
        public static ListViewItem[] GetFileInfo(string[] unfiltList)
        {
            var listViewItems = new List<ListViewItem>();
            foreach (var filePath in unfiltList)
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

            return listViewItems.ToArray();
        }
    }
}

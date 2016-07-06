using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Diagnostics.Tools
{
    public class FileSorter
    {
        public static List<FileInfo> SortByLastWriteTime(string path, string filter)
        {
            FileInfo[] tFiles = new DirectoryInfo(path).GetFiles(filter);
            List<FileInfo> tReturn = new List<FileInfo>();
            foreach (FileInfo item in tFiles)
                tReturn.Add(item);
            tReturn.Sort(new LastWriteTimeComparer());
            tFiles = null;
            return tReturn;
        }

        private class LastWriteTimeComparer : IComparer<FileInfo>
        {
            public int Compare(FileInfo f2, FileInfo f1)
            {
                return DateTime.Compare(f1.LastWriteTime, f2.LastWriteTime);
            }

        }

    }
}

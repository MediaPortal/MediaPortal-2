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
        #region Public Methods

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

        #endregion Public Methods

        #region Private Classes

        private class LastWriteTimeComparer : IComparer<FileInfo>
        {
            #region Public Methods

            public int Compare(FileInfo f1, FileInfo f2)
            {
                return DateTime.Compare(f2.LastWriteTime, f1.LastWriteTime);
            }

            #endregion Public Methods

        }

        #endregion Private Classes

    }
}

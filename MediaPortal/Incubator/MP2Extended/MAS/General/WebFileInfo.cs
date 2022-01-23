#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.IO;

namespace MediaPortal.Plugins.MP2Extended.MAS.General
{
    public class WebFileInfo : WebObject
    {
        public WebFileInfo()
        {
            LastAccessTime = new DateTime(1970, 1, 1);
            LastModifiedTime = new DateTime(1970, 1, 1);
            Exists = false;
        }

        public WebFileInfo(FileInfo info)
            : this()
        {
            if (info != null)
            {
                IsLocalFile = true;
                Size = info.Length;
                Name = info.Name;
                Path = info.FullName;
                LastAccessTime = info.LastAccessTime;
                LastModifiedTime = info.LastWriteTime;
                Extension = info.Extension;
                IsReadOnly = info.IsReadOnly;
                Exists = true;
                OnNetworkDrive = new Uri(info.FullName).IsUnc;
            }
        }

        public WebFileInfo(string path)
            : this(File.Exists(path) ? new FileInfo(path) : null)
        {
        }

        public bool IsLocalFile { get; set; }
        public long Size { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public DateTime LastAccessTime { get; set; }
        public DateTime LastModifiedTime { get; set; }
        public string Extension { get; set; }
        public bool IsReadOnly { get; set; }
        public bool Exists { get; set; }
        public bool OnNetworkDrive { get; set; }

        public override string ToString()
        {
            return Path;
        }
    }
}

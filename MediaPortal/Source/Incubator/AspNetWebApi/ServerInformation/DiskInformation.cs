#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

namespace MediaPortal.Plugins.AspNetWebApi.ServerInformation
{
  public class DiskInformation
  {
    /// <summary>
    /// Volume Label, e.g. "Media 01"
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Drive Letter, e.g. "C:\"
    /// </summary>
    public string Letter { get; set; }

    /// <summary>
    /// File System, e.g. "NTFS"
    /// </summary>
    public string FileSystem { get; set; }

    /// <summary>
    /// Free Diskspace in bytes
    /// </summary>
    public long TotalFreeSpace { get; set; }

    /// <summary>
    /// Total disk size in bytes
    /// </summary>
    public long TotalSize { get; set; }
  }
}

#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.IO;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;

namespace Components.UPnPServer
{
  // TODO Albert78: After this class is now cleaned up, it doesn't provide more functionality
  // than directly using MediaServerCore2, so it should be deleted.
  public class UPnPMediaServer2 : MarshalByRefObject, IDisposable
  {
    // Fields
    protected MediaServerCore2 _mediaServerCore;

    // Methods
    public UPnPMediaServer2(MediaServerCore2 mediaServerCore)
    {
      if (mediaServerCore == null)
        throw new NullReferenceException("MediaServer core object is null");
      _mediaServerCore = mediaServerCore;
    }

    public void Dispose()
    {
      _mediaServerCore.Dispose();
    }

    public DvMediaContainer2 AddDirectory(DvMediaContainer2 parent, string subfolder)
    {
      return _mediaServerCore.AddDirectory(parent, subfolder);
    }

    public void DeserializeTree(BinaryFormatter formatter, FileStream fstream)
    {
      _mediaServerCore.DeserializeTree(formatter, fstream);
    }

    public IList GetSharedDirectories()
    {
      return _mediaServerCore.Directories;
    }

    public string[] GetSharedDirectoryNames()
    {
      IList directories = _mediaServerCore.Directories;
      string[] strArray = new string[directories.Count];
      int index = 0;
      foreach (MediaServerCore2.SharedDirectoryInfo info in directories)
      {
        strArray[index] = (string)info.directory.Clone();
        index++;
      }
      return strArray;
    }

    public bool RemoveDirectory(DirectoryInfo directory)
    {
      return _mediaServerCore.RemoveDirectory(directory);
    }

    public void ResetTree()
    {
      _mediaServerCore.ResetCoreRoot();
    }

    public void SerializeTree(BinaryFormatter formatter, FileStream fstream)
    {
      _mediaServerCore.SerializeTree(formatter, fstream);
    }

    public bool UpdatePermissions(DirectoryInfo directory, bool restricted, bool allowWrite)
    {
      return _mediaServerCore.UpdatePermissions(directory, restricted, allowWrite);
    }
  }
}

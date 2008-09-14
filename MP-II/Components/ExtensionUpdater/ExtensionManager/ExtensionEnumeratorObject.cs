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
using Components.ExtensionUpdater.ExtensionManager;

namespace Components.ExtensionUpdater.ExtensionManager
{
  public class ExtensionEnumeratorObject : ExtensionPackage
  {
    public ExtensionEnumeratorObject(ExtensionPackage package, ExtensionPackageState state) : base(package)
    {
      State = state;
      DownloadUrl = string.Empty;
      Downloads = 0;
      Date = DateTime.MinValue;
      Size = 0;
    }

    public ExtensionEnumeratorObject()
        : base()
    {
      State =ExtensionPackageState.Unknown;
      DownloadUrl = string.Empty;
      Downloads = 0;
      Date = DateTime.MinValue;
      Size = 0;
    }

    public ExtensionEnumeratorObject(ExtensionDependency dep)
    {
      State = ExtensionPackageState.Unknown;
      PackageId = string.Empty;
      ExtensionId = dep.ExtensionId;
      Name = string.Empty;
      FileName = string.Empty;
      Version = dep.Version;
      VersionType = string.Empty;
      Author = string.Empty;
      Description = string.Empty;
      DownloadUrl = string.Empty;
      Downloads = 0;
      Date = DateTime.MinValue;
      Size = 0;
    }

    public ExtensionEnumeratorObject(ExtensionEnumeratorObject obj)
        :base(obj)
    {
      State = obj.State;
      Description = obj.Description;
      DownloadUrl = obj.DownloadUrl;
      Downloads = obj.Downloads;
      Date = obj.Date;
      Size = obj.Size;
    }

    ExtensionPackageState _state = ExtensionPackageState.Unknown;
    public ExtensionPackageState State
    {
      get
      {
        return _state;
      }
      set
      {
        _state = value;
      }
    }

    string _downloadUrl;
    /// <summary>
    /// Gets or sets the download URL.
    /// </summary>
    /// <value>The download URL.</value>
    public string DownloadUrl
    {
      get
      {
        return _downloadUrl;
      }
      set
      {
        _downloadUrl = value;
      }
    }

    int _downloads;
    /// <summary>
    /// Gets or sets the number of downloads.
    /// </summary>
    /// <value>The downloads.</value>
    public int Downloads
    {
      get
      {
        return _downloads;
      }
      set
      {
        _downloads = value;
      }
    }

    DateTime _date;
    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    /// <value>The date.</value>
    public DateTime Date
    {
      get
      {
        return _date;
      }
      set
      {
        _date = value;
      }
    }

    int _size;
    /// <summary>
    /// Gets or sets the size of mpi file.
    /// </summary>
    /// <value>The size.</value>
    public int Size
    {
      get
      {
        return _size;
      }
      set
      {
        _size = value;
      }
    }

    public override string ToString()
    {
      return string.Format("{0}[{1}]", Name, ExtensionId);
    }
  }
}
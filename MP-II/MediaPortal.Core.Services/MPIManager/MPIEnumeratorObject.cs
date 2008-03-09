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
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.MPIManager;

namespace MediaPortal.Services.MPIManager
{
  public class MPIEnumeratorObject:MPIPackage
  {
    public MPIEnumeratorObject(MPIPackage package, MPIPackageState state)
      :base(package)
    {
      this.State = state;
      this.DownloadUrl = string.Empty;
      this.Downloads = 0;
      this.Date = DateTime.MinValue;
      this.Size = 0;
    }

    public MPIEnumeratorObject()
      : base()
    {
      this.State =MPIPackageState.Unknown;
      this.DownloadUrl = string.Empty;
      this.Downloads = 0;
      this.Date = DateTime.MinValue;
      this.Size = 0;
    }

    public MPIEnumeratorObject(MPIDependency dep)
    {
      this.State = MPIPackageState.Unknown;
      this.PackageId = string.Empty;
      this.ExtensionId = dep.ExtensionId;
      this.Name = string.Empty;
      this.FileName = string.Empty;
      this.Version = dep.Version;
      this.VersionType = string.Empty;
      this.Author = string.Empty;
      this.Description = string.Empty;
      this.DownloadUrl = string.Empty;
      this.Downloads = 0;
      this.Date = DateTime.MinValue;
      this.Size = 0;
    }

    public MPIEnumeratorObject(MPIEnumeratorObject obj)
      :base(obj)
    {
      this.State = obj.State;
      this.Description = obj.Description;
      this.DownloadUrl = obj.DownloadUrl;
      this.Downloads = obj.Downloads;
      this.Date = obj.Date;
      this.Size = obj.Size;
    }

    MPIPackageState _state = MPIPackageState.Unknown;
    public MPIPackageState State
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
      return string.Format("{0}[{1}]",this.Name,this.ExtensionId);
    }
  }
}

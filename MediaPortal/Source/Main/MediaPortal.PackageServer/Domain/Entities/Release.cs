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

using System;
using System.Collections.Generic;
using MediaPortal.PackageServer.Domain.Entities.Interfaces;

namespace MediaPortal.PackageServer.Domain.Entities
{
  public class Release : IEntity
  {
    public long ID { get; set; }
    public long PackageID { get; set; }

    public DateTime Released { get; set; }
    public bool IsAvailable { get; set; }

    public string Metadata { get; set; } // serialized plugin.xml descriptor
    public string Version { get; set; }
    public int ApiVersion { get; set; } // CurrentAPI in the plugin descriptor file

    public string PackageFileName { get; set; }
    public int PackageSize { get; set; }
    public int DownloadCount { get; set; }

    public virtual Package Package { get; set; }
    public virtual ICollection<Dependency> Dependencies { get; set; }

    public Release()
    {
      Dependencies = new List<Dependency>();
    }

    public Release(long packageID, DateTime released, string metadata, string version, int apiVersion, string packageFileName, int packageSize) : this()
    {
      PackageID = packageID;
      Released = released;
      IsAvailable = true;
      Metadata = metadata;
      Version = version;
      ApiVersion = apiVersion;
      PackageFileName = packageFileName;
      PackageSize = packageSize;
    }
  }
}
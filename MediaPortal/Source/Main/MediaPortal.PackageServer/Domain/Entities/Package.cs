#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using MediaPortal.Common.PluginManager.Packages.DataContracts.Enumerations;
using MediaPortal.PackageServer.Domain.Entities.Interfaces;

namespace MediaPortal.PackageServer.Domain.Entities
{
  public class Package : IEntity
  {
    public long ID { get; set; }
    public long OwnerUserID { get; set; }
    public long? CurrentReleaseID { get; set; }

    public Guid Guid { get; set; }
    public string Name { get; set; }
    public PackageType PackageType { get; set; }

    public string Authors { get; set; }
    public string License { get; set; }
    public string Description { get; set; }

    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }

    public virtual User Owner { get; set; }
    public virtual Release CurrentRelease { get; set; }

    public virtual ICollection<Tag> Tags { get; set; }
    public virtual ICollection<Release> Releases { get; set; }
    public virtual ICollection<Review> Reviews { get; set; }

    public Package()
    {
      Reviews = new List<Review>();
      Tags = new List<Tag>();
      Releases = new List<Release>();
    }

    public Package(long ownerUserID, Guid guid, string name, PackageType packageType, string authors, string license, string description) : this()
    {
      OwnerUserID = ownerUserID;
      Guid = guid;
      Name = name;
      PackageType = packageType;
      Authors = authors;
      License = license;
      Description = description;
    }
  }
}
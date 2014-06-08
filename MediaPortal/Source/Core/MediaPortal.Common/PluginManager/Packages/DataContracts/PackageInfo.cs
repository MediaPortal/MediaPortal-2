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
using System.Linq;
using MediaPortal.Common.PluginManager.Packages.DataContracts.Enumerations;

namespace MediaPortal.Common.PluginManager.Packages.DataContracts
{
  public class PackageInfo
  {
    public long ID { get; set; }

    public Guid Guid { get; set; }
    public PackageType PackageType { get; set; }

    public string Name { get; set; }
    public string Authors { get; set; }
    public string License { get; set; }
    public string Description { get; set; }

    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }

    public int TotalDownloadCount { get; set; }
    public int ReviewCount { get; set; }
    public int RatingCount { get; set; }
    public double AverateRating { get; set; }

    public ReleaseInfo CurrentRelease { get; set; }
    public ICollection<int> ApiVersionsAvailable { get; set; }

    public ICollection<string> CategoryTags { get; set; }
    public ICollection<long> Releases { get; set; }
    public ICollection<ReviewInfo> Reviews { get; set; }

    public PackageInfo()
    {
      ApiVersionsAvailable = new List<int>();
      CategoryTags = new List<string>();
      Releases = new List<long>();
      Reviews = new List<ReviewInfo>();
    }

    public PackageInfo(long id, Guid guid, PackageType packageType, string name, string authors, string license, string description,
      DateTime created, DateTime modified, int totalDownloadCount, int reviewCount, int ratingCount, double averateRating,
      ReleaseInfo currentRelease, IEnumerable<int> apiVersionsAvailable, IEnumerable<string> categoryTags, IEnumerable<long> releases, IEnumerable<ReviewInfo> reviews) : this()
    {
      ID = id;
      Guid = guid;
      PackageType = packageType;
      Name = name;
      Authors = authors;
      License = license;
      Description = description;
      Created = created;
      Modified = modified;
      TotalDownloadCount = totalDownloadCount;
      ReviewCount = reviewCount;
      RatingCount = ratingCount;
      AverateRating = averateRating;
      CurrentRelease = currentRelease;
      ApiVersionsAvailable = apiVersionsAvailable.ToList();
      CategoryTags = categoryTags.ToList();
      Releases = releases.ToList();
      Reviews = reviews.ToList();
    }
  }
}
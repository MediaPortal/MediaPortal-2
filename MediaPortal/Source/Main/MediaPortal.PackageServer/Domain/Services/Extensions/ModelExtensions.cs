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
using System.Linq;
using MediaPortal.Common.PluginManager.Packages.DataContracts;
using MediaPortal.PackageServer.Domain.Entities;
using MediaPortal.PackageServer.Domain.Entities.Enumerations;

namespace MediaPortal.PackageServer.Domain.Services.Extensions
{
  public static class ModelExtensions
  {
    public static PackageInfo ToPackageInfo(this Package package)
    {
      return new PackageInfo
      {
        ID = package.ID,
        Guid = package.Guid,
        PackageType = package.PackageType,
        Name = package.Name,
        Description = package.Description,
        Authors = package.Authors,
        License = package.License,
        Created = package.Created,
        Modified = package.Modified,
      };
    }

    public static PackageInfo ToPackageInfoFull(this Package package)
    {
      var info = package.ToPackageInfo();
      info.TotalDownloadCount = package.Releases.Sum(r => r.DownloadCount);
      info.ReviewCount = package.Reviews.Count(r => !string.IsNullOrEmpty(r.Body));
      info.RatingCount = package.Reviews.Count();
      info.AverageRating = package.Reviews.Any() ? Math.Round(package.Reviews.Average(r => r.Rating),1) : 0.0;
      info.CurrentRelease = package.CurrentRelease.ToReleaseInfo();
      info.CategoryTags = package.Tags.Where(t => t.Type == TagType.Category).Select(t => t.Name).ToList();
      info.Releases = package.Releases.Where(r => r.IsAvailable).OrderByDescending(r => r.Released)
        // TODO we don't currently have min-max system versions in the db.. returning dummy strings for now
        .Select(r => new ReleaseSummary(r.ID, r.Released, r.Version, r.ApiVersion, "2.0.0.0-alpha5", "2.0.0.0", 
                                        r.Dependencies.Select(d => d.ToDependencySummary()))).ToList();
      info.Reviews = package.Reviews.Where(x => x.Title != null).Select(x => x.ToReviewInfo()).ToList();
      return info;
    }

    public static DependencySummary ToDependencySummary(this Dependency dependency)
    {
      return new DependencySummary
      {
        IsCoreDependency = dependency.IsCoreComponentDependency,
        Name = dependency.IsCoreComponentDependency ? dependency.CoreComponent : dependency.Package.Name
      };
    }

    public static ReleaseSummary ToReleaseSummary(this Release release)
    {
      return new ReleaseSummary
      {
        ID = release.ID,
        Released = release.Released,
        Version = release.Version,
        ApiVersion = release.ApiVersion
      };
    }

    public static ReleaseInfo ToReleaseInfo(this Release release)
    {
      return new ReleaseInfo
      {
        ID = release.ID,
        Released = release.Released,
        Version = release.Version,
        ApiVersion = release.ApiVersion,
        PackageSize = release.PackageSize,
        DownloadUrl = string.Format("/uploads/{0}/{1}", release.Package.Name, release.PackageFileName),
        DownloadCount = release.DownloadCount
      };
    }

    public static ReviewInfo ToReviewInfo(this Review review)
    {
      return new ReviewInfo
      {
        Author = review.User.Name ?? "Anonymous Coward",
        Created = review.Created,
        Rating = review.Rating,
        LanguageCulture = review.LanguageCulture,
        Title = review.Title,
        Body = review.Body
      };
    }
  }
}

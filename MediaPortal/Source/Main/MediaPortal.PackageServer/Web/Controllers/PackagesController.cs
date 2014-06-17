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
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using MediaPortal.Common.PluginManager.Discovery;
using MediaPortal.Common.PluginManager.Packages.DataContracts;
using MediaPortal.Common.PluginManager.Packages.DataContracts.Packages;
using MediaPortal.PackageServer.Domain.Entities;
using MediaPortal.PackageServer.Domain.Entities.Enumerations;
using MediaPortal.PackageServer.Domain.Infrastructure.Context;
using MediaPortal.PackageServer.Domain.Services.Extensions;
using MediaPortal.PackageServer.Utility.Hooks;
using MediaPortal.PackageServer.Utility.Security;

namespace MediaPortal.PackageServer.Controllers
{
  [Authenticate]
  [AuthorizePartial]
  [RoutePrefix("packages")]
  public class PackagesController : BaseController
  {
    [AllowAnonymous]
    [Route("list")]
    public virtual ActionResult List(PackageListQuery model)
    {
      using (var db = new DataContext())
      {
        // filter available packages according to query
        var query = db.Packages.Include(e => e.CurrentRelease).Include(e => e.Tags).Include(e => e.Releases).Include(e => e.Reviews)
          .Where(p => p.PackageType.HasFlag(model.PackageType));
        if (model.PartialAuthor != null)
          query = query.Where(e => e.Authors.Contains(model.PartialAuthor));
        if (model.PartialPackageName != null)
          query = query.Where(e => e.Name.Contains(model.PartialPackageName));
        
        var packages = query.ToList();

        // filter by tags in memory as we don't trust EF to generate a good query for this
        if (model.CategoryTags != null && model.CategoryTags.Any())
        {
          var tags = model.CategoryTags.Select(t => t.ToLower());
          packages = packages.Where(e => e.Tags.Where(t => t.Type == TagType.Category).Select(t => t.Name).Intersect(tags).Any()).ToList();
        }

        // filter by compatibility with core components and other available plugins
        if (model.CoreComponents != null && model.CoreComponents.Any())
        {
          var repository = new PluginRepository(model.CoreComponents, packages.Select(e => e.CurrentRelease.Metadata.ParsePluginDefinition()));
          packages = packages.Where(p => repository.IsCompatible(repository.Models[p.Guid], ignoreConflicts: true)).ToList();
        }

        // transform domain entities into result models
        var result = packages.Select(p => p.ToPackageInfoFull()).ToList();
        return Json(result);
      }
    }

    [AllowAnonymous]
    [Route("{id:long:min(1)}/details")]
    public virtual ActionResult Details(long id)
    {
      using (var db = new DataContext())
      {
        var package = db.Packages.Include(e => e.CurrentRelease).Include(e => e.Tags).FirstOrDefault(p => p.ID == id);
        if (package == null)
          return new HttpStatusCodeResult(HttpStatusCode.NotFound, "No package with the given id could be found.");
        return Json(package.ToPackageInfoFull());
      }
    }

    [AllowAnonymous]
    [Route("find-release")]
    public virtual ActionResult FindRelease(PackageReleaseQuery model)
    {
      if (model == null || string.IsNullOrEmpty(model.PackageName) || string.IsNullOrEmpty(model.PackageVersion))
        return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed, "Invalid or missing parameters to complete request.");

      using (var db = new DataContext())
      {
        var result = db.Releases.Include(e => e.Package).FirstOrDefault(r => r.Version == model.PackageVersion && r.Package.Name == model.PackageName);
        if (result == null)
          return new HttpStatusCodeResult(HttpStatusCode.NotFound, "No package with the given name and version could be found.");
        return Json(result.ToReleaseInfo());
      }
    }

    [AllowAnonymous]
    [Route("{id:long:min(1)}/releases")]
    public virtual ActionResult Releases(long id)
    {
      using (var db = new DataContext())
      {
        var package = db.Packages.Include(p => p.Releases).FirstOrDefault(p => p.ID == id);
        if (package == null)
          return Json(new List<ReleaseInfo>());

        return Json(package.Releases.Select(r => r.ToReleaseInfo()));
      }
    }

    [AllowAnonymous]
    [Route("{id:long:min(1)}/reviews")]
    public virtual ActionResult Reviews(long id)
    {
      using (var db = new DataContext())
      {
        var reviews = db.Reviews.Include(r => r.User).Where(r => r.PackageID == id).ToList();
        return Json(reviews.Select(r => r.ToReviewInfo()));
      }
    }

    [Authorize(Roles = "User")]
    [Route("{id:long:min(1)}/reviews/add")]
    public virtual ActionResult AddReview(AddPackageReviewModel model)
    {
      var principal = User as Principal;
      if (principal == null || principal.UserID != model.UserID)
        return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Request must be submitted with matching user credentials.");

      using (var db = new DataContext())
      {
        var package = db.Packages.Include(p => p.Reviews).FirstOrDefault(p => p.ID == model.PackageID);
        if (package == null)
          return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed, "Package not found.");

        var existingReview = package.Reviews.FirstOrDefault(r => r.UserID == principal.UserID);
        if (existingReview != null)
        {
          existingReview.Rating = model.Review.Rating;
          existingReview.LanguageCulture = model.Review.LanguageCulture ?? existingReview.LanguageCulture;
          existingReview.Title = model.Review.Title ?? existingReview.Title;
          existingReview.Body = model.Review.Body ?? existingReview.Body;
        }
        else
        {
          package.Reviews.Add(new Review
          {
            PackageID = package.ID,
            UserID = principal.UserID,
            Created = DateTime.UtcNow,
            Rating = model.Review.Rating,
            LanguageCulture = model.Review.LanguageCulture,
            Title = model.Review.Title,
            Body = model.Review.Body
          });
        }
        db.SaveChanges();
        return new HttpStatusCodeResult(HttpStatusCode.Created);
      }
    }

    [Authorize(Roles = "User")]
    [Route("update-check")]
    public virtual ActionResult UpdateCheck(PackageUpdateQuery query)
    {
      if (query == null || (int)query.PackageType == 0
          || query.PluginVersions == null || query.PluginVersions.Count == 0
          || query.CoreComponents == null || query.CoreComponents.Count == 0)
        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Unable to check for updates (query is incomplete).");

      var principal = User as Principal;
      using (var db = new DataContext())
      {
        var installedReleases = (from r in db.Releases.Include(e => e.Package)
          where r.Version == query.PluginVersions[r.Package.Guid] && r.Package.PackageType.HasFlag(query.PackageType)
          select new { r.Package, Release = r, Metadata = r.Metadata.ParsePluginDefinition() }).ToList();
        //if( installedReleases.Count != query.PluginVersions.Count )
        // log.Warning( "User has unknown package release installed (assuming anything we have is newer)." );

        var packages = db.Packages.Include(e => e.CurrentRelease).Where(e => query.PluginVersions.ContainsKey(e.Guid)).ToList();
        //if( packages.Count != query.PluginVersions.Count )
        // log.Warning( "User has unknown package installed." );

        var updatedPackages = packages.Where(e => e.CurrentRelease.Version != query.PluginVersions[e.Guid]).ToList();

        var pluginRepository = new PluginRepository(query.CoreComponents, installedReleases.Select(i => i.Metadata));
        var compatibleUpdates = new List<Release>();
        foreach (var update in updatedPackages)
        {
          // check current release first
          var metadata = update.CurrentRelease.Metadata.ParsePluginDefinition();
          if (pluginRepository.IsCompatible(metadata))
          {
            compatibleUpdates.Add(update.CurrentRelease);
          }
          else // search older releases for newest compatible version
          {
            var releases = update.Releases.OrderByDescending(e => e.Released).Skip(1);
            foreach (var release in releases)
            {
              metadata = release.Metadata.ParsePluginDefinition();
              if (metadata.PluginVersion == query.PluginVersions[update.Guid])
                break; // user already has most recent compatible version
              if (pluginRepository.IsCompatible(metadata))
              {
                // found compatible release
                compatibleUpdates.Add(release);
                break;
              }
            }
          }
        }
        return Json(compatibleUpdates.Select(p => p.ToReleaseInfo()));
      }
    }
  }
}

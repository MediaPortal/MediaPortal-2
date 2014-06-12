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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using MediaPortal.Common.PluginManager.Discovery;
using MediaPortal.Common.PluginManager.Models;
using MediaPortal.Common.PluginManager.Packages.DataContracts.Authors;
using MediaPortal.Common.PluginManager.Packages.DataContracts.Enumerations;
using MediaPortal.PackageServer.Domain.Entities;
using MediaPortal.PackageServer.Domain.Entities.Enumerations;
using MediaPortal.PackageServer.Domain.Infrastructure.Context;
using MediaPortal.PackageServer.Utility.Security;

namespace MediaPortal.PackageServer.Controllers
{
  [Authenticate]
  [Authorize(Roles = "Author")]
  [RoutePrefix("package")]
  public class AuthorController : Controller
  {
    private const string UPLOAD_PATH = "/uploads";
    private const string WORK_PATH = "/work";

    [HttpPost]
    [Route("publish")]
    public virtual ActionResult Publish(PublishPackageModel model)
    {
      // verify model
      if (model == null
          || string.IsNullOrWhiteSpace(model.FileName)
          || string.IsNullOrWhiteSpace(model.Content))
        return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed, "Invalid or missing parameters to complete request.");

      // save package to work directory
      var rootPath = Server.MapPath(WORK_PATH);
      if (!Directory.Exists(rootPath))
        Directory.CreateDirectory(rootPath);
      var tempFilePath = Path.Combine(rootPath, Path.GetTempFileName());
      try
      {
        var packageBytes = Convert.FromBase64String(model.Content);
        System.IO.File.WriteAllBytes(tempFilePath, packageBytes);

        // extract plugin.xml descriptor file from package
        string descriptor;
        PluginMetadata metadata;
        try
        {
          using (var archive = ZipFile.Open(tempFilePath, ZipArchiveMode.Read))
          {
            // plugin.xml is stored inside subfolder (plugin name), get it via name without care of path.
            var entry = archive.Entries.First(e => e.Name == "plugin.xml");
            using (var stream = entry.Open())
            {
              using (var reader = new StreamReader(stream))
              {
                descriptor = reader.ReadToEnd();
                // verify descriptor file and extract metadata 
                metadata = descriptor.ParsePluginDefinition();
              }
            }
          }
        }
        catch (Exception)
        {
          return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "The uploaded package is not a valid MP2 package.");
        }

        if (metadata == null)
          return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "The plugin descriptor file in the uploaded package could not be parsed.");

        // verify that we can use plugin name as folder path
        if (Path.GetInvalidPathChars().Intersect(metadata.Name.ToCharArray()).Any())
          return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "The name of the plugin (in the descriptor file) has invalid characters and cannot be used.");

        using (var db = new DataContext())
        {
          var principal = User as Principal;
          // first see if package exists in the database
          var package = db.Packages.FirstOrDefault(p => p.Guid == metadata.PluginId);
          var packageExists = package != null;

          // create package
          if (!packageExists)
          {
            PackageType packageType;
            if (!Enum.TryParse(model.PackageType, true, out packageType))
              return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "The package type ('client' or 'server') must be specified for new packages.");

            package = new Package(principal.UserID, metadata.PluginId, metadata.Name, packageType, metadata.Author, metadata.Copyright, metadata.Description);

            // add package tags
            if (model.CategoryTags.Any())
            {
              var knownTags = db.Tags.Where(t => t.Type == TagType.Category || t.Type == TagType.Auto);
              foreach (var tagName in model.CategoryTags.Select(t => t.ToLowerInvariant()))
              {
                var tag = knownTags.FirstOrDefault(t => t.Name == tagName);
                if (tag == null)
                {
                  tag = new Tag(TagType.Auto, tagName.ToLowerInvariant());
                  db.Tags.Add(tag);
                }
                package.Tags.Add(tag);
              }
            }
            db.Packages.Add(package);
            db.SaveChanges();
          }

          // verify that package belongs to authenticated user
          if (package.OwnerUserID != principal.UserID)
            return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Only the owner of a package is allowed to publish new releases.");

          // verify that release does not already exist
          var release = db.Releases.FirstOrDefault(r => r.PackageFileName == model.FileName);
          if (release != null)
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "The uploaded package file is already associated with a release (did you forget to increase the version number?).");
          if (packageExists)
          {
            release = db.Releases.FirstOrDefault(r => r.PackageID == package.ID && r.Version == metadata.PluginVersion);
            if (release != null)
              return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "The package already has a release with the same version number.");
          }

          // add release
          release = new Release(package.ID, DateTime.UtcNow, descriptor, metadata.PluginVersion, metadata.DependencyInfo.CurrentApi, model.FileName, packageBytes.Length);
          db.Releases.Add(release);
          db.SaveChanges();

          // TODO add dependency information?

          // update current release for package
          package.CurrentRelease = release;
          db.SaveChanges();
        }

        // move file into target folder
        var uploadPath = Server.MapPath(UPLOAD_PATH);
        var targetPath = Path.Combine(uploadPath, metadata.Name);
        if (!Directory.Exists(targetPath))
          Directory.CreateDirectory(targetPath);
        System.IO.File.Move(tempFilePath, Path.Combine(targetPath, model.FileName));

        return new HttpStatusCodeResult(HttpStatusCode.Created);
      }
      finally
      {
        if (System.IO.File.Exists(tempFilePath))
          System.IO.File.Delete(tempFilePath);
      }
    }

    [HttpPost]
    [Route("recall")]
    public virtual ActionResult Recall(RecallPackageModel model)
    {
      // verify model
      if (model == null
          || (model.ReleaseID == null && string.IsNullOrWhiteSpace(model.PackageName))
          || (model.ReleaseID == null && string.IsNullOrWhiteSpace(model.PackageVersion)))
        return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed);

      // locate affected package and release
      using (var db = new DataContext())
      {
        var release = model.ReleaseID.HasValue
          ? db.Releases.FirstOrDefault(r => r.ID == model.ReleaseID.Value)
          : db.Releases.FirstOrDefault(r => r.Package.Name == model.PackageName && r.Version == model.PackageVersion);
        if (release == null)
          return new HttpStatusCodeResult(HttpStatusCode.NotFound, "No release could be found using the supplied data.");

        // recall release
        release.IsAvailable = false;

        var package = release.Package ?? db.Packages.Single(p => p.ID == release.PackageID);
        if (package.CurrentReleaseID == release.ID)
        {
          // mark older version as current
          var olderRelease = db.Releases.Where(r => r.PackageID == package.ID && r.IsAvailable && r.ID != release.ID).OrderByDescending(r => r.Released).FirstOrDefault();
          package.CurrentRelease = olderRelease;
        }
        db.SaveChanges();
      }
      return new HttpStatusCodeResult(HttpStatusCode.OK);
    }
  }
}
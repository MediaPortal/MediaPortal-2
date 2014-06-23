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
using System.Web.Mvc;
using MediaPortal.Common.PluginManager.Packages.DataContracts.Enumerations;
using MediaPortal.PackageServer.Domain.Entities.Enumerations;
using MediaPortal.PackageServer.Domain.Infrastructure.Context;
using MediaPortal.PackageServer.Models;
using MediaPortal.PackageServer.Utility.Hooks;
using MediaPortal.PackageServer.Utility.Security;

namespace MediaPortal.PackageServer.Controllers
{
  [AuthorizePartial]
  public class HomeController : BaseController
  {
    [AllowAnonymous]
    public virtual ActionResult Index()
    {
      ViewBag.Title = "MediaPortal 2 Package Server";
      using (var db = new DataContext())
      {
        ViewBag.PackageCount = db.Packages.Count();
        ViewBag.ReleaseCount = db.Releases.Count(x => x.IsAvailable);
        ViewBag.DownloadCount = db.Releases.Sum(x => x.DownloadCount);
      }
      return View();
    }

    [AllowAnonymous]
    [HttpGet]
    public virtual JsonResult FilterOptions()
    {
      using (var db = new DataContext())
      {
        var model = new PackageFilterOptionsModel
        {
          PackageTypes = Enum.GetNames(typeof(PackageType)).ToList(),
          Tags = db.Tags.Where(t => t.Type == TagType.Category).Select(t => t.Name).ToList(),
          // TODO we need a system versions repository.. probably needs to be in db.
          SystemVersions = new List<string> { "2.0.0.0-alpha5", "2.0.0.0-beta1", "2.0.0.0" }
        };
        return Json(model);
      }
    }
  }
}

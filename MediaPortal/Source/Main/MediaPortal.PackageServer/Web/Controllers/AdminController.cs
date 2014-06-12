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
using System.Net;
using System.Web.Mvc;
using MediaPortal.Common.General;
using MediaPortal.Common.PluginManager.Packages.DataContracts.UserAdmin;
using MediaPortal.PackageServer.Domain.Entities;
using MediaPortal.PackageServer.Domain.Entities.Enumerations;
using MediaPortal.PackageServer.Domain.Infrastructure.Context;
using MediaPortal.PackageServer.Utility.Security;

namespace MediaPortal.PackageServer.Controllers
{
  [Authenticate]
  [Authorize(Roles = "Admin")]
  [RoutePrefix("admin")]
  public class AdminController : Controller
  {
    [HttpPost]
    [Route("create-user")]
    public virtual ActionResult Create(CreateUserModel model)
    {
      // verify model
      if (model == null
          || string.IsNullOrWhiteSpace(model.Alias)
          || string.IsNullOrWhiteSpace(model.Password)
          || string.IsNullOrWhiteSpace(model.Name))
        return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed);

      using (var db = new DataContext())
      {
        // first make sure user with same alias does not exist
        var user = db.Users.FirstOrDefault(u => u.Alias == model.Alias);
        if (user != null)
        {
          var message = string.Format("A user with the alias '{0}' already exists.", model.Alias);
          return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed, message);
        }

        // create user
        user = new User
        {
          Alias = model.Alias,
          Name = model.Name,
          PasswordHash = model.Password.Hash(),
          AuthType = AuthType.Integral,
          Created = DateTime.UtcNow,
          Modified = DateTime.UtcNow,
          LastSeen = DateTime.UtcNow,
          Culture = model.Culture,
          Role = Role.Author,
          Status = AccountStatus.Created,
        };
        db.Users.Add(user);
        db.SaveChanges();
      }
      return new HttpStatusCodeResult(HttpStatusCode.Created);
    }

    [HttpPost]
    [Route("revoke-user")]
    public virtual ActionResult Revoke(RevokeUserModel model)
    {
      // verify model
      if (model == null
          || string.IsNullOrWhiteSpace(model.Alias)
          || string.IsNullOrWhiteSpace(model.Reason))
        return new HttpStatusCodeResult(HttpStatusCode.PreconditionFailed);

      using (var db = new DataContext())
      {
        var user = db.Users.FirstOrDefault(u => u.Alias == model.Alias && u.AuthType == AuthType.Integral);
        if (user == null)
        {
          var message = string.Format("Could not find a user with alias '{0}' to revoke.", model.Alias);
          return new HttpStatusCodeResult(HttpStatusCode.NotFound, message);
        }

        // revoke user
        user.Status = AccountStatus.Revoked;
        user.Modified = DateTime.UtcNow;
        user.Deleted = DateTime.UtcNow;
        user.RevokeReason = model.Reason;
        db.SaveChanges();
      }
      return new HttpStatusCodeResult(HttpStatusCode.OK);
    }
  }
}
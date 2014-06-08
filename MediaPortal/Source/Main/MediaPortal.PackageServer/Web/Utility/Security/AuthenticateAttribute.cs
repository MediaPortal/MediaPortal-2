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
using System.Net.Http.Headers;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Filters;
using MediaPortal.Common.General;
using MediaPortal.PackageServer.Domain.Entities;
using MediaPortal.PackageServer.Domain.Entities.Enumerations;
using MediaPortal.PackageServer.Domain.Infrastructure.Context;

namespace MediaPortal.PackageServer.Utility.Security
{
  public class AuthenticateAttribute : ActionFilterAttribute, IAuthenticationFilter
  {
    public void OnAuthentication(AuthenticationContext filterContext)
    {
      try
      {
        var authHeader = filterContext.RequestContext.HttpContext.Request.Headers["Authorization"];
        AuthenticationHeaderValue authHeaderValue;
        if (AuthenticationHeaderValue.TryParse(authHeader, out authHeaderValue))
        {
          switch (authHeaderValue.Scheme)
          {
            case "Basic":
              AuthenticateWithCredentials(filterContext, authHeaderValue.Parameter);
              break;
            case "Implicit":
              AuthenticateWithSystemIdentity(filterContext, authHeaderValue.Parameter);
              break;
          }
        }
      }
      catch // can't list them all
      {
      }
    }

    private static void AuthenticateWithCredentials(AuthenticationContext filterContext, string parameters)
    {
      var credentials = Encoding.ASCII.GetString(Convert.FromBase64String(parameters));
      var data = credentials.Split(':');
      var userName = data[0];
      var passwordHash = data[1].Hash();
      using (var db = new DataContext())
      {
        var user = db.Users.FirstOrDefault(u => u.Alias == userName && u.PasswordHash == passwordHash
                                                && !u.Deleted.HasValue && u.Status != AccountStatus.Revoked && u.AuthType == AuthType.Integral);
        if (user != null)
        {
          user.LastSeen = DateTime.UtcNow;
          db.SaveChanges();
          filterContext.Principal = new Principal(user.ID, user.Alias, user.Role);
        }
      }
    }

    private static void AuthenticateWithSystemIdentity(AuthenticationContext filterContext, string parameters)
    {
      var systemIdentity = new Guid(Encoding.ASCII.GetString(Convert.FromBase64String(parameters)));
      using (var db = new DataContext())
      {
        var user = db.Users.FirstOrDefault(u => u.AuthType == AuthType.MediaPortalSystemIdentity && u.SourceIdentity == systemIdentity
                                                && !u.Deleted.HasValue && u.Status != AccountStatus.Revoked && u.Role == Role.User);
        if (user != null)
        {
          user.LastSeen = DateTime.UtcNow;
          db.SaveChanges();
        }
        else
        {
          //var languageHeader = filterContext.RequestContext.HttpContext.Request.Headers[ "Accept-Language" ];
          //var culture = languageHeader == null ? null : languageHeader.Trim();          
          user = new User
          {
            Alias = systemIdentity.ToString(),
            Name = systemIdentity.ToString(),
            AuthType = AuthType.MediaPortalSystemIdentity,
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow,
            Culture = null,
            Role = Role.User,
            Status = AccountStatus.Active,
          };
          db.Users.Add(user);
          db.SaveChanges();
        }
        filterContext.Principal = new Principal(user.ID, user.Alias, user.Role);
      }
    }

    public void OnAuthenticationChallenge(AuthenticationChallengeContext filterContext)
    {
      //var user = filterContext.HttpContext.User;    
      //if (user == null || !user.Identity.IsAuthenticated)
      //{
      //  filterContext.Result = new HttpUnauthorizedResult();
      //}
    }
  }
}
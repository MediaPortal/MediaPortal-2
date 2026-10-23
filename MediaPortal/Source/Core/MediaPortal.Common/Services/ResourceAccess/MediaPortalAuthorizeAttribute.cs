#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
#if NET5_0_OR_GREATER
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
#else
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
#endif

namespace MediaPortal.Common.Services.ResourceAccess
{
#if NET5_0_OR_GREATER
  /// <summary>
  /// Requires a caller identified by <see cref="ResourceServer"/>'s bearer token, unless the server was told not
  /// to authorize at all.
  /// </summary>
  /// <remarks>
  /// <para>
  /// It is an <see cref="IAuthorizationFilter"/> and deliberately <em>not</em> an <see cref="AuthorizeAttribute"/>,
  /// which is what it was when this branch was written. Deriving from <c>AuthorizeAttribute</c> also makes the
  /// attribute an <c>IAuthorizeData</c>, and that adds the framework's own "the caller must be authenticated"
  /// policy to the endpoint on top of the rule below. The framework runs its policy in <c>UseAuthorization()</c>,
  /// before any MVC filter, so an anonymous caller is answered with <c>401</c> here and - the part that matters -
  /// is refused even when <see cref="ServerSettings.WebAutorizationEnabled"/> is off, because the framework has
  /// never heard of that setting. MP2's rule has always been "identify the caller unless the server was configured
  /// not to care", which is a rule the framework cannot express; applied twice, the stricter of the two wins and
  /// the setting stops meaning anything.
  /// </para>
  /// <para>
  /// The status code for a refusal is <c>403</c>, the one this branch already produced. A missing or bad token
  /// would be a <c>401</c> by the letter of RFC 6749; the difference decides nothing for MP2's clients, which ask
  /// the token endpoint again either way, and changing it now would be a change in the API's answers rather than
  /// part of making authorization work on this target.
  /// </para>
  /// </remarks>
  public class MediaPortalAuthorizeAttribute : Attribute, IAuthorizationFilter
  {
    private bool? _webAutorizationEnabled = null;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
      // A route that says it may be read without a name means it: the wake screen has to list the household
      // and show its pictures before anybody has said anything, and a picture of a person is not a secret
      // from the person whose television it is.
      foreach (object metadata in context.ActionDescriptor.EndpointMetadata)
        if (metadata is Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute)
          return;

      var authenticated = context.HttpContext.User?.Identity?.IsAuthenticated ?? false;

      if (!_webAutorizationEnabled.HasValue)
      {
        ServerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ServerSettings>();
        _webAutorizationEnabled = settings.WebAutorizationEnabled;
      }
      if (!_webAutorizationEnabled.Value)
        authenticated = true;
      else
        authenticated &= context.HttpContext.User?.Identity?.AuthenticationType == ResourceServer.MEDIAPORTAL_AUTHENTICATION_TYPE;

      if (!authenticated)
      {
        context.Result = new StatusCodeResult((int)System.Net.HttpStatusCode.Forbidden);
        return;
      }
    }
  }
#else
  public class MediaPortalAuthorizeAttribute : AuthorizeAttribute
  {
    private bool? _webAutorizationEnabled = null;

    protected override bool IsAuthorized(HttpActionContext actionContext)
    {
      var owinContext = actionContext.Request.GetOwinContext();
      var authenticated = owinContext.Authentication.User?.Identity?.IsAuthenticated ?? false;

      if (!_webAutorizationEnabled.HasValue)
      {
        ServerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ServerSettings>();
        _webAutorizationEnabled = settings.WebAutorizationEnabled;
      }
      if (!_webAutorizationEnabled.Value)
        authenticated = true;
      else
        authenticated &= owinContext.Authentication.User?.Identity?.AuthenticationType == ResourceServer.MEDIAPORTAL_AUTHENTICATION_TYPE;

      return authenticated;
    }
  }
#endif
}

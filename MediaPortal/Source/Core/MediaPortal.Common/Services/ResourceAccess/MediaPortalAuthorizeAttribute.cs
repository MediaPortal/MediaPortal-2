#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace MediaPortal.Common.Services.ResourceAccess
{
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
}

#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.OnlineVideos;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.OnlineVideos
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(WebBoolResult), Summary = "This function changes the value of a site property.")]
  [ApiFunctionParam(Name = "siteId", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "property", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "value", Type = typeof(string), Nullable = false)]
  internal class SetOnlineVideosSiteSetting
  {
    public Task<WebBoolResult> ProcessAsync(IOwinContext context, string siteId, string property, string value)
    {
      if (siteId == null)
        throw new BadRequestException("SetOnlineVideosSiteSetting: siteId is null");
      if (property == null)
        throw new BadRequestException("SetOnlineVideosSiteSetting: property is null");
      if (value == null)
        throw new BadRequestException("SetOnlineVideosSiteSetting: value is null");

      string siteName;
      OnlineVideosIdGenerator.DecodeSiteId(siteId, out siteName);

      return Task.FromResult(new WebBoolResult { Result = MP2Extended.OnlineVideosManager.SetSiteSetting(siteName, property, value) });
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}

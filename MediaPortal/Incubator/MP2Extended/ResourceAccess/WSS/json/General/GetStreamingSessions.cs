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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.WSS.General;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.General
{
  // Todo: Add the missing information
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  internal class GetStreamingSessions
  {
    public static Task<IList<WebStreamingSession>> ProcessAsync(IOwinContext context, string filter = null)
    {
      return Task.FromResult<IList<WebStreamingSession>>(StreamControl.GetStreamItems().Select(streamItem => new WebStreamingSession
      {
        ClientDescription = streamItem.Value.ClientDescription, 
        Profile = streamItem.Value.Profile.Name, 
        Identifier = streamItem.Key, 
        StartPosition = streamItem.Value.StartPosition,
        TranscodingInfo = new WebTranscodingInfo(streamItem.Value.StreamContext),
        StartTime = streamItem.Value.StartTime,
        SourceId = streamItem.Value.RequestedMediaItem.MediaItemId.ToString(),
        ClientIPAddress = streamItem.Value.ClientIp,
        DisplayName = streamItem.Value.Title,
      }).ToList());
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}

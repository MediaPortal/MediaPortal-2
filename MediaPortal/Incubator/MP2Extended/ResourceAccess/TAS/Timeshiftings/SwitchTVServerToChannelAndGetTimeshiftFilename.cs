﻿#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;
using MP2Extended.Extensions;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Timeshiftings
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "channelId", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "userName", Type = typeof(string), Nullable = false)]
  internal class SwitchTVServerToChannelAndGetTimeshiftFilename
  {
    public static async Task<WebStringResult> ProcessAsync(IOwinContext context, string userName, string channelId)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("SwitchTVServerToChannelAndGetTimeshiftFilename: ITvProvider not found");

      if (userName == null)
        throw new BadRequestException("SwitchTVServerToChannelAndGetTimeshiftFilename: userName is null");

      var item = await TVAccess.StartTimeshiftAsync(context, int.Parse(channelId), userName);
      if (item == null)
        throw new BadRequestException("SwitchTVServerToChannelAndGetTimeshiftFilename: Couldn't start timeshifting");

      string resourcePathStr = item.PrimaryProviderResourcePath();
      var resourcePath = ResourcePath.Deserialize(resourcePathStr);
      var stra = SlimTvResourceProvider.GetResourceAccessor(resourcePath.BasePathSegment.Path);
      string url = "";
      if (stra is ILocalFsResourceAccessor)
      {
        url = ((ILocalFsResourceAccessor)stra).LocalFileSystemPath;
      }
      else
      {
        await TVAccess.StopTimeshiftAsync(context, int.Parse(channelId), userName);
      }

      return new WebStringResult { Result = url };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}

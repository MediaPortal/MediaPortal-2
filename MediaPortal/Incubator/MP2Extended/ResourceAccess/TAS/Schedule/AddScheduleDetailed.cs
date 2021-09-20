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

using System;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Schedule
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "Adds a new Schedule with detailed infomration to TVE.")]
  [ApiFunctionParam(Name = "channelId", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "title", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "startTime", Type = typeof(DateTime), Nullable = false)]
  [ApiFunctionParam(Name = "endTime", Type = typeof(DateTime), Nullable = false)]
  [ApiFunctionParam(Name = "scheduleType", Type = typeof(WebScheduleType), Nullable = false)]
  [ApiFunctionParam(Name = "preRecordInterval", Type = typeof(int), Nullable = true)]
  [ApiFunctionParam(Name = "postRecordInterval", Type = typeof(int), Nullable = true)]
  [ApiFunctionParam(Name = "directory", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "priority", Type = typeof(int), Nullable = true)]
  internal class AddScheduleDetailed
  {
    public static async Task<WebBoolResult> ProcessAsync(IOwinContext context, string channelId, string title, DateTime startTime, DateTime endTime, WebScheduleType scheduleType, int preRecordInterval, int postRecordInterval, string directory, int priority)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("AddScheduleDetailed: ITvProvider not found");

      bool result = await TVAccess.CreateScheduleAsync(context, int.Parse(channelId), title, startTime, endTime, scheduleType, preRecordInterval, postRecordInterval, directory, priority);
      return new WebBoolResult { Result = result };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}

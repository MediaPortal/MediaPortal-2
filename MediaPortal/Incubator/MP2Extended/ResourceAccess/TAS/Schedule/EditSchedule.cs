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
using System.Collections.Generic;
using System.Linq;
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
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "Enables you to edit a already existend schedule.")]
  [ApiFunctionParam(Name = "scheduleId", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "channelId", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "title", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "startTime", Type = typeof(DateTime), Nullable = true)]
  [ApiFunctionParam(Name = "endTime", Type = typeof(DateTime), Nullable = true)]
  [ApiFunctionParam(Name = "scheduleType", Type = typeof(WebScheduleType), Nullable = true)]
  [ApiFunctionParam(Name = "preRecordInterval", Type = typeof(int), Nullable = true)]
  [ApiFunctionParam(Name = "postRecordInterval", Type = typeof(int), Nullable = true)]
  [ApiFunctionParam(Name = "directory", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "priority", Type = typeof(int), Nullable = true)]
  internal class EditSchedule
  {
    public static async Task<WebBoolResult> ProcessAsync(IOwinContext context, string scheduleId, string channelId = null, string title = null, DateTime? startTime = null, DateTime? endTime = null, WebScheduleType? scheduleType = null, int? preRecordInterval = null, int? postRecordInterval = null, string directory = null, int? priority = null)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("EditSchedule: ITvProvider not found");

      bool result = await TVAccess.EditScheduleAsync(context, int.Parse(scheduleId),
        channelId != null ? int.Parse(channelId) : (int?)null,
        title,
        startTime,
        endTime,
        scheduleType,
        preRecordInterval,
        postRecordInterval,
        directory,
        priority);

      return new WebBoolResult { Result = result };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}

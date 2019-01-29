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
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Misc
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(WebStringResult), Summary = "This function is not really supported in MP2Ext.\r\nOnly 'preRecordInterval' and 'postRecordInterval' are supported by MP2Ext settings. These are !not! read from the TVE DB.")]
  [ApiFunctionParam(Name = "tagName", Type = typeof(string), Nullable = false)]
  internal class ReadSettingFromDatabase
  {
    public static Task<WebStringResult> ProcessAsync(IOwinContext context, string tagName)
    {
      if (tagName == null)
        throw new BadRequestException("ReadSettingFromDatabase: tagName is null");

      string output = "0";
      switch (tagName)
      {
        case "preRecordInterval":
          output = MP2Extended.Settings.PreRecordInterval.ToString();
          break;
        case "postRecordInterval":
          output = MP2Extended.Settings.PostRecordInterval.ToString();
          break;
      }

      return Task.FromResult(new WebStringResult { Result = output });
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}

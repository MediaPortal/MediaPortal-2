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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using System.Threading.Tasks;
using Microsoft.Owin;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Common.Settings;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Misc
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(WebStringResult), Summary = "This function is not really supported in MP2Ext.\r\nOnly 'preRecordInterval' and 'postRecordInterval' are supported by MP2Ext settings. These are !not! written to the TVE DB.")]
  [ApiFunctionParam(Name = "tagName", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "value", Type = typeof(string), Nullable = false)]
  internal class WriteSettingToDatabase
  {
    public static Task<WebBoolResult> ProcessAsync(IOwinContext context, string tagName, string value)
    {
      if (tagName == null)
        throw new BadRequestException("WriteSettingToDatabase: tagName is null");
      if (value == null)
        throw new BadRequestException("WriteSettingToDatabase: value is null");

      bool saveSettings = false;
      if (tagName == "preRecordInterval" && int.TryParse(value, out int preRecInt))
      {
        MP2Extended.Settings.PreRecordInterval = preRecInt;
        saveSettings = true;
      }
      else if (tagName == "postRecordInterval" && int.TryParse(value, out int postRecInt))
      {
        MP2Extended.Settings.PostRecordInterval = postRecInt;
        saveSettings = true;
      }

      if (saveSettings)
      {
        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
        settingsManager.Save(MP2Extended.Settings);
      }

      return Task.FromResult(new WebBoolResult { Result = saveSettings });
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}

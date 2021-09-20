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

using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Utils;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.DAS.json.Settings
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(WebBoolResult), Summary = "Allowes to change a MP2Ext setting.")]
  [ApiFunctionParam(Name = "name", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "value", Type = typeof(string), Nullable = false)]
  internal static class ChangeSetting
  {
    public static Task<WebBoolResult> ProcessAsync(IOwinContext context, string name, string value)
    {
      // Security
      if (!(context.Authentication.User?.Identity?.IsAuthenticated ?? false))
        return Task.FromResult(new WebBoolResult { Result = false });
      if (!(context.Authentication.User?.IsInRole("Admin") ?? false))
        return Task.FromResult(new WebBoolResult { Result = false });

      if (string.IsNullOrEmpty(name))
        throw new BadRequestException("ChangeSetting: name is null or empty");
      if (value == null)
        throw new BadRequestException("ChangeSetting: value is null");

      var properties = MP2Extended.Settings.GetType().GetProperties().ToList();
      int index = properties.FindIndex(x => x.Name == name);
      if (index == -1)
      {
        Logger.Warn("ChangeSetting: A setting with the name '{0}' wasn't found!", name);
        return Task.FromResult(new WebBoolResult { Result = false });
      }

      var property = properties[index];
      object convertedValue;

      bool result = value.TryParse(property.PropertyType, out convertedValue);
      if (result)
        property.SetValue(MP2Extended.Settings, convertedValue);

      return Task.FromResult(new WebBoolResult { Result = result });
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}

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
using MediaPortal.Plugins.MP2Extended.Common;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Control
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(WebBoolResult), Summary = "")]
  [ApiFunctionParam(Name = "identifier", Type = typeof(string), Nullable = false)]
  internal class StopStream
  {
    public static async Task<WebBoolResult> ProcessAsync(IOwinContext context, string identifier)
    {
      bool result = true;

      if (identifier == null)
      {
        Logger.Debug("StopStream: Identifier is null");
        result = false;
      }

      if (!(await StreamControl.StopStreamingAsync(identifier)))
      {
        Logger.Debug("StopStream: Unknown identifier: {0}", identifier);
        result = false;
      }

     return new WebBoolResult { Result = result };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}

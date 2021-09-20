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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Filter
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<string>),
    Summary = "Get all available values for a given field")]
  [ApiFunctionParam(Name = "mediaType", Type = typeof(WebMediaType), Nullable = false)]
  [ApiFunctionParam(Name = "filterField", Type = typeof(string), Nullable = false)]
  //[ApiFunctionParam(Name = "provider", Type = typeof(int), Nullable = true)]
  [ApiFunctionParam(Name = "op", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "limit", Type = typeof(int), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  [ApiFunctionParam(Name = "start", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "end", Type = typeof(int), Nullable = false)]
  internal class GetFilterValuesByRange
  {
    public static async Task<IList<string>> ProcessAsync(IOwinContext context, int start, int end, WebMediaType mediaType, string filterField, string op, int? limit, WebSortOrder? order)
    {
     IList<string> output = await GetFilterValues.ProcessAsync(context, mediaType, filterField, op, limit, order);

      // Get Range
      output = output.TakeRange(start, end).ToList();
      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}

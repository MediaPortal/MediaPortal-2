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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.Movie;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie.BaseClasses;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS;
using Microsoft.Owin;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  internal class GetMoviesDetailed : BaseMovieDetailed
  {
    public static Task<IList<WebMovieDetailed>> ProcessAsync(IOwinContext context, string filter, WebSortField? sort, WebSortOrder? order)
    {
      IList<MediaItem> items = MediaLibraryAccess.GetMediaItemsByAspect(context, BasicNecessaryMIATypeIds, BasicOptionalMIATypeIds, null);
      if (items.Count == 0)
        throw new BadRequestException("No Movies found");

      var output = items.Select(item => MovieDetailed(item))
        .Filter(filter);

      // sort and filter
      if (sort != null && order != null)
        output = output.Filter(filter).SortWebMovieDetailed(sort, order);

      return System.Threading.Tasks.Task.FromResult<IList<WebMovieDetailed>>(output.ToList());
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}

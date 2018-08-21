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
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.OnlineVideos;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.OnlineVideos
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<WebOnlineVideosSite>), Summary = "This function returns a list of all available Sites on the OnlineVideos Server.")]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  internal class GetOnlineVideosGlobalSites
  {
    public Task<IList<WebOnlineVideosGlobalSite>> ProcessAsync(IOwinContext context, string filter, WebSortField? sort, WebSortOrder? order)
    {
      List<WebOnlineVideosGlobalSite> output = MP2Extended.OnlineVideosManager.GetGlobalSites().Select(site => new WebOnlineVideosGlobalSite
      {
        Id = OnlineVideosIdGenerator.BuildSiteId(site.Site.Name),
        Title = site.Site.Name,
        Description = site.Site.Description,
        Creator = site.Site.Owner_FK.Substring(0, site.Site.Owner_FK.IndexOf('@')).Replace('.', ' ').Replace('_', ' '),
        Language = site.Site.Language,
        IsAdult = site.Site.IsAdult,
        State = (WebOnlineVideosSiteState)site.Site.State,
        ReportCount = site.Site.ReportCount,
        LastUpdated = site.Site.LastUpdated,
        Added = site.Added
      }).ToList();

      // sort and filter
      if (sort != null && order != null)
      {
        output = output.AsQueryable().Filter(filter).SortMediaItemList(sort, order).ToList();
      }
      else
        output = output.Filter(filter).ToList();
      return System.Threading.Tasks.Task.FromResult<IList<WebOnlineVideosGlobalSite>>(output);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  internal class GetMusicArtistsBasic
  {
    public Task<IList<WebMusicArtistBasic>> ProcessAsync(IOwinContext context, string filter, WebSortField? sort, WebSortOrder? order)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);

      HomogenousMap items = MediaLibraryAccess.GetGroups(context, necessaryMIATypes, AudioAspect.ATTR_ARTISTS);
      HomogenousMap itemsAlbum = MediaLibraryAccess.GetGroups(context, necessaryMIATypes, AudioAspect.ATTR_ALBUMARTISTS);

      List<WebMusicArtistBasic> output = new List<WebMusicArtistBasic>();

      if (items.Count == 0)
        return System.Threading.Tasks.Task.FromResult<IList<WebMusicArtistBasic>>(output);

      output = (from item in items
        where item.Key is string
        select new WebMusicArtistBasic
        {
          Id = Convert.ToBase64String((new UTF8Encoding().GetBytes(item.Key.ToString()))), Title = item.Key.ToString(), PID = 0, HasAlbums = itemsAlbum.ContainsKey(item.Key)
        }).ToList();

      
      // sort and filter
      if (sort != null && order != null)
      {
       output = output.AsQueryable().Filter(filter).SortMediaItemList(sort, order).ToList();
      }
      else
        output = output.AsQueryable().Filter(filter).ToList();


      return System.Threading.Tasks.Task.FromResult<IList<WebMusicArtistBasic>>(output);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}

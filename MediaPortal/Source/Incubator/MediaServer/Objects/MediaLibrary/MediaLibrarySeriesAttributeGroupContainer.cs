#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MediaServer.Objects.Basic;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Plugins.Transcoding.Interfaces.Aspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  internal class MediaLibrarySeriesAttributeGroupContainer : BasicContainer
  {
    private readonly MediaItemAspectMetadata.SingleAttributeSpecification _attributeGroup;

    public MediaLibrarySeriesAttributeGroupContainer(string id, string title, MediaItemAspectMetadata.SingleAttributeSpecification attributeGroup, EndPointSettings client)
      : base(id, client)
    {
      Title = title;
      _attributeGroup = attributeGroup;
    }

    public IList<MediaItem> GetItems()
    {
      List<Guid> necessaryMias = new List<Guid>(NECESSARY_EPISODE_MIA_TYPE_IDS);
      if (necessaryMias.Contains(EpisodeAspect.ASPECT_ID)) necessaryMias.Remove(EpisodeAspect.ASPECT_ID); //Group MIA cannot be present
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
      HomogenousMap seriesItems = library.GetValueGroups(EpisodeAspect.ATTR_SERIESNAME, null, ProjectionFunction.None, necessaryMias.ToArray(), 
        new RelationalFilter(_attributeGroup, RelationalOperator.EQ, Title), true);

      List<string> seriesNames = new List<string>();
      foreach (object o in seriesItems.Keys)
      {
        if (o is string)
        {
          seriesNames.Add(o.ToString());
        }
      }

      return library.Search(new MediaItemQuery(NECESSARY_SERIES_MIA_TYPE_IDS, null, 
        new InFilter(SeriesAspect.ATTR_SERIESNAME, seriesNames)), true);
    }

    public override void Initialise()
    {
      IList<MediaItem> items = GetItems();

      foreach (MediaItem item in items)
      {
        Add(new MediaLibrarySeriesItem(item, null, Client));
      }
    }
  }
}

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
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MediaServer.Objects.Basic;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.MediaManagement;
using System;
using System.Linq;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  internal class MediaLibraryPersonContainer : BasicContainer
  {
    protected Guid _role;
    protected Guid _linkedRole;
    protected Guid[] _necessaryMIAs;

    public MediaLibraryPersonContainer(string id, Guid role, Guid linkedRole, Guid[] necessaryMIAs, EndPointSettings client)
      : base(id, client)
    {
      _role = role;
      _linkedRole = linkedRole;
      _necessaryMIAs = necessaryMIAs;
    }

    private IList<MediaItem> GetItems()
    {
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
      IFilter filter = new FilteredRelationshipFilter(_role, _linkedRole, null);
      MediaItemQuery query = new MediaItemQuery(NECESSARY_PERSON_MIA_TYPE_IDS, filter)
      {
         SortInformation = new List<SortInformation> { new SortInformation(PersonAspect.ATTR_PERSON_NAME, SortDirection.Ascending) }
      };
      return library.Search(query, true, UserId, false);
    }

    public override void Initialise()
    {
      foreach (MediaItem item in GetItems())
      {
        string title = "?";
        if (MediaItemAspect.TryGetAspect(item.Aspects, PersonAspect.Metadata, out SingleMediaItemAspect personAspect))
        {
          title = personAspect.GetAttributeValue<string>(PersonAspect.ATTR_PERSON_NAME);
        }
        string key = Id + ":" + item.MediaItemId;

        if(_necessaryMIAs.Contains(MovieAspect.ASPECT_ID))
          Add(new MediaLibraryMovieActorItem(key, title, new RelationshipFilter(_linkedRole, _role, item.MediaItemId), Client));
        else if (_necessaryMIAs.Contains(AudioAspect.ASPECT_ID))
          Add(new MediaLibraryMusicArtistItem(key, title, new RelationshipFilter(_linkedRole, _role, item.MediaItemId), Client));
        else if (_necessaryMIAs.Contains(AudioAlbumAspect.ASPECT_ID))
          Add(new MediaLibraryAlbumArtistItem(key, title, new RelationshipFilter(_linkedRole, _role, item.MediaItemId), Client));
      }
    }
  }
}

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

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  internal class MediaLibraryMovieActorsContainer : BasicContainer
  {
    public MediaLibraryMovieActorsContainer(string id, EndPointSettings client)
      : base(id, client)
    {
    }

    public HomogenousMap GetItems()
    {
      List<Guid> necessaryMias = new List<Guid>(NECESSARY_MOVIE_MIA_TYPE_IDS);
      if (necessaryMias.Contains(VideoAspect.ASPECT_ID)) necessaryMias.Remove(VideoAspect.ASPECT_ID); //Group MIA cannot be present
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
      return library.GetValueGroups(VideoAspect.ATTR_ACTORS, null, ProjectionFunction.None, necessaryMias.ToArray(), null, true);
    }

    public override void Initialise()
    {
      HomogenousMap items = GetItems();

      foreach (KeyValuePair<object, object> item in items)
      {
        if (item.Key == null) continue;
        string title = item.Key.ToString();
        string key = Id + ":" + title;

        Add(new MediaLibraryMovieActorItem(key, title, Client));
      }
    }
  }
}

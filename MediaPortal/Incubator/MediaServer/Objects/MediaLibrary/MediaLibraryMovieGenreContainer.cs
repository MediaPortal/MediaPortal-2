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
using MediaPortal.Plugins.Transcoding.Aspects;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  internal class MediaLibraryMovieGenreContainer : BasicContainer
  {
    private static readonly Guid[] NECESSARY_MIA_TYPE_IDS = {
      MediaAspect.ASPECT_ID,
      MovieAspect.ASPECT_ID,
      TranscodeItemVideoAspect.ASPECT_ID
    };

    public MediaLibraryMovieGenreContainer(string id, EndPointSettings client)
      : base(id, client)
    {
    }

    public HomogenousMap GetItems()
    {
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
      return library.GetValueGroups(VideoAspect.ATTR_GENRES, null, ProjectionFunction.None, NECESSARY_MIA_TYPE_IDS, null, true);
    }

    public override void Initialise()
    {
	  base.Initialise();
	  
      HomogenousMap items = GetItems();

      foreach (KeyValuePair<object, object> item in items)
      {
        string title = (string)item.Key ?? "<Unknown>";
        string key = Id + ":" + title;

        _children.Add(key, new MediaLibraryMovieGenreItem(key, title, Client));
      }
    }
  }
}

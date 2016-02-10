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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Plugins.Transcoding.Aspects;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  class MediaLibraryMovieGenreItem : MediaLibraryContainer, IDirectoryMovieGenre
  {
    private static readonly Guid[] NECESSARY_MIA_TYPE_IDS = {
      MediaAspect.ASPECT_ID,
      VideoAspect.ASPECT_ID,
      MovieAspect.ASPECT_ID,
      TranscodeItemVideoAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID
    };

    public MediaLibraryMovieGenreItem(string id, string title, EndPointSettings client)
      : base(id, title, NECESSARY_MIA_TYPE_IDS, null, new RelationalFilter(VideoAspect.ATTR_GENRES, RelationalOperator.EQ, title), client)
    {
      ServiceRegistration.Get<ILogger>().Debug("Created movie genre {0}={1}", id, title);
    }

    public override string Class
    {
      get { return "object.container.genre.movieGenre"; }
    }

    public string LongDescription{ get; set; }
    public string Description{ get; set; }
  }
}

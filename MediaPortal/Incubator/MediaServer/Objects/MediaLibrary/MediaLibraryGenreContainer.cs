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
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.MediaServer.Objects.Basic;
using MediaPortal.Extensions.MediaServer.Profiles;
using MediaPortal.Common.MediaManagement.MLQueries;
using System.Linq;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  internal class MediaLibraryGenreContainer : BasicContainer
  {
    protected Guid[] _necessaryMIAs;

    public MediaLibraryGenreContainer(string id, Guid[] necessaryMIAs, EndPointSettings client)
      : base(id, client)
    {
      _necessaryMIAs = necessaryMIAs;
    }

    public HomogenousMap GetItems()
    {
      List<Guid> necessaryMias = new List<Guid>(_necessaryMIAs);
      if (necessaryMias.Contains(GenreAspect.ASPECT_ID)) necessaryMias.Remove(GenreAspect.ASPECT_ID); //Group MIA cannot be present
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
      return library.GetValueGroups(GenreAspect.ATTR_GENRE, null, ProjectionFunction.None, necessaryMias, AppendUserFilter(null, necessaryMias), true, false);
    }

    public override void Initialise()
    {
      HomogenousMap items = GetItems();

      foreach (var item in items)
      {
        try
        {
          if (item.Key == null) continue;
          string title = item.Key.ToString();
          string key = Id + ":" + title;

          if (_necessaryMIAs.Contains(MovieAspect.ASPECT_ID))
            Add(new MediaLibraryMovieGenreItem(key, title, new RelationalFilter(GenreAspect.ATTR_GENRE, RelationalOperator.EQ, title), Client));
          else if (_necessaryMIAs.Contains(AudioAspect.ASPECT_ID))
            Add(new MediaLibraryMusicGenreItem(key, title, new RelationalFilter(GenreAspect.ATTR_GENRE, RelationalOperator.EQ, title), Client));
          else if (_necessaryMIAs.Contains(AudioAlbumAspect.ASPECT_ID))
            Add(new MediaLibraryAlbumGenreItem(key, title, new RelationalFilter(GenreAspect.ATTR_GENRE, RelationalOperator.EQ, title), Client));
          else if (_necessaryMIAs.Contains(SeriesAspect.ASPECT_ID))
            Add(new MediaLibrarySeriesGenreItem(key, title, new RelationalFilter(GenreAspect.ATTR_GENRE, RelationalOperator.EQ, title), Client));
        }
        catch (Exception ex)
        {
          Logger.Error("Item '{0}' could not be added", ex, item.Key);
        }
      }
    }
  }
}

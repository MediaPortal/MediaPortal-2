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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Settings;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  /// <summary>
  /// Holds a GUI item which represents a movie filter choice.
  /// </summary>
  public class MovieFilterItem : PlayableContainerMediaItem
  {
    public override void Update(MediaItem mediaItem)
    {
      base.Update(mediaItem);

      MovieCollectionInfo movieCollection = new MovieCollectionInfo();
      if (!movieCollection.FromMetadata(mediaItem.Aspects))
        return;

      CollectionName = movieCollection.CollectionName.Text ?? "";

      int? count;
      if (mediaItem.Aspects.ContainsKey(MovieCollectionAspect.ASPECT_ID))
      {
        if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MovieCollectionAspect.ATTR_AVAILABLE_MOVIES, out count))
          AvailableMovies = count.Value.ToString();
        else
          AvailableMovies = "";

        if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MovieCollectionAspect.ATTR_NUM_MOVIES, out count))
          TotalMovies = count.Value.ToString();
        else
          TotalMovies = "";

        if (ShowVirtualSetting.ShowVirtualMovieMedia)
          Movies = TotalMovies;
        else
          Movies = AvailableMovies;
      }

      FireChange();
    }

    public string CollectionName
    {
      get { return this[Consts.KEY_MOVIE_COLLECTION]; }
      set { SetLabel(Consts.KEY_MOVIE_COLLECTION, value); }
    }

    public string AvailableMovies
    {
      get { return this[Consts.KEY_AVAIL_MOVIES]; }
      set { SetLabel(Consts.KEY_AVAIL_MOVIES, value); }
    }

    public string TotalMovies
    {
      get { return this[Consts.KEY_TOTAL_MOVIES]; }
      set { SetLabel(Consts.KEY_TOTAL_MOVIES, value); }
    }

    public string Movies
    {
      get { return this[Consts.KEY_NUM_MOVIES]; }
      set { SetLabel(Consts.KEY_NUM_MOVIES, value); }
    }
  }
}

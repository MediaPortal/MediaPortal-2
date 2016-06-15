#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.Models.Navigation;

namespace MediaPortal.Extensions.UserServices.FanArtService.Client.ImageSourceProvider
{
  public class FanartImageSourceProvider : IFanartImageSourceProvider
  {
    bool IFanartImageSourceProvider.TryCreateFanartImageSource(ListItem listItem, out FanArtImageSource fanartImageSource)
    {
      SeriesFilterItem series = listItem as SeriesFilterItem;
      if (series != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Series,
          FanArtName = series.SimpleTitle
        };
        return true;
      }
      SeriesItem episode = listItem as SeriesItem;
      if (episode != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Series,
          FanArtName = episode.Series
        };
        return true;
      }
      MovieFilterItem movieCollection = listItem as MovieFilterItem;
      if (movieCollection != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.MovieCollection,
          FanArtName = movieCollection.SimpleTitle
        };
        return true;
      }
      MovieItem movie = listItem as MovieItem;
      if (movie != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Movie,
          // Fanart loading now depends on the MediaItemId to support local fanart
          FanArtName = movie.MediaItem.MediaItemId.ToString()
        };
        return true;
      }
      VideoItem video = listItem as VideoItem;
      if (video != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Movie,
          // Fanart loading now depends on the MediaItemId to support local fanart
          FanArtName = video.MediaItem.MediaItemId.ToString()
        };
        return true;
      }
      fanartImageSource = null;
      return false;
    }
  }
}
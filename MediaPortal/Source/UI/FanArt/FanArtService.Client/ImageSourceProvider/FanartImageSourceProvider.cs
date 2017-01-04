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

using MediaPortal.Common.FanArt;
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
          FanArtName = series.MediaItem.MediaItemId.ToString()
        };
        return true;
      }
      SeasonFilterItem season = listItem as SeasonFilterItem;
      if (season != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.SeriesSeason,
          FanArtName = season.MediaItem.MediaItemId.ToString()
        };
        return true;
      }
      EpisodeItem episode = listItem as EpisodeItem;
      if (episode != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Episode,
          FanArtName = episode.MediaItem.MediaItemId.ToString()
        };
        return true;
      }
      MovieFilterItem movieCollection = listItem as MovieFilterItem;
      if (movieCollection != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.MovieCollection,
          FanArtName = movieCollection.MediaItem.MediaItemId.ToString()
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
      AlbumFilterItem albumItem = listItem as AlbumFilterItem;
      if (albumItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Album,
          // Fanart loading now depends on the MediaItemId to support local fanart
          FanArtName = albumItem.MediaItem.MediaItemId.ToString()
        };
        return true;
      }
      AudioItem audioItem = listItem as AudioItem;
      if (audioItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Audio,
          // Fanart loading now depends on the MediaItemId to support local fanart
          FanArtName = audioItem.MediaItem.MediaItemId.ToString()
        };
        return true;
      }
      ActorFilterItem actorItem = listItem as ActorFilterItem;
      if (actorItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Actor,
          // Fanart loading now depends on the MediaItemId to support local fanart
          FanArtName = actorItem.MediaItem.MediaItemId.ToString()
        };
        return true;
      }
      DirectorFilterItem directorItem = listItem as DirectorFilterItem;
      if (directorItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Director,
          // Fanart loading now depends on the MediaItemId to support local fanart
          FanArtName = directorItem.MediaItem.MediaItemId.ToString()
        };
        return true;
      }
      WriterFilterItem writerItem = listItem as WriterFilterItem;
      if (writerItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Writer,
          // Fanart loading now depends on the MediaItemId to support local fanart
          FanArtName = writerItem.MediaItem.MediaItemId.ToString()
        };
        return true;
      }
      ArtistFilterItem artisitItem = listItem as ArtistFilterItem;
      if (artisitItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Artist,
          // Fanart loading now depends on the MediaItemId to support local fanart
          FanArtName = artisitItem.MediaItem.MediaItemId.ToString()
        };
        return true;
      }
      ComposerFilterItem composerItem = listItem as ComposerFilterItem;
      if (composerItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Composer,
          // Fanart loading now depends on the MediaItemId to support local fanart
          FanArtName = composerItem.MediaItem.MediaItemId.ToString()
        };
        return true;
      }
      CharacterFilterItem characterItem = listItem as CharacterFilterItem;
      if (characterItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Character,
          // Fanart loading now depends on the MediaItemId to support local fanart
          FanArtName = characterItem.MediaItem.MediaItemId.ToString()
        };
        return true;
      }
      CompanyFilterItem companyItem = listItem as CompanyFilterItem;
      if (companyItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Company,
          // Fanart loading now depends on the MediaItemId to support local fanart
          FanArtName = companyItem.MediaItem.MediaItemId.ToString()
        };
        return true;
      }
      TVNetworkFilterItem tvNetworkItem = listItem as TVNetworkFilterItem;
      if (tvNetworkItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.TVNetwork,
          // Fanart loading now depends on the MediaItemId to support local fanart
          FanArtName = tvNetworkItem.MediaItem.MediaItemId.ToString()
        };
        return true;
      }
      ImageItem imgItem = listItem as ImageItem;
      if (imgItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Image,
          // Fanart loading now depends on the MediaItemId to support local fanart
          FanArtName = imgItem.MediaItem.MediaItemId.ToString()
        };
        return true;
      }
      fanartImageSource = null;
      return false;
    }
  }
}

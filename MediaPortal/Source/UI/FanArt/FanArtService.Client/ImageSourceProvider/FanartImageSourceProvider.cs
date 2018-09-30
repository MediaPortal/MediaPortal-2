#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
    protected static string GetFanArtName(ListItem listItem)
    {
      PlayableMediaItem playableMediaItem = listItem as PlayableMediaItem;
      if (playableMediaItem != null)
      {
        if (playableMediaItem.MediaItem != null)
          // Fanart loading now depends on the MediaItemId to support local fanart
          return playableMediaItem.MediaItem.MediaItemId.ToString();
        return string.Empty;
      }
      FilterItem filterItem = listItem as FilterItem;
      if (filterItem != null)
      {
        if (filterItem.MediaItem != null)
          // Fanart loading now depends on the MediaItemId to support local fanart
          return filterItem.MediaItem.MediaItemId.ToString();
        return string.Empty;
      }
      return string.Empty;
    }

    bool IFanartImageSourceProvider.TryCreateFanartImageSource(ListItem listItem, out FanArtImageSource fanartImageSource)
    {
      string fanArtName = GetFanArtName(listItem);

      SeriesFilterItem series = listItem as SeriesFilterItem;
      if (series != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Series,
          FanArtName = fanArtName
        };
        return true;
      }
      SeasonFilterItem season = listItem as SeasonFilterItem;
      if (season != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.SeriesSeason,
          FanArtName = fanArtName
        };
        return true;
      }
      EpisodeItem episode = listItem as EpisodeItem;
      if (episode != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Episode,
          FanArtName = fanArtName
        };
        return true;
      }
      MovieFilterItem movieCollection = listItem as MovieFilterItem;
      if (movieCollection != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.MovieCollection,
          FanArtName = fanArtName
        };
        return true;
      }
      MovieItem movie = listItem as MovieItem;
      if (movie != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Movie,
          FanArtName = fanArtName
        };
        return true;
      }
      VideoItem video = listItem as VideoItem;
      if (video != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Movie,
          FanArtName = fanArtName
        };
        return true;
      }
      AlbumFilterItem albumItem = listItem as AlbumFilterItem;
      if (albumItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Album,
          FanArtName = fanArtName
        };
        return true;
      }
      AudioItem audioItem = listItem as AudioItem;
      if (audioItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Audio,
          FanArtName = fanArtName
        };
        return true;
      }
      ActorFilterItem actorItem = listItem as ActorFilterItem;
      if (actorItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Actor,
          FanArtName = fanArtName
        };
        return true;
      }
      DirectorFilterItem directorItem = listItem as DirectorFilterItem;
      if (directorItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Director,
          FanArtName = fanArtName
        };
        return true;
      }
      WriterFilterItem writerItem = listItem as WriterFilterItem;
      if (writerItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Writer,
          FanArtName = fanArtName
        };
        return true;
      }
      ArtistFilterItem artisitItem = listItem as ArtistFilterItem;
      if (artisitItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Artist,
          FanArtName = fanArtName
        };
        return true;
      }
      ComposerFilterItem composerItem = listItem as ComposerFilterItem;
      if (composerItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Composer,
          FanArtName = fanArtName
        };
        return true;
      }
      CharacterFilterItem characterItem = listItem as CharacterFilterItem;
      if (characterItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Character,
          FanArtName = fanArtName
        };
        return true;
      }
      CompanyFilterItem companyItem = listItem as CompanyFilterItem;
      if (companyItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Company,
          FanArtName = fanArtName
        };
        return true;
      }
      TVNetworkFilterItem tvNetworkItem = listItem as TVNetworkFilterItem;
      if (tvNetworkItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.TVNetwork,
          FanArtName = fanArtName
        };
        return true;
      }
      ImageItem imgItem = listItem as ImageItem;
      if (imgItem != null)
      {
        fanartImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaTypes.Image,
          FanArtName = fanArtName
        };
        return true;
      }
      fanartImageSource = null;
      return false;
    }
  }
}

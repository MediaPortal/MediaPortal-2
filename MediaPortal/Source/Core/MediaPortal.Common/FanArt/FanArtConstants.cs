#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

namespace MediaPortal.Common.FanArt
{
  public static class FanArtMediaTypes
  {
    public const string Undefined = "Undefined";
    public const string Movie = "Movie";
    public const string MovieCollection = "MovieCollection";
    public const string Series = "Series";
    public const string SeriesSeason = "SeriesSeason";
    public const string Episode = "Episode";
    public const string Actor = "Actor";
    public const string Artist = "Artist";
    public const string ChannelTv = "ChannelTv";
    public const string ChannelRadio = "ChannelRadio";
    public const string Album = "Album";
    public const string Audio = "Audio";
    public const string Image = "Image";
    public const string Character = "Character";
    public const string Director = "Director";
    public const string Writer = "Writer";
    public const string Composer = "Composer";
    public const string Conductor = "Conductor";
    public const string Company = "Company";
    public const string TVNetwork = "TVNetwork";
    public const string MusicLabel = "MusicLabel";

    public static bool TryGetMediaItemFanArtType(MediaItem mediItem, out string fanArtMediaType)
    {
      fanArtMediaType = null;

      if (mediItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
        fanArtMediaType = FanArtMediaTypes.Audio;
      else if (mediItem.Aspects.ContainsKey(AudioAlbumAspect.ASPECT_ID))
        fanArtMediaType = FanArtMediaTypes.Album;
      else if (mediItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID))
        fanArtMediaType = FanArtMediaTypes.Movie;
      else if (mediItem.Aspects.ContainsKey(MovieCollectionAspect.ASPECT_ID))
        fanArtMediaType = FanArtMediaTypes.MovieCollection;
      else if (mediItem.Aspects.ContainsKey(SeriesAspect.ASPECT_ID))
        fanArtMediaType = FanArtMediaTypes.Series;
      else if (mediItem.Aspects.ContainsKey(SeasonAspect.ASPECT_ID))
        fanArtMediaType = FanArtMediaTypes.SeriesSeason;
      else if (mediItem.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID))
        fanArtMediaType = FanArtMediaTypes.Episode;
      else if (mediItem.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
        fanArtMediaType = FanArtMediaTypes.Image;
      else if (mediItem.Aspects.ContainsKey(CharacterAspect.ASPECT_ID))
        fanArtMediaType = FanArtMediaTypes.Character;
      else if (mediItem.Aspects.ContainsKey(PersonAspect.ASPECT_ID))
      {
        if (MediaItemAspect.TryGetAspect(mediItem.Aspects, PersonAspect.Metadata, out var aspect))
        {
          string occupation = (string)aspect[PersonAspect.ATTR_OCCUPATION];
          if (occupation == PersonAspect.OCCUPATION_ACTOR)
            fanArtMediaType = FanArtMediaTypes.Actor;
          else if (occupation == PersonAspect.OCCUPATION_ARTIST)
            fanArtMediaType = FanArtMediaTypes.Artist;
          else if (occupation == PersonAspect.OCCUPATION_COMPOSER)
            fanArtMediaType = FanArtMediaTypes.Composer;
          else if (occupation == PersonAspect.OCCUPATION_CONDUCTOR)
            fanArtMediaType = FanArtMediaTypes.Conductor;
          else if (occupation == PersonAspect.OCCUPATION_DIRECTOR)
            fanArtMediaType = FanArtMediaTypes.Director;
          else if (occupation == PersonAspect.OCCUPATION_WRITER)
            fanArtMediaType = FanArtMediaTypes.Writer;
        }
      }
      else if (mediItem.Aspects.ContainsKey(CompanyAspect.ASPECT_ID))
      {
        if (MediaItemAspect.TryGetAspect(mediItem.Aspects, CompanyAspect.Metadata, out var aspect))
        {
          string type = (string)aspect[CompanyAspect.ATTR_COMPANY_TYPE];
          if (type == CompanyAspect.COMPANY_MUSIC_LABEL)
            fanArtMediaType = FanArtMediaTypes.MusicLabel;
          else if (type == CompanyAspect.COMPANY_PRODUCTION)
            fanArtMediaType = FanArtMediaTypes.Company;
          else if (type == CompanyAspect.COMPANY_TV_NETWORK)
            fanArtMediaType = FanArtMediaTypes.TVNetwork;
        }
      }

      return !string.IsNullOrEmpty(fanArtMediaType);
    }
  }

  public static class FanArtTypes
  {
    public const string Undefined = "Undefined";
    public const string Poster = "Poster";
    public const string Banner = "Banner";
    public const string FanArt = "FanArt";
    public const string Cover = "Cover";
    public const string Thumbnail = "Thumbnail";
    public const string ClearArt = "ClearArt";
    public const string DiscArt = "DiscArt";
    public const string Logo = "Logo";
  }
}

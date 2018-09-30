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

namespace MediaPortal.Common.MediaManagement.Helpers
{
  public static class HelperExtensionMethods
  {
    public static SeriesInfo AsSeries(this BaseInfo info)
    {
      SeriesInfo series = info as SeriesInfo;
      if (series != null)
        return series;
      SeasonInfo season = info as SeasonInfo;
      if (season != null)
        return season.CloneBasicInstance<SeriesInfo>();
      EpisodeInfo episode = info as EpisodeInfo;
      if (episode != null)
        return episode.CloneBasicInstance<SeriesInfo>();
      return null;
    }

    public static SeasonInfo AsSeason(this BaseInfo info)
    {
      SeasonInfo season = info as SeasonInfo;
      if (season != null)
        return season;
      EpisodeInfo episode = info as EpisodeInfo;
      if (episode != null)
        return episode.CloneBasicInstance<SeasonInfo>();
      return null;
    }

    public static AlbumInfo AsAlbum(this BaseInfo info)
    {
      AlbumInfo album = info as AlbumInfo;
      if (album != null)
        return album;
      TrackInfo track = info as TrackInfo;
      if (track != null)
        return track.CloneBasicInstance<AlbumInfo>();
      return null;
    }

    public static MovieCollectionInfo AsMovieCollection(this BaseInfo info)
    {
      MovieCollectionInfo collection = info as MovieCollectionInfo;
      if (collection != null)
        return collection;
      MovieInfo movie = info as MovieInfo;
      if (movie != null)
        return movie.CloneBasicInstance<MovieCollectionInfo>();
      return null;
    }
  }
}

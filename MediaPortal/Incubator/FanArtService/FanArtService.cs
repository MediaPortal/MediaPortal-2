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
using System.IO;
using System.Linq;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;

namespace MediaPortal.Extensions.UserServices.FanArtService
{
  public class FanArtService : IFanArtService
  {
    public IList<FanArtImage> GetFanArt(FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom)
    {
      string baseFolder = GetBaseFolder(mediaType, name);
      // No known series
      if (baseFolder == null || !Directory.Exists(baseFolder))
        return null;

      string pattern = GetPattern(mediaType, fanArtType, name);
      if (string.IsNullOrEmpty(pattern))
        return null;

      DirectoryInfo directoryInfo = new DirectoryInfo(baseFolder);
      try
      {
        List<string> files = directoryInfo.GetFiles(pattern).Select(file => file.FullName).ToList();
        List<FanArtImage> fanArtImages = files.Select(f => FanArtImage.FromFile(f, maxWidth, maxHeight)).Where(fanArtImage => fanArtImage != null).ToList();

        if (fanArtImages.Count == 0)
          return null;
        return singleRandom ? GetSingleRandom(fanArtImages) : fanArtImages;
      }
      catch (DirectoryNotFoundException)
      {
        return null;
      }
    }

    protected IList<FanArtImage> GetSingleRandom(IList<FanArtImage> fullList)
    {
      if (fullList.Count <= 1)
        return fullList;

      Random rnd = new Random(DateTime.Now.Millisecond);
      int rndIndex = rnd.Next(fullList.Count - 1);
      return new List<FanArtImage> { fullList[rndIndex] };
    }

    protected string GetPattern(FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string name)
    {
      switch (mediaType)
      {
        case FanArtConstants.FanArtMediaType.MovieCollection:
        case FanArtConstants.FanArtMediaType.Movie:
          switch (fanArtType)
          {
            case FanArtConstants.FanArtType.Poster:
              return "Posters\\*.jpg";
            case FanArtConstants.FanArtType.FanArt:
              return "Backdrops\\*.jpg";
            default:
              return null;
          }

        case FanArtConstants.FanArtMediaType.Series:
          switch (fanArtType)
          {
            case FanArtConstants.FanArtType.Banner:
              return "img_graphical_*.jpg";
            case FanArtConstants.FanArtType.Poster:
              return "img_posters_*.jpg";
            case FanArtConstants.FanArtType.FanArt:
              return "img_fan-*.jpg";
            default:
              return null;
          }

        case FanArtConstants.FanArtMediaType.Channel:
          return string.Format("{0}.png", name);
      }
      return null;
    }

    protected string GetBaseFolder(FanArtConstants.FanArtMediaType mediaType, string name)
    {
      switch (mediaType)
      {
        case FanArtConstants.FanArtMediaType.Series:
          int tvDbId;
          return !SeriesTvDbMatcher.Instance.TryGetTvDbId(name, out tvDbId) ? null : Path.Combine(SeriesTvDbMatcher.CACHE_PATH, tvDbId.ToString());

        case FanArtConstants.FanArtMediaType.Movie:
          int movieDbId;
          return !MovieTheMovieDbMatcher.Instance.TryGetMovieDbId(name, out movieDbId) ? null : Path.Combine(MovieTheMovieDbMatcher.CACHE_PATH, movieDbId.ToString());

        case FanArtConstants.FanArtMediaType.MovieCollection:
          int collectionId;
          return !MovieTheMovieDbMatcher.Instance.TryGetCollectionId(name, out collectionId) ? null : Path.Combine(MovieTheMovieDbMatcher.CACHE_PATH, "COLL_" + collectionId);

        case FanArtConstants.FanArtMediaType.Channel:
          return @"Plugins\SlimTv.Service\Content\ChannelLogos";

        default:
          return null;
      }
    }
  }
}

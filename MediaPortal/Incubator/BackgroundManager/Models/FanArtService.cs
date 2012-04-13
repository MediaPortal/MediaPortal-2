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

namespace MediaPortal.UiComponents.BackgroundManager.Models
{
  public class FanArtService : IFanArtService
  {
    // TODO: Temporary lookup for series and their TvDB ID; This translation should be saved either in ML, Server settings,..
    // TODO: Implement a series matcher and scraper to map series and then download fanart.
    // TODO: Implement the features of this service as server plugin; change this class to act as proxy to access server
    Dictionary<string, int> _seriesLookup = new Dictionary<string, int>
                                                 {
                                                   { "Eureka", 79334},
                                                   { "EUReKA - Die geheime Stadt", 79334},
                                                   { "Caprica", 85040},
                                                   { "Fringe", 82066},
                                                   { "Fringe - Grenzfälle des FBI", 82066},
                                                   { "Bionic Woman", 80370},
                                                   { "Falling Skies", 205281},
                                                   { "FlashForward", 84024},
                                                   { "Heroes", 79501},
                                                   { "Haven", 158661},
                                                 };

    public IList<string> GetFanArt(FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string name, bool singleRandom)
    {
      string baseFolder = GetBaseFolder(mediaType, name);
      // No known series
      if (baseFolder == null)
        return new List<string> { fanArtType == FanArtConstants.FanArtType.Banner ? "NoBanner.png" : "NoPoster.png" };

      string pattern = GetPattern(fanArtType);
      DirectoryInfo directoryInfo = new DirectoryInfo(baseFolder);
      List<string> files = directoryInfo.GetFiles(pattern).Select(file => file.FullName).ToList();
      return singleRandom ? GetSingleRandom(files) : files;
    }

    protected IList<string> GetSingleRandom(IList<string> fullList)
    {
      if (fullList.Count <= 1)
        return fullList;

      Random rnd = new Random(DateTime.Now.Millisecond);
      int rndIndex = rnd.Next(fullList.Count - 1);
      return new List<string> { fullList[rndIndex] };
    }

    protected string GetPattern(FanArtConstants.FanArtType fanArtType)
    {
      switch(fanArtType)
      {
        case FanArtConstants.FanArtType.Banner:
          return "img_graphical_*.jpg";
        case FanArtConstants.FanArtType.Poster:
          return "img_posters_*.jpg";
        case FanArtConstants.FanArtType.FanArt:
        default:
          return "img_fan-*.jpg";
      }
    }

    protected string GetBaseFolder(FanArtConstants.FanArtMediaType mediaType, string name)
    {
      int part;
      if (!_seriesLookup.TryGetValue(name, out part))
        return null;

      return @"D:\Coding\MP\MP2\Series\TvdbLib\Cache\" + part;
    }
  }
}

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
    public IList<string> GetFanArt(FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string name, bool singleRandom)
    {
      string baseFolder = GetBaseFolder(mediaType, name);
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
      return @"D:\Coding\MP\MP2\Series\TvdbLib\Cache\79334";
    }
  }
}

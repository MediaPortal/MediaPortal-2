#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.Plugins.SlimTv.SlimTvResources.FanartProvider
{
  public class SlimTvFanartProvider : IFanArtProvider
  {
    /// <summary>
    /// Gets a list of <see cref="FanArtImage"/>s for a requested <paramref name="mediaType"/>, <paramref name="fanArtType"/> and <paramref name="name"/>.
    /// The name can be: Series name, Actor name, Artist name depending on the <paramref name="mediaType"/>.
    /// </summary>
    /// <param name="mediaType">Requested FanArtMediaType</param>
    /// <param name="fanArtType">Requested FanArtType</param>
    /// <param name="name">Requested name of Series, Actor, Artist...</param>
    /// <param name="maxWidth">Maximum width for image. <c>0</c> returns image in original size.</param>
    /// <param name="maxHeight">Maximum height for image. <c>0</c> returns image in original size.</param>
    /// <param name="singleRandom">If <c>true</c> only one random image URI will be returned</param>
    /// <param name="result">Result if return code is <c>true</c>.</param>
    /// <returns><c>true</c> if at least one match was found.</returns>
    public bool TryGetFanArt(FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<string> result)
    {
      result = null;
      string baseFolder = GetBaseFolder(mediaType, name);
      // No known series
      if (baseFolder == null || !Directory.Exists(baseFolder))
        return false;

      string pattern = GetPattern(mediaType, fanArtType, name);
      if (string.IsNullOrEmpty(pattern))
        return false;

      try
      {
        DirectoryInfo directoryInfo = new DirectoryInfo(baseFolder);
        if (directoryInfo.Exists)
        {
          result = directoryInfo.GetFiles(pattern).Select(file => file.FullName).ToList();
          return result.Count > 0;
        }
      }
      catch (Exception) { }
      return false;
    }

    protected string GetPattern(FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string name)
    {
      if (mediaType != FanArtConstants.FanArtMediaType.Channel)
        return null;

      return FileUtils.GetSafeFilename(string.Format("{0}.png", name));
    }

    protected string GetBaseFolder(FanArtConstants.FanArtMediaType mediaType, string name)
    {
      switch (mediaType)
      {
        case FanArtConstants.FanArtMediaType.Channel:
          return @"Plugins\SlimTv.Resources\Resources\ChannelLogos";
        default:
          return null;
      }
    }
  }
}

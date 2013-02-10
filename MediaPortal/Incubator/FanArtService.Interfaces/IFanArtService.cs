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

using System.Collections.Generic;

namespace MediaPortal.Extensions.UserServices.FanArtService.Interfaces
{
  /// <summary>
  /// Delegate definition for <see cref="IFanArtService.GetFanArt"/>.
  /// </summary>
  /// <param name="mediaType">Requested FanArtMediaType</param>
  /// <param name="fanArtType">Requested FanArtType</param>
  /// <param name="name">Requested name of Series, Actor, Artist...</param>
  /// <param name="maxWidth">Maximum width for image. <c>0</c> returns image in original size.</param>
  /// <param name="maxHeight">Maximum height for image. <c>0</c> returns image in original size.</param>
  /// <param name="singleRandom">If <c>true</c> only one random image URI will be returned</param>
  /// <returns>List of fanart image URIs</returns>
  public delegate IList<FanArtImage> GetFanArtDelegate(FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom);

  /// <summary>
  /// <see cref="IFanArtService"/> provides fanart images for different media types and allows scraping of information from internet sources
  /// like <c>http://thetvdb.com</c>.
  /// </summary>
  public interface IFanArtService
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
    /// <returns>List of fanart image URIs</returns>
    IList<FanArtImage> GetFanArt(FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom);
  }
}

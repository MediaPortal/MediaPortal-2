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

using System.Collections.Generic;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Extensions.UserServices.FanArtService.Interfaces
{
  public enum FanArtProviderSource
  {
    /// <summary>
    /// Uses fanart cache as source.
    /// </summary>
    Cache,
    /// <summary>
    /// Looks for fanart files that can be used as source.
    /// </summary>
    File,
    /// <summary>
    /// Uses database as fanart source.
    /// </summary>
    Database,
    /// <summary>
    /// Retrieves fanart from external sources like online sources (like IMDB, TMDB, TvDB, ...).
    /// </summary>
    Online,
    /// <summary>
    /// Provide fanart that could not be retrieved from any other source as a last resort.
    /// </summary>
    FallBack,
  }

  /// <summary>
  /// <see cref="IFanArtService"/> provides fanart images for different media types. It will try to find content provided by any of the registered <see cref="Providers"/>.
  /// </summary>
  public interface IFanArtService
  {
    /// <summary>
    /// Gets the list of all registered <see cref="IFanArtProvider"/>.
    /// </summary>
    IList<IFanArtProvider> Providers { get; }

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
    IList<FanArtImage> GetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom);
  }

  /// <summary>
  /// <see cref="IFanArtProvider"/> provides fanart images for specific media types.
  /// </summary>
  public interface IFanArtProvider
  {
    FanArtProviderSource Source { get; }

    /// <summary>
    /// Gets a list of file names for a requested <paramref name="mediaType"/>, <paramref name="fanArtType"/> and <paramref name="name"/>.
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
    bool TryGetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<IResourceLocator> result);
  }

  /// <summary>
  /// <see cref="IBinaryFanArtProvider"/> provides binary fanart images for specific media types.
  /// </summary>
  public interface IBinaryFanArtProvider : IFanArtProvider
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
    bool TryGetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<FanArtImage> result);
  }
}

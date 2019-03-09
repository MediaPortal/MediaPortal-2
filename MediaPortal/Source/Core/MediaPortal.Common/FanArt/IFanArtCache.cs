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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaPortal.Common.FanArt
{
  /// <summary>
  /// Delegate that tries to download an image to the specified directory.
  /// </summary>
  /// <param name="saveDirectory">The directory where the image should be saved.</param>
  /// <returns><c>true</c> if the image was saved successfully.</returns>
  public delegate Task<bool> TrySaveFanArtAsyncDelegate(string saveDirectory);

  /// <summary>
  /// Delegate that tries to download an image to the specified directory.
  /// </summary>
  /// <typeparam name="T">The type of the image file to save.</typeparam>
  /// <param name="saveDirectory">The directory where the image should be saved.</param>
  /// <param name="fanArtFile">The image file to save.</param>
  /// <returns><c>true</c> if the image was saved successfully.</returns>
  public delegate Task<bool> TrySaveMultipleFanArtAsyncDelegate<T>(string saveDirectory, T fanArtFile);

  /// <summary>
  /// Interface for storage and retrieval of fanart images for media items.
  /// </summary>
  public interface IFanArtCache
  {
    /// <summary>
    /// Tries to save a fanart image to the cache using the specified <see cref="TrySaveFanArtAsyncDelegate"/>.
    /// </summary>
    /// <param name="mediaItemId">The id of the media item.</param>
    /// <param name="title">The title to use for the cache directory.</param>
    /// <param name="fanArtType">The type of fanart.</param>
    /// <param name="saveDlgt"><see cref="TrySaveFanArtAsyncDelegate"/> that saves an image to the cache directory.</param>
    /// <returns><c>true</c> if the image was succesfully saved to the cache.</returns>
    Task<bool> TrySaveFanArt(Guid mediaItemId, string title, string fanArtType, TrySaveFanArtAsyncDelegate saveDlgt);

    /// <summary>
    /// Tries to save multiple fanart images to the cache using the specified <see cref="TrySaveMultipleFanArtAsyncDelegate<typeparamref name="T"/>"/>.
    /// </summary>
    /// <typeparam name="T">The type of the image file to save.</typeparam>
    /// <param name="mediaItemId">The id of the media item.</param>
    /// <param name="title">The title to use for the cache directory.</param>
    /// <param name="fanArtType">The type of fanart.</param>
    /// <param name="files">Collection of fanart files to save.</param>
    /// <param name="saveDlgt"><see cref="TrySaveMultipleFanArtAsyncDelegate<typeparamref name="T"/> that saves each image file.</param>
    /// <returns>The number of images that were succesfully saved to the cache.</returns>
    Task<int> TrySaveFanArt<T>(Guid mediaItemId, string title, string fanArtType, ICollection<T> files, TrySaveMultipleFanArtAsyncDelegate<T> saveDlgt);

    /// <summary>
    /// Gets a list of paths to all fanart of the specified type for the specified media item.
    /// </summary>
    /// <param name="mediaItemId">The id of the media item.</param>
    /// <param name="fanArtType">The type of fanart.</param>
    /// <returns>List of fanart paths.</returns>
    IList<string> GetFanArtFiles(Guid mediaItemId, string fanArtType);

    /// <summary>
    /// Gets a list of all media item ids which have fanart.
    /// </summary>
    /// <returns>List of media item ids with fanart.</returns>
    ICollection<Guid> GetAllFanArtIds();

    /// <summary>
    /// Deletes all fanart for the specified media item.
    /// </summary>
    /// <param name="mediaItemId"></param>
    void DeleteFanArtFiles(Guid mediaItemId);
  }
}

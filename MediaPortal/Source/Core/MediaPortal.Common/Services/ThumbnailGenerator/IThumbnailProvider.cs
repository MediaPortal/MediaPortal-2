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

using System.IO;

namespace MediaPortal.Common.Services.ThumbnailGenerator
{
  /// <summary>
  /// Interface to implement different thumbnail generation providers, which will be used in the <see cref="IThumbnailGenerator"/> service.
  /// </summary>
  public interface IThumbnailProvider
  {
    /// <summary>
    /// Creates a thumbnail for the specified <paramref name="fileOrFolderPath"/> with the given paramters.
    /// </summary>
    /// <param name="fileOrFolderPath">The file or folder path to create a thumbnail image for.</param>
    /// <param name="width">The desired width of the thumbnail image.</param>
    /// <param name="height">The desired height of the thumbnail image.</param>
    /// <param name="cachedOnly">True to return only cached thumbs.</param>
    /// <param name="imageData">The image data of the given <paramref name="imageType"/>.</param>
    /// <param name="imageType">The type of the <paramref name="imageData"/>.</param>
    /// <returns><c>true</c>, if the thumbnail could successfully be created, else <c>false</c>.</returns>
    bool GetThumbnail(string fileOrFolderPath, int width, int height, bool cachedOnly, out byte[] imageData, out ImageType imageType);

    /// <summary>
    /// Creates a thumbnail for the specified <paramref name="stream"/> with the given paramters.
    /// </summary>
    /// <param name="stream">A Stream to create a thumbnail image for.</param>
    /// <param name="width">The desired width of the thumbnail image.</param>
    /// <param name="height">The desired height of the thumbnail image.</param>
    /// <param name="cachedOnly">True to return only cached thumbs.</param>
    /// <param name="imageData">The image data of the given <paramref name="imageType"/>.</param>
    /// <param name="imageType">The type of the <paramref name="imageData"/>.</param>
    /// <returns><c>true</c>, if the thumbnail could successfully be created, else <c>false</c>.</returns>
    bool GetThumbnail(Stream stream, int width, int height, bool cachedOnly, out byte[] imageData, out ImageType imageType);
  }
}

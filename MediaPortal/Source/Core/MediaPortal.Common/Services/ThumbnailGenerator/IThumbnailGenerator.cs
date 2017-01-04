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

namespace MediaPortal.Common.Services.ThumbnailGenerator
{
  public enum ImageType
  {
    Unknown = 0,
    Jpeg = 1,
    Png = 2
  }

  /// <summary>
  /// Handler delegate to be called when a thumbnail was created.
  /// </summary>
  /// <param name="sourcePath">The input file path the thumbnail was generated for.</param>
  /// <param name="success">If set to <c>true</c>, the thumbnail could be created successfully,
  /// else the creation didn't work for some reason.</param>
  /// <param name="imageData">Returns the data of the created image, if <c>success == true</c>.
  /// Else, the value is undefined.</param>
  /// <param name="imageType">Returns the type of the created image, if <c>success == true</c>.
  /// Else, the value is undefined.</param>
  public delegate void CreatedDelegate(string sourcePath, bool success, byte[] imageData, ImageType imageType);

  /// <summary>
  /// Interface for a thumbnail generator service, which takes synchronous and asynchronous thumbnail
  /// generation tasks for input files and folders. This interface doesn't expose any information about the
  /// storage of the created thumbnails. They might be stored in the same folder as the requested images, or in a thumbnail
  /// db or somewhere else.
  /// </summary>
  public interface IThumbnailGenerator
  {
    /// <summary>
    /// Creates a thumbnail for the specified <paramref name="fileOrFolderPath"/> with a default size.
    /// </summary>
    /// <param name="fileOrFolderPath">The file or folder path to create a thumbnail image for.</param>
    /// <param name="cachedOnly">True to return only cached thumbs.</param>
    /// <param name="imageData">The image data of the given <paramref name="imageType"/>.</param>
    /// <param name="imageType">The type of the <paramref name="imageData"/>.</param>
    /// <returns><c>true</c>, if the thumbnail could successfully be created, else <c>false</c>.</returns>
    bool GetThumbnail(string fileOrFolderPath, bool cachedOnly, out byte[] imageData, out ImageType imageType);

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
    /// Asynchronously creates a thumbnail file for the specified <paramref name="fileOrFolderPath"/> with a default size.
    /// </summary>
    /// <param name="fileOrFolderPath">The file or folder path to create a thumbnail image for.</param>
    /// <param name="createdDelegate">Optional delegate method which will be called when the
    /// thumbnail creation is finished.</param>
    void GetThumbnail_Async(string fileOrFolderPath, CreatedDelegate createdDelegate);

    /// <summary>
    /// Asynchronously creates a thumbnail file for the specified <paramref name="fileOrFolderPath"/> with
    /// the given parameters.
    /// </summary>
    /// <param name="fileOrFolderPath">The file or folder path to create a thumbnail image for.</param>
    /// <param name="width">The desired width of the thumbnail image.</param>
    /// <param name="height">The desired height of the thumbnail image.</param>
    /// <param name="createdDelegate">Optional delegate method which will be called when the
    ///   thumbnail creation is finished.</param>
    void GetThumbnail_Async(string fileOrFolderPath, int width, int height, CreatedDelegate createdDelegate);
  }
}

#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

namespace MediaPortal.UI.Thumbnails
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
  /// Interface for an asynchonous thumbnail generator service, which takes thumbnail generation
  /// tasks for input files and folders. There are also methods to query the execution state for
  /// input files. This interface doesn't expose any information about the storage of the created
  /// thumbnails. They might be stored in the same folder as the requested images, or in a thumbnail
  /// db or somewhere else.
  /// </summary>
  public interface IAsyncThumbnailGenerator
  {
    /// <summary>
    /// Returns the information if a thumbnail for the specified <paramref name="fileOrFolder"/>
    /// already exists.
    /// </summary>
    /// <param name="fileOrFolderPath">The path of a file or folder for that the thumbnail is requested.</param>
    /// <returns><c>true</c>, if the thumbnail for the specified <paramref name="fileOrFolderPath"/>
    /// already exists. <c>false</c>, if the thumbnail doesn't exist or if its creation is not finished
    /// yet.</returns>
    bool Exists(string fileOrFolderPath);

    /// <summary>
    /// Returns the information if this thumbnail generator already is scheduled to create a
    /// thumbnail for the specified <paramref name="fileOrFolderPath"/>.
    /// </summary>
    /// <param name="fileOrFolderPath">The file or folder path for that the thumbnail should be created.</param>
    /// <returns><c>true</c>, if the creation of a thumbnail for the specified
    /// <paramref name="fileOrFolderPath"/> is already scheduled and not finished yet.</returns>
    bool IsCreating(string fileOrFolderPath);

    bool GetThumbnail(string fileOrFolderPath, out byte[] imageData, out ImageType imageType);

    /// <summary>
    /// Convenience method for <see cref="Create"/>. Will call <see cref="Create"/> with default
    /// parameters and without a finish method delegate.
    /// </summary>
    /// <param name="fileOrFolderPath">The file or folder, the thumbnail should be generated for.</param>
    void CreateThumbnail(string fileOrFolderPath);

    /// <summary>
    /// Creates a thumbnail file for the specified <paramref name="fileOrFolderPath"/>.
    /// </summary>
    /// <param name="fileOrFolderPath">The file or folder path to create a thumbnail image for.</param>
    /// <param name="width">The desired width of the thumbnail image.</param>
    /// <param name="height">The desired height of the thumbnail image.</param>
    /// <param name="quality">The desired quality level of the thumbnail. The range of values
    /// is 1 (lowest quality) up to 100 (highest quality).</param>
    /// <param name="createdDelegate">Optional delegate method which will be called when the
    /// thumbnail creation is finished.</param>
    void Create(string fileOrFolderPath, int width, int height, int quality,
        CreatedDelegate createdDelegate);
  }
}

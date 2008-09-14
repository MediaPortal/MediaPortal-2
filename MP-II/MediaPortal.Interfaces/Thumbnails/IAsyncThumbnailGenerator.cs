#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.IO;

namespace MediaPortal.Thumbnails
{
  /// <summary>
  /// Handler delegate to be called when a thumbnail was created.
  /// </summary>
  /// <param name="source">The input file the thumbnail should be generated for.</param>
  /// <param name="destination">The resulting thumbnail file.</param>
  /// <param name="success">If set to <c>true</c>, the thumbnail could be created successfully,
  /// else the creation didn't work for some reason.</param>
  public delegate void CreatedDelegate(FileSystemInfo source, FileInfo destination, bool success);

  /// <summary>
  /// Interface for an asynchonous thumbnail generator service, which takes thumbnail generation
  /// tasks for input files and folders. There are also methods to query the execution state for
  /// input files.
  /// </summary>
  public interface IAsyncThumbnailGenerator
  {
    /// <summary>
    /// Returns the information if a thumbnail for the specified <paramref name="fileOrFolder"/>
    /// already exists.
    /// </summary>
    /// <param name="fileOrFolder">The file or folder for that the thumbnail is requested.</param>
    /// <returns><c>true</c>, if the thumbnail for the specified <paramref name="fileOrFolder"/>
    /// already exists. <c>false</c>, if the thumbnail doesn't exist or if its creation is not finished
    /// yet.</returns>
    bool Exists(FileSystemInfo fileOrFolder);

    /// <summary>
    /// Returns the information if this thumbnail generator already has the job to create a
    /// thumbnail for the specified <paramref name="fileOrFolder"/>.
    /// </summary>
    /// <param name="fileOrFolder">The file or folder for that the thumbnail should be created.</param>
    /// <returns><c>true</c>, if the creation of a thumbnail for the specified
    /// <paramref name="fileOrFolder"/> is already scheduled and not finished yet.</returns>
    bool IsCreating(FileSystemInfo fileOrFolder);

    byte[] GetThumbnail(FileSystemInfo fileOrFolder);

    /// <summary>
    /// Convenience method for <see cref="Create"/>. Will call <see cref="Create"/> with default
    /// parameters and without a finish method delegate.
    /// </summary>
    /// <param name="fileOrFolder">The file or folder, the thumbnail should be generated for.</param>
    void CreateThumbnail(FileSystemInfo fileOrFolder);

    /// <summary>
    /// Creates a thumbnail file for the specified <paramref name="fileOrFolder"/>.
    /// If the <paramref name="fileOrFolder"/> parameter is a <see cref="FileInfo"/>, the thumbnail
    /// file will be created in the same folder as the source file, with the same file name, with an
    /// extension of ".jpg". If <paramref name="fileOrFolder"/> is a <see cref="DirectoryInfo"/>,
    /// the thumbnail will be created in that folder with a name of <see cref="FOLDER_THUMB_NAME"/>.
    /// </summary>
    /// <param name="fileOrFolder">The file or folder to create a thumbnail image for.</param>
    /// <param name="width">The desired width of the thumbnail image.</param>
    /// <param name="height">The desired height of the thumbnail image.</param>
    /// <param name="quality">The desired quality level of the thumbnail. The range of values
    /// is 1 (lowest quality) up to 100 (highest quality).</param>
    /// <param name="createdDelegate">Optional delegate method which will be called when the
    /// thumbnail creation is finished.</param>
    void Create(FileSystemInfo fileOrFolder, int width, int height, int quality,
        CreatedDelegate createdDelegate);
  }
}

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

using System.Collections.Generic;
using MediaPortal.Media.MediaManager;

namespace MediaPortal.Media.Importers
{
  /// <summary>
  /// Worker interface a media importer has to implement.
  /// The metadata for an importer are given in the plugin descriptor file for the importer.
  /// </summary>
  public interface IImporter
  {
    /// <summary>
    /// Called by the importer manager before the import process starts
    /// Allows the importer to do housekeeping before importing the first file
    /// i.e. The Music Importer would check for non-existing music files
    /// </summary>
    /// <param name="availableFiles">The number of Files the Importer should process</param>
    void BeforeImport(int availableFiles);

    /// <summary>
    /// Called by the importer manager after the last file has been imported
    /// Allows the importer to do Cleanup
    /// </summary>
    void AfterImport();

    /// <summary>
    /// Called by the importer manager when a full-import needs to be done for each file.
    /// </summary>
    /// <param name="filePath">The path of the file to import.</param>
    bool FileImport(string filePath);

    /// <summary>
    /// Called by the importer manager after it detected that a file was deleted
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="filePath">The path of the deleted file.</param>
    void FileDeleted(string filePath);

    /// <summary>
    /// Called by the importer manager after it detected that a file was created
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="filePath">The path of the new file.</param>
    void FileCreated(string filePath);

    /// <summary>
    /// Called by the importer manager after it detected that a file was changed
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="filePath">The path of the changed file.</param>
    void FileChanged(string filePath);

    /// <summary>
    /// Called by the importer manager after it detected that a file or dikrectory was renamed
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="filePath">The new path of the changed file.</param>
    /// <param name="oldFilePath">The old path of the changed file.</param>
    void FileRenamed(string filePath, string oldFilePath);

    /// <summary>
    /// Called by the importer manager after it detected that a directory was deleted
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="path">The path of the deleted folder.</param>
    void DirectoryDeleted(string path);

    void GetMetaDataFor(string folder, ref IList<IAbstractMediaItem> items);
  }
}

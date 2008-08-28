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
  public interface IImporter
  {
    ///// <summary>
    ///// Gets the importer name.
    ///// </summary>
    ///// <value>The importer name.</value>
    //string Name { get;}

    ///// <summary>
    ///// Gets the file-extensions the importer supports
    ///// </summary>
    ///// <value>The file-extensions.</value>
    //string Extensions { get;}

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
    /// Called by the importer manager when a full-import needs to be done
    /// </summary>
    /// <param name="folder">the file.</param>
    bool FileImport(string filename);

    /// <summary>
    /// Called by the importer manager after it detected that a file was deleted
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the deleted file.</param>
    void FileDeleted(string filename);

    /// <summary>
    /// Called by the importer manager after it detected that a file was created
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the new file.</param>
    void FileCreated(string filename);

    /// <summary>
    /// Called by the importer manager after it detected that a file was changed
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the changed file.</param>
    void FileChanged(string filename);

    /// <summary>
    /// Called by the importer manager after it detected that a file or dikrectory was renamed
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the changed file.</param>
    void FileRenamed(string filename, string oldFileName);

    /// <summary>
    /// Called by the importer manager after it detected that a directory was deleted
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the changed file.</param>
    void DirectoryDeleted(string directoryname);

    void GetMetaDataFor(string folder, ref List<IAbstractMediaItem> items);
  }
}

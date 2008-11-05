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

using System;
using System.Collections.Generic;
using MediaPortal.Media.MediaManagement;

namespace MediaPortal.Media.Importers
{
  /// <summary>
  /// Interface for accessing the shares/importer engine. This interface provides methods to
  /// access/add/remove shares and to start and configure the import.
  /// </summary>
  public interface IImporterManager
  {
    /// <summary>
    /// Adds a new local share which should be imported & watched.
    /// </summary>
    /// <param name="folderPath">The path of the folder to import.</param>
    void AddShare(string folderPath);

    /// <summary>
    /// Removes a share from the collection of monitored shares.
    /// </summary>
    /// <param name="folderPath">The path of the folder to remove.</param>
    void RemoveShare(string folderPath);

    /// <summary>
    /// Returns the information if the importer manager watches the specified share
    /// <paramref name="folderPath"/>.
    /// </summary>
    /// <param name="folderPath">The path of the folder to check.</param>
    /// <returns><c>true</c>, if the importer manager watches the specified
    /// <paramref name="folderPath"/>, else <c>false</c>.</returns>
    bool ContainsShare(string folderPath);

    /// <summary>
    /// Returns a collection of the folder paths of all shares being watched.
    /// </summary>
    ICollection<string> Shares { get;}

    /// <summary>
    /// Forces a complete import to be done on specified folder.
    /// </summary>
    /// <remarks>
    /// The folder must already be added via <see cref="AddShare"/>.
    /// </remarks>
    /// <param name="folderPath">The path of the folder to be imported.</param>
    /// <param name="refresh">If set to <c>true</c>, the importer will also refresh
    /// existing objects. Else, it only adds new objects.</param>
    /// <exception cref="ArgumentException">If the folder was not added as a share.</exception>
    void ForceImport(string folderPath, bool refresh);

    /// <summary>
    /// Forces a complete (re-)import of all shares.
    /// </summary>
    /// <param name="refresh">If set to <c>true</c>, the importer will also refresh
    /// existing objects. Else, it only adds new objects.</param>
    void ForceImport(bool refresh);

    /// <summary>
    /// Gets the meta data for a folder.
    /// </summary>
    /// <param name="folder">The folder.</param>
    /// <param name="items">The items.</param>
    void GetMetaDataFor(string folder, ref IList<IAbstractMediaItem> items);
  }
}

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
using MediaPortal.Media.MediaManager;

namespace MediaPortal.Media.Importers
{
  public interface IImporterManager
  {
    /// <summary>
    /// Gets a collection of all registered importers.
    /// </summary>
    ICollection<IImporter> Importers { get;}

    /// <summary>
    /// Gets the importer for the specific name.
    /// </summary>
    /// <param name="name">The name of the importer to retrieve.</param>
    /// <returns>Importer with the specified name, if present. If no importer with the name is
    /// registered, the method returns <c>null</c>.</returns>
    IImporter GetImporterByName(string name);

    /// <summary>
    /// Returns a collection of importers supporting the specified file <paramref name="extension"/>.
    /// </summary>
    /// <param name="extension">The extension to examine.</param>
    /// <returns>Collection of importers supporting the extension.</returns>
    ICollection<IImporter> GetImporterByExtension(string extension);

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
    /// Gets the meta data for a folder.
    /// </summary>
    /// <param name="folder">The folder.</param>
    /// <param name="items">The items.</param>
    void GetMetaDataFor(string folder, ref IList<IAbstractMediaItem> items);

  }
}

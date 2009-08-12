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

namespace MediaPortal.Database
{
  /// <summary>
  /// Provides database schema management functions. The guidelines for database setup and access must be observed.
  /// 
  /// TODO: Describe concept for database updates
  /// </summary>
  public interface IDatabase
  {
    /// <summary>
    /// Gets a list of all named sub schemas which are currently active in the database.
    /// </summary>
    /// <returns>Collection of sub schema names.</returns>
    ICollection<string> GetDatabaseSubSchemas();

    /// <summary>
    /// Gets the managed version number for the sub module's database schema which was created previously by method
    /// <see cref="UpdateSubSchema"/> for the <paramref name="subSchemaName"/>'s ID.
    /// </summary>
    /// <param name="subSchemaName">Identificator for the sub schema whose version is requested.</param>
    /// <param name="versionMajor">Current major version number of the sub schema of the given
    /// <paramref name="subSchemaName"/>.</param>
    /// <param name="versionMinor">Current minor version number of the sub schema of the given
    /// <paramref name="subSchemaName"/>.</param>
    /// <returns><c>true</c>, if the sub schema with the given <paramref name="subSchemaName"/> has a version entry in
    /// this database, i.e. if it ran a schema setup script before.</returns>
    bool GetSubSchemaVersion(string subSchemaName, out int versionMajor, out int versionMinor);

    /// <summary>
    /// Creates or updates the sub schema of the given <paramref name="subSchemaName"/> and sets its major and minor version
    /// number.
    /// The caller should first call <see cref="GetSubSchemaVersion"/> and decide which creation or update script to use.
    /// </summary>
    /// <param name="subSchemaName">Identificator for the sub schema which will be created/updated.</param>
    /// <param name="currentVersionMajor">Current major version number of the schema to update by the
    /// <paramref name="updateScript"/>.</param>
    /// <param name="currentVersionMinor">Current minor version number of the schema to update by the
    /// <paramref name="updateScript"/>.</param>
    /// <param name="updateScript">Script to create or update the sub schema with the given <paramref name="subSchemaName"/>
    /// to its current version [<paramref name="newVersionMajor"/>].[<paramref name="newVersionMinor"/>].</param>
    /// <param name="newVersionMajor">Major version number of the new schema.</param>
    /// <param name="newVersionMinor">Minor version number of the new schema.</param>
    /// <exception cref="ArgumentException">If the specified <paramref name="currentVersionMajor"/> and
    /// <paramref name="currentVersionMinor"/> don't match the current sub schema's version.</exception>
    void UpdateSubSchema(string subSchemaName, int currentVersionMajor, int currentVersionMinor,
        string updateScript, int newVersionMajor, int newVersionMinor);
  }
}

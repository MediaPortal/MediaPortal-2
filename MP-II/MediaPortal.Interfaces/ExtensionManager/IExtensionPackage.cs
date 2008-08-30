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

namespace MediaPortal.Core.ExtensionManager
{
  /// <summary>
  /// Interface to a single MPI object, manage loading mpi package
  /// </summary>
  public interface IExtensionPackage
  {

    string Name { get; set; }

    string FileName { get; set; }
    /// <summary>
    /// Gets or sets the GUID, unic for every package 
    /// </summary>
    /// <value>The id.</value>
    string PackageId { get; set; }

    string ExtensionId { get; set; }

    string Version { get; set; }

    string VersionType { get; set; }

    string ExtensionType { get; set; }

    bool Load(string filename);

    string Author { get; set; }

    string Description { get; set; }


    //List<IMPIFileItem> Items { get; set; }
  }
}

#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System.Collections.Generic;

namespace MediaPortal.Plugins.MediaServer.Objects
{
  /// <summary>
  /// This is a derived class of object used to represent containers e.g. a music album.
  /// </summary>
  public interface IDirectoryContainer : IDirectoryObject
  {
    /// <summary>
    /// Child count for the object. Applies to containers only.
    /// </summary>
    [DirectoryProperty("@childCount", Required = false)]
    int ChildCount { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [DirectoryProperty("upnp:createClass", Required = false)]
    IList<IDirectoryCreateClass> CreateClass { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [DirectoryProperty("upnp:searchClass", Required = false)]
    IList<IDirectorySearchClass> SearchClass { get; set; }

    /// <summary>
    /// When true, the ability to perform a Search() action under a container is enabled, otherwise a Search() under that container will return no results. The default value of this attribute when it is absent on a container is false
    /// </summary>
    [DirectoryProperty("@searchable", Required = false)]
    bool Searchable { get; set; }
  }
}
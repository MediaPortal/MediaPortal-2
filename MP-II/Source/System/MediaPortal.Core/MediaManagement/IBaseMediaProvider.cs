#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Provide access to physical media files from arbitrary sources which can be specified by a path.
  /// </summary>
  public interface IBaseMediaProvider : IMediaProvider
  {
    /// <summary>
    /// Returns the information if the given <paramref name="path"/> is a valid resource path in this provider.
    /// </summary>
    /// <param name="path">Path to evaluate.</param>
    /// <returns><c>true</c>, if the given <paramref name="path"/> exists (i.e. can be accessed by this provider),
    /// else <c>false</c>.</returns>
    bool IsResource(string path);

    /// <summary>
    /// Creates a resource accessor for the given <paramref name="path"/>.
    /// </summary>
    /// <param name="path">Path to be accessed by the result resource accessor.</param>
    /// <returns>Resource accessor instance.</returns>
    /// <exception cref="ArgumentException">If the given <paramref name="path"/> is not a valid path or if the resource
    /// described by the path doesn't exist.</exception>
    IResourceAccessor CreateMediaItemAccessor(string path);
  }
}

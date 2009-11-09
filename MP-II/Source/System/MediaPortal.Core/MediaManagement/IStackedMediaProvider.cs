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

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Interface to provide access to media files which are read from a resource accessor provided by another media provider.
  /// </summary>
  /// <remarks>
  /// This interface behaves the same way as its pendant <see cref="IBaseMediaProvider"/>, except that all media access methods need
  /// an additional parameter for the base input stream.
  /// </remarks>
  public interface IStackedMediaProvider : IMediaProvider
  {
    /// <summary>
    /// Creates a resource accessor for the given <paramref name="path"/>, interpreted in the given
    /// <paramref name="baseResourceAccessor"/>.
    /// </summary>
    /// <param name="baseResourceAccessor">Resource accessor this provider should take as input.</param>
    /// <param name="path">Path to be accessed by the returned resource accessor.</param>
    /// <returns>Resource accessor instance.</returns>
    /// <exception cref="ArgumentException">If the given <paramref name="path"/> is not a valid path.</exception>
    IResourceAccessor CreateResourceAccessor(IResourceAccessor baseResourceAccessor, string path);
  }
}

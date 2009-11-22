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
  /// Interface to provide access to media files which are read from a resource accessor provided by another media provider.
  /// </summary>
  /// <remarks>
  /// MP-II supports chaining of media providers. A chained media provider reads its input data from another media provider,
  /// which itself can be a base media provider or another chained media provider.
  /// This interface provides method a <see cref="CreateResourceAccessor"/> as well as interface
  /// <see cref="IBaseMediaProvider"/>, except that it needs an additional parameter for the base resource which provides
  /// the input stream for this provider.
  /// </remarks>
  public interface IChainedMediaProvider : IMediaProvider
  {
    /// <summary>
    /// Returns the information if this chained media provider can use the given
    /// <paramref name="potentialBaseResourceAccessor"/> as base resource accessor for providing a file system out of the
    /// input resource.
    /// </summary>
    /// <returns><c>true</c> if the given resource accessor can be used to chain this provider to, else <c>false</c></returns>
    bool CanChainUp(IResourceAccessor potentialBaseResourceAccessor);

    /// <summary>
    /// Returns the information if the given <paramref name="path"/> is a valid resource path in this provider, interpreted
    /// in the given <paramref name="baseResourceAccessor"/>.
    /// </summary>
    /// <param name="baseResourceAccessor">Resource accessor for the base resource, this provider should take as
    /// input.</param>
    /// <param name="path">Path to evaluate.</param>
    /// <returns><c>true</c>, if the given <paramref name="path"/> exists (i.e. can be accessed by this provider),
    /// else <c>false</c>.</returns>
    bool IsResource(IResourceAccessor baseResourceAccessor, string path);

    /// <summary>
    /// Creates a resource accessor for the given <paramref name="path"/>, interpreted in the given
    /// <paramref name="baseResourceAccessor"/>.
    /// </summary>
    /// <param name="baseResourceAccessor">Resource accessor for the base resource, this provider should take as
    /// input.</param>
    /// <param name="path">Path to be accessed by the returned resource accessor.</param>
    /// <returns>Resource accessor instance or <c>null</c>, if the given <paramref name="baseResourceAccessor"/> cannot
    /// be used to chain this media provider up. The returned resource accessor may be of any interface derived
    /// from <see cref="IResourceAccessor"/>, i.e. a file system provider will return a resource accessor of interface
    /// <see cref="IFileSystemResourceAccessor"/>.</returns>
    /// <exception cref="ArgumentException">If the given <paramref name="path"/> is not a valid path or if the resource
    /// described by the path doesn't exist in the <paramref name="baseResourceAccessor"/>.</exception>
    IResourceAccessor CreateResourceAccessor(IResourceAccessor baseResourceAccessor, string path);
  }
}

#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

namespace MediaPortal.Common.ResourceAccess
{
  /// <summary>
  /// Interface to provide access to media files which are read from a resource accessor provided by another resource provider.
  /// </summary>
  /// <remarks>
  /// MP2 supports chains of resource providers. A chained resource provider reads its input data from another resource provider,
  /// which itself can be a base resource provider or another chained resource provider.
  /// </remarks>
  public interface IChainedResourceProvider : IResourceProvider
  {
    /// <summary>
    /// Tries to use the given <paramref name="potentialBaseResourceAccessor"/> as base resource accessor for providing a file system
    /// out of the input resource.
    /// </summary>
    /// <param name="potentialBaseResourceAccessor">Resource accessor for the base resource, this provider should take as input.
    /// The ownership of the given base resource accessor remains at the caller.</param>
    /// <param name="path">Local provider path to the resource in the chained filesystem to return (e.g. <c>"/"</c>).</param>
    /// <param name="resultResourceAccessor">Resource accessor in the chained file system at the given <paramref name="path"/>.
    /// This parameter is only set to a sensible value if this method returns <c>true</c>.</param>
    /// <returns><c>true</c> if this provider could successfully chain up onto the given resource accessor, else <c>false</c></returns>
    bool TryChainUp(IFileSystemResourceAccessor potentialBaseResourceAccessor, string path, out IFileSystemResourceAccessor resultResourceAccessor);

    /// <summary>
    /// Returns the information if the given <paramref name="path"/> is a valid resource path in this provider, interpreted
    /// in the given <paramref name="baseResourceAccessor"/>.
    /// </summary>
    /// <param name="baseResourceAccessor">Resource accessor for the base resource, this provider should take as input.
    /// The base resource accessor must not be disposed by this method!</param>
    /// <param name="path">Local provider path to evaluate.</param>
    /// <returns><c>true</c>, if the given <paramref name="path"/> exists (i.e. can be accessed by this provider),
    /// else <c>false</c>.</returns>
    bool IsResource(IFileSystemResourceAccessor baseResourceAccessor, string path);
  }
}

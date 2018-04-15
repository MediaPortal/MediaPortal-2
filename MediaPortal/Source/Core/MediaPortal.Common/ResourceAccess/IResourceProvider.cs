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

using MediaPortal.Common.MediaManagement;

namespace MediaPortal.Common.ResourceAccess
{
  /// <summary>
  /// Interface to provide access to physical media files.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This interface is the root interface for all resource providers. Resource providers are separated into
  /// <see cref="IBaseResourceProvider"/>s and <see cref="IChainedResourceProvider"/>s. See their interface docs for more
  /// information.
  /// </para>
  /// <para>
  /// The resource provider is partitioned in its metadata part (see <see cref="Metadata"/>) and this worker class.
  /// </para>
  /// </remarks>
  public interface IResourceProvider
  {
    /// <summary>
    /// Metadata descriptor for this resource provider.
    /// </summary>
    ResourceProviderMetadata Metadata { get; }
  }
}

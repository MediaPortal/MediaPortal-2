#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

using System;
using System.IO;

namespace MediaPortal.Common.MediaManagement.ResourceAccess
{
  /// <summary>
  /// Base methods of the local filesystem resource provider which are needed in the media accessor.
  /// </summary>
  /// <remarks>
  /// The local filesystem provider is at some locations the most important (default-) resource provider. The media manager
  /// for example builds its default shares on directories in the local filesystem. Because all resource providers come
  /// into the system by plugins, we cannot access methods in classes provided by the local filesystem provider plugin
  /// directly. Instead, we use this base class with common static methods in the core which is by design the base class for
  /// the local filesystem resource provider.
  /// </remarks>
  public class LocalFsResourceProviderBase
  {
    #region Public constants

    /// <summary>
    /// GUID string for the local filesystem resource provider.
    /// </summary>
    protected const string LOCAL_FS_RESOURCE_PROVIDER_ID_STR = "{E88E64A8-0233-4fdf-BA27-0B44C6A39AE9}";

    /// <summary>
    /// Local filesystem resource provider GUID.
    /// </summary>
    public static Guid LOCAL_FS_RESOURCE_PROVIDER_ID = new Guid(LOCAL_FS_RESOURCE_PROVIDER_ID_STR);

    #endregion

    #region Public methods

    /// <summary>
    /// Transforms a path from the local filesystem resource provider to a DOS path.
    /// </summary>
    /// <param name="providerPath">Path which is valid in the local filesystem resource provider.
    /// The specified resource may not exist in filesystem.</param>
    /// <returns></returns>
    public static string ToDosPath(string providerPath)
    {
      if (string.IsNullOrEmpty(providerPath) || providerPath == "/")
        return string.Empty;
      if (providerPath.StartsWith("/"))
        providerPath = providerPath.Substring(1);
      return providerPath.Replace('/', Path.DirectorySeparatorChar);
    }


    /// <summary>
    /// Transforms a DOS path to a path which can be used in the local filesystem resource provider.
    /// </summary>
    /// <param name="dosPath">DOS or UNC path.</param>
    /// <returns>Path which is valid in the local filesystem resource provider.</returns>
    public static string ToProviderPath(string dosPath)
    {
      dosPath = dosPath.Replace(Path.DirectorySeparatorChar, '/');
      return dosPath.StartsWith("/") ? dosPath : "/" + dosPath;
    }

    /// <summary>
    /// Returns a <see cref="ResourcePath"/> instance which points to the given DOS path.
    /// </summary>
    /// <param name="dosPath">DOS path to be wrapped.</param>
    /// <returns>Resource path instance for the given DOS path.</returns>
    public static ResourcePath ToResourcePath(string dosPath)
    {
      return ResourcePath.BuildBaseProviderPath(LOCAL_FS_RESOURCE_PROVIDER_ID, "/" + dosPath.Replace(Path.DirectorySeparatorChar, '/'));
    }

    #endregion
  }
}

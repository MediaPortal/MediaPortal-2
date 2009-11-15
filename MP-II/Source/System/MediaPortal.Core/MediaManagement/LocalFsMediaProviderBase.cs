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
using System.IO;

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Base methods of the local filesystem media provider which are needed in the media accessor.
  /// </summary>
  /// <remarks>
  /// The local filesystem provider is at some locations the most important (default-) media provider. The media manager
  /// for example builds its default shares on directories in the local filesystem. Because all media providers come
  /// into the system by plugins, we cannot access methods in classes provided by the local filesystem provider plugin
  /// directly. Instead, we use this base class with common static methods in the core which is by design the base class for
  /// the local filesystem media provider.
  /// </remarks>
  public class LocalFsMediaProviderBase
  {
    #region Public constants

    /// <summary>
    /// GUID string for the local filesystem media provider.
    /// </summary>
    protected const string LOCAL_FS_MEDIA_PROVIDER_ID_STR = "{E88E64A8-0233-4fdf-BA27-0B44C6A39AE9}";

    /// <summary>
    /// Local filesystem media provider GUID.
    /// </summary>
    public static Guid LOCAL_FS_MEDIA_PROVIDER_ID = new Guid(LOCAL_FS_MEDIA_PROVIDER_ID_STR);

    #endregion

    #region Public methods

    public static string ToDosPath(string providerPath)
    {
      if (string.IsNullOrEmpty(providerPath) || providerPath == "/" || !providerPath.StartsWith("/"))
        return string.Empty;
      providerPath = providerPath.Substring(1);
      return providerPath.Replace('/', Path.DirectorySeparatorChar);
    }

    public static string ToProviderPath(string dosPath)
    {
      return "/" + dosPath.Replace(Path.DirectorySeparatorChar, '/');
    }

    public static ResourcePath ToProviderResourcePath(string dosPath)
    {
      return ResourcePath.BuildBaseProviderPath(LOCAL_FS_MEDIA_PROVIDER_ID, "/" + dosPath.Replace(Path.DirectorySeparatorChar, '/'));
    }

    #endregion
  }
}

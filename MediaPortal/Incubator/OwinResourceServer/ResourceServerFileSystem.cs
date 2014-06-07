#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Collections.Generic;
using MediaPortal.Common.ResourceAccess;
using Microsoft.Owin.FileSystems;

namespace MediaPortal.Plugins.WebServices.OwinResourceServer
{
  /// <summary>
  /// Implementation of <see cref="IFileSystem"/> providing access via ResourceAccessors
  /// </summary>
  class ResourceServerFileSystem : IFileSystem
  {

    #region IFileSystem implementation

    /// <summary>
    /// Tries to get a <see cref="ResourceServerFileInfo"/> for a given ResourcePath
    /// </summary>
    /// <param name="subpath">String in the form "/[ResourcePath]"</param>
    /// <param name="fileInfo">A new <see cref="ResourceServerFileInfo"/> object or <c>null</c></param>
    /// <returns><c>true</c> if a valid <see cref="ResourceServerFileInfo"/> could be created, else <c>false</c></returns>
    /// <remarks>
    /// This class requires that the subpath contains a string in the form "/[ResourcePath]"
    /// </remarks>
    public bool TryGetFileInfo(string subpath, out IFileInfo fileInfo)
    {
      // First try to make a ResourcePath out of subpath. If it doesn't work, return false
      fileInfo = null;
      ResourcePath resourcePath;
      try
      {
        // We filter out the first '/' character, which was added by the ResourceQueryToPathMiddleware
        // to make the ResourcePath look like a valid RequestPath.
        resourcePath = ResourcePath.Deserialize(subpath.Substring(1));
      }
      catch (Exception)
      {
        return false;
      }
      
      // Second try to make an IResourceAccessor our of the ResourcePath. If it doesn't work, return false
      IResourceAccessor ra;
      if (!resourcePath.TryCreateLocalResourceAccessor(out ra))
        return false;
      
      // Third try to make an IFileSystemResourceAccessor out of the IResourceAccessor. If it doesn't work,
      // dipose the IResourceAccessor and return false
      var fsra = ra as IFileSystemResourceAccessor;
      if (fsra == null)
      {
        ra.Dispose();
        return false;
      }
      
      // Now we are sure that subpath contains an IFileSystemResourceAccessor. Dipose it and
      // create a ResourceServerFileInfo for it.
      fsra.Dispose();
      fileInfo = new ResourceServerFileInfo(resourcePath);
      return true;
    }

    /// <summary>
    /// We do not support directory browsing, yet.
    /// </summary>
    /// <param name="subpath"></param>
    /// <param name="contents"></param>
    /// <returns></returns>
    public bool TryGetDirectoryContents(string subpath, out IEnumerable<IFileInfo> contents)
    {
      contents = null;
      return false;
    }

    #endregion

  }
}

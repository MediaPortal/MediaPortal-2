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
using System.IO;
using MediaPortal.Common.ResourceAccess;
using Microsoft.Owin.FileSystems;

namespace MediaPortal.Plugins.WebServices.OwinResourceServer
{  
  /// <summary>
  /// Implementation of <see cref="IFileInfo"/> providing access via ResourceAccessors
  /// </summary>
  /// <remarks>
  /// Objects of this type are created by the StaticFileMiddleware. We have no control to Dispose()
  /// these objects. We therefore only keep a <see cref="ResourcePath"/> as field, not an
  /// <see cref="IFileSystemResourceAccessor"/>. The latter is created and disposed on each access to this class.
  /// This class requires that for the provided ResourcePath an IFileSystemResourceAccessor can be obtained. An
  /// IResourceAccessor is not sufficient. This class will throw exceptions if this is not the case. The respective
  /// <see cref="IFileSystem"/> implementation has to ensure that this is the case.
  /// </remarks>
  class ResourceServerFileInfo : IFileInfo
  {
    #region Fields

    private readonly ResourcePath _resourcePath;

    #endregion

    #region Constructor

    public ResourceServerFileInfo(ResourcePath resourcePath)
    {
      _resourcePath = resourcePath;
    }

    #endregion

    #region Private methods

    private IFileSystemResourceAccessor GetFsra()
    {
      IResourceAccessor ra;
      _resourcePath.TryCreateLocalResourceAccessor(out ra);
      var fsra = ra as IFileSystemResourceAccessor;
      if (fsra == null)
      {
        ra.Dispose();
        return null;
      }
      return fsra;
    }

    #endregion

    #region IFileInfo Implementations

    public Stream CreateReadStream()
    {
      using (var fsra = GetFsra())
        return fsra.OpenRead();
    }

    public long Length
    {
      get
      {
        using (var fsra = GetFsra())
          return fsra.Size;
      }
    }

    public string PhysicalPath
    {
      get
      {
        // avoid direct access
        return null;
      }
    }

    public string Name
    {
      get
      {
        using (var fsra = GetFsra())
          return fsra.ResourceName;
      }
    }

    public DateTime LastModified
    {
      get
      {
        using (var fsra = GetFsra())
          return fsra.LastChanged;
      }
    }

    public bool IsDirectory
    {
      get
      {
        using (var fsra = GetFsra())
          return !fsra.IsFile;
      }
    }

    #endregion
  }
}

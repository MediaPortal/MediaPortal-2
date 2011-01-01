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
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Core.SystemResolver;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Core.Services.MediaManagement
{
  public class ResourceLocator : IResourceLocator
  {
    #region Protected fields

    protected SystemName _nativeSystem;
    protected ResourcePath _nativeResourcePath;

    #endregion

    public ResourceLocator(SystemName system, ResourcePath nativeResourcePath)
    {
      _nativeSystem = system;
      _nativeResourcePath = nativeResourcePath;
    }

    public ResourceLocator CreateResourceLocator(string systemId, ResourcePath nativeResourcePath)
    {
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      SystemName system = systemResolver.GetSystemNameForSystemId(systemId);
      if (system == null)
        return null;
      return new ResourceLocator(system, nativeResourcePath);
    }

    public SystemName NativeSystem
    {
      get { return _nativeSystem; }
    }

    public ResourcePath NativeResourcePath
    {
      get { return _nativeResourcePath; }
    }

    public IResourceAccessor CreateAccessor()
    {
      if (_nativeSystem.IsLocalSystem())
      {
        _nativeResourcePath.CheckValidLocalPath();
        return _nativeResourcePath.CreateLocalResourceAccessor();
      }
      IFileSystemResourceAccessor fsra;
      if (RemoteFileSystemResourceAccessor.ConnectFileSystem(_nativeSystem, _nativeResourcePath, out fsra))
        return fsra;
      IResourceAccessor ra;
      if (RemoteFileResourceAccessor.ConnectFile(this, out ra))
        return ra;
      throw new IllegalCallException("Cannot create resource accessor for resource location '{0}' at host '{1}'", _nativeResourcePath, _nativeSystem);
    }

    public ILocalFsResourceAccessor CreateLocalFsAccessor()
    {
      IResourceAccessor accessor = CreateAccessor();
      //// Try to get an ILocalFsResourceAccessor
      ILocalFsResourceAccessor result = accessor as ILocalFsResourceAccessor;
      if (result != null)
        // Simple case: The media item is located in the local file system or the media provider returns
        // an ILocalFsResourceAccessor from elsewhere - simply return it
        return result;
      try
      {
        // Set up a resource bridge mapping the remote or complex resource to a local file
        return new RemoteResourceToLocalFsAccessBridge(accessor);
      }
      catch (Exception)
      {
        accessor.Dispose();
        throw;
      }
    }

    #region Base overrides

    public override string ToString()
    {
      return string.Format("Resource '{0}' at system '{1}", _nativeResourcePath, _nativeSystem);
    }

    #endregion
  }
}
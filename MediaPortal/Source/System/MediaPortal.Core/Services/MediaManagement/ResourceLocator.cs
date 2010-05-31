#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
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

    public SystemName NativeSystem
    {
      get { return _nativeSystem; }
    }

    public ResourcePath NativeResourcePath
    {
      get { return _nativeResourcePath; }
    }

    // Implementation hint: This method is responsible for creating all temporary connections/mappings/resources to access
    // the media item specified by this instance. It is also responsible for providing an ITidyUpExecutor for cleaning up
    // the resources which have been set up.
    public IResourceAccessor CreateAccessor()
    {
      if (!_nativeSystem.IsLocalSystem())
        // TODO: Implement resource accessors for remote media resources
        throw new NotImplementedException("ResourceLocator.CreateAccessor for remote media items is not implemented yet");
      _nativeResourcePath.CheckValidLocalPath();
      return _nativeResourcePath.CreateLocalMediaItemAccessor();
    }

    // Implementation hint: This method is responsible for creating all temporary connections/mappings/resources to access
    // the media item specified by this instance. It is also responsible for providing an ITidyUpExecutor for cleaning up
    // the resources which have been set up.
    public ILocalFsResourceAccessor CreateLocalFsAccessor()
    {
      IResourceAccessor accessor = CreateAccessor();
      // Try to get an ILocalFsResourceAccessor
      ILocalFsResourceAccessor result = accessor as ILocalFsResourceAccessor;
      if (result != null)
        // Simple case: The media item is located in the local file system or the media provider returns
        // an ILocalFsResourceAccessor from elsewhere - simply return it
        return result;
      try
      {
        if (!accessor.IsFile)
          throw new IllegalCallException("This resource locator doesn't denote a file resource");
        // Set up a resource bridge mapping the remote or complex resource to a local file
        return new RemoteResourceToLocalFsAccessBridge(accessor);
      }
      catch (Exception)
      {
        accessor.Dispose();
        throw;
      }
    }
  }
}
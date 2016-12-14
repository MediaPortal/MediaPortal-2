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

using MediaPortal.Common.General;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.RemoteResourceProvider;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Common.Services.ResourceAccess
{
  public class ResourceLocator : IResourceLocator
  {
    #region Protected fields

    protected string _nativeSystemId;
    protected ResourcePath _nativeResourcePath;

    #endregion
    
    /// <summary>
    /// Convenience constructor to create a resource locator for the local system.
    /// </summary>
    /// <param name="localResourcePath">Path of the desired resource in the local system.</param>
    public ResourceLocator(ResourcePath localResourcePath)
    {
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      _nativeSystemId = systemResolver.LocalSystemId;
      _nativeResourcePath = localResourcePath;
    }

    /// <summary>
    /// Creates a new resource accessor for the resource located in the system with the given <paramref name="systemId"/> with the
    /// given <paramref name="nativeResourcePath"/>.
    /// </summary>
    /// <param name="systemId">Id of the MP2 system where the desirec resource is located.</param>
    /// <param name="nativeResourcePath">Path of the desired resource in the system with the given <paramref name="systemId"/>.</param>
    public ResourceLocator(string systemId, ResourcePath nativeResourcePath)
    {
      _nativeSystemId = systemId;
      _nativeResourcePath = nativeResourcePath;
    }

    public string NativeSystemId
    {
      get { return _nativeSystemId; }
    }

    public ResourcePath NativeResourcePath
    {
      get { return _nativeResourcePath; }
    }

    public IResourceAccessor CreateAccessor()
    {
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      IResourceAccessor result;
      if (_nativeResourcePath.IsNetworkResource)
      {
        if (_nativeResourcePath.TryCreateLocalResourceAccessor(out result))
          return result;
      }
      SystemName nativeSystem = systemResolver.GetSystemNameForSystemId(_nativeSystemId);
      if (nativeSystem == null)
        throw new IllegalCallException("Cannot create resource accessor for resource location '{0}' at system '{1}': System is not available", _nativeResourcePath, _nativeSystemId);
      // Try to access resource locally. This might work if we have the correct resource providers installed.
      if (nativeSystem.IsLocalSystem() && _nativeResourcePath.IsValidLocalPath && _nativeResourcePath.TryCreateLocalResourceAccessor(out result))
        return result;
      IFileSystemResourceAccessor fsra;
      if (RemoteFileSystemResourceAccessor.ConnectFileSystem(_nativeSystemId, _nativeResourcePath, out fsra))
        return fsra;
      throw new IllegalCallException("Cannot create resource accessor for resource location '{0}' at system '{1}'", _nativeResourcePath, _nativeSystemId);
    }

    public bool TryCreateLocalFsAccessor(out ILocalFsResourceAccessor localFsResourceAccessor)
    {
      IResourceAccessor accessor = CreateAccessor();
      IFileSystemResourceAccessor fsra = accessor as IFileSystemResourceAccessor;
      if (fsra == null)
      {
        accessor.Dispose();
        localFsResourceAccessor = null;
        return false;
      }
      try
      {
        localFsResourceAccessor = StreamedResourceToLocalFsAccessBridge.StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(fsra);
        return true;
      }
      catch
      {
        accessor.Dispose();
        throw;
      }
    }

    #region Base overrides

    public override string ToString()
    {
      return string.Format("Resource '{0}' at system '{1}", _nativeResourcePath, _nativeSystemId);
    }

    #endregion
  }
}
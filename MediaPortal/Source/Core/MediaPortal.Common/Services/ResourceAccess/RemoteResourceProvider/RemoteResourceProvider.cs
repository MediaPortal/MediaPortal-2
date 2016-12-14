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

using System;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Common.Services.ResourceAccess.RemoteResourceProvider
{
  /// <summary>
  /// Resource provider implementation for accessing remote resources.
  /// </summary>
  public class RemoteResourceProvider : IBaseResourceProvider
  {
    #region Consts

    protected const string REMOTE_RESOURCE_PROVIDER_ID_STR = "{79BBC72B-D721-4273-938E-AE010E8855BA}";
    public static Guid REMOTE_RESOURCE_PROVIDER_ID = new Guid(REMOTE_RESOURCE_PROVIDER_ID_STR);

    protected const string RES_RESOURCE_PROVIDER_NAME = "[RemoteResourceProvider.Name]";
    protected const string RES_RESOURCE_PROVIDER_DESCRIPTION = "[RemoteResourceProvider.Description]";

    #endregion

    #region Protected fields

    protected ResourceProviderMetadata _metadata;

    #endregion

    #region Ctor

    public RemoteResourceProvider()
    {
      _metadata = new ResourceProviderMetadata(REMOTE_RESOURCE_PROVIDER_ID, RES_RESOURCE_PROVIDER_NAME, RES_RESOURCE_PROVIDER_DESCRIPTION, true, false);
    }

    #endregion

    internal static string BuildProviderPath(string nativeSystemId, ResourcePath nativeResourcePath)
    {
      return nativeSystemId + ":" + nativeResourcePath.Serialize();
    }

    internal static bool TryExtractSystemAndPath(string providerPath, out string systemId, out ResourcePath nativeResourcePath)
    {
      systemId = null;
      nativeResourcePath = null;
      int sepIndex = providerPath.IndexOf(':');
      if (sepIndex < 1)
        return false;
      systemId = providerPath.Substring(0, sepIndex);
      try
      {
        nativeResourcePath = ResourcePath.Deserialize(providerPath.Substring(sepIndex + 1));
        return true;
      }
      catch (ArgumentException)
      {
        return false;
      }
    }

    #region IBaseResourceProvider implementation

    public ResourceProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool IsResource(string path)
    {
      IResourceAccessor ra;
      if (TryCreateResourceAccessor(path, out ra))
      {
        ra.Dispose();
        return true;
      }
      return false;
    }

    public bool TryCreateResourceAccessor(string path, out IResourceAccessor result)
    {
      string nativeSystemId;
      ResourcePath nativeResourcePath;
      if (!TryExtractSystemAndPath(path, out nativeSystemId, out nativeResourcePath))
        throw new InvalidDataException("Path '{0}' is not a valid path for remote resource provider", path);
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      SystemName nativeSystem = systemResolver.GetSystemNameForSystemId(nativeSystemId);
      if (nativeSystem == null)
        throw new IllegalCallException("Cannot create resource accessor for resource location '{0}' at system '{1}': System is not available", nativeResourcePath, nativeSystemId);
      // Try to access resource locally. This might work if we have the correct resource providers installed.
      if (nativeSystem.IsLocalSystem() && nativeResourcePath.IsValidLocalPath && nativeResourcePath.TryCreateLocalResourceAccessor(out result))
        return true;
      IFileSystemResourceAccessor fsra;
      if (RemoteFileSystemResourceAccessor.ConnectFileSystem(nativeSystemId, nativeResourcePath, out fsra))
      {
        result = fsra;
        return true;
      }
      result = null;
      return false;
    }

    public ResourcePath ExpandResourcePathFromString(string pathStr)
    {
      // Not supported
      return null;
    }

    #endregion
  }
}

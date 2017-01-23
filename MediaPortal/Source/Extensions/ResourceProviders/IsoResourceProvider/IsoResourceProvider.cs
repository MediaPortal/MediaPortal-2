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
using System.Collections.Generic;
using System.IO;
using DiscUtils;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Extensions.ResourceProviders.IsoResourceProvider
{
  /// <summary>
  /// Resource provider implementation for ISO files.
  /// </summary>
  /// <remarks>
  /// Provider paths used by this resource provider are in standard provider path form and thus can be processed by
  /// the methods in <see cref="ProviderPathHelper"/>.
  /// </remarks>
  public class IsoResourceProvider : IChainedResourceProvider
  {
    #region Consts

    protected const string ISO_RESOURCE_PROVIDER_ID_STR = "{112728B1-F71D-4284-9E5C-3462E8D3C74D}";
    public static Guid ISO_RESOURCE_PROVIDER_ID = new Guid(ISO_RESOURCE_PROVIDER_ID_STR);

    protected const string RES_RESOURCE_PROVIDER_NAME = "[IsoResourceProvider.Name]";
    protected const string RES_RESOURCE_PROVIDER_DESCRIPTION = "[IsoResourceProvider.Description]";

    #endregion

    #region Protected fields

    protected ResourceProviderMetadata _metadata;

    protected object _syncObj = new object();
    internal IDictionary<string, IsoResourceProxy> _isoUsages = new Dictionary<string, IsoResourceProxy>(); // Keys to proxy objects

    #endregion

    #region Ctor

    public IsoResourceProvider()
    {
      _metadata = new ResourceProviderMetadata(ISO_RESOURCE_PROVIDER_ID, RES_RESOURCE_PROVIDER_NAME, RES_RESOURCE_PROVIDER_DESCRIPTION, false, false);
    }

    #endregion

    void OnIsoResourceProxyOrphaned(IsoResourceProxy proxy)
    {
      lock (_syncObj)
      {
        if (proxy.UsageCount > 0)
          // Double check if the proxy was reused when the lock was not set
          return;
        _isoUsages.Remove(proxy.Key);
        proxy.Dispose();
      }
    }

    internal IsoResourceProxy CreateIsoResourceProxy(string key, IFileSystemResourceAccessor isoFileResourceAccessor)
    {
      IsoResourceProxy result = new IsoResourceProxy(key, isoFileResourceAccessor);
      result.Orphaned += OnIsoResourceProxyOrphaned;
      return result;
    }

    #region IResourceProvider implementation

    public ResourceProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    #endregion

    #region IChainedResourceProvider implementation

    public bool TryChainUp(IFileSystemResourceAccessor potentialBaseResourceAccessor, string path, out IFileSystemResourceAccessor resultResourceAccessor)
    {
      resultResourceAccessor = null;
      string resourcePathName = potentialBaseResourceAccessor.ResourcePathName;
      if (string.IsNullOrEmpty(resourcePathName) || !potentialBaseResourceAccessor.IsFile ||
          !".iso".Equals(DosPathHelper.GetExtension(resourcePathName), StringComparison.OrdinalIgnoreCase))
        return false;

      lock (_syncObj)
      {
        string key = potentialBaseResourceAccessor.CanonicalLocalResourcePath.Serialize();
        try
        {
          IsoResourceProxy proxy;
          if (!_isoUsages.TryGetValue(key, out proxy))
            _isoUsages.Add(key, proxy = CreateIsoResourceProxy(key, potentialBaseResourceAccessor));
          resultResourceAccessor = new IsoResourceAccessor(this, proxy, path);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("IsoResourceProvider: Error chaining up to '{0}'", e, potentialBaseResourceAccessor.CanonicalLocalResourcePath);
          return false;
        }
        return true;
      }
    }

    public bool IsResource(IFileSystemResourceAccessor baseResourceAccessor, string path)
    {
      string resourceName = baseResourceAccessor.ResourceName;
      if (string.IsNullOrEmpty(resourceName) || !baseResourceAccessor.IsFile)
        return false;

      // Test if we have already an ISO proxy for that ISO file...
      lock (_syncObj)
      {
        string key = baseResourceAccessor.CanonicalLocalResourcePath.Serialize();
        try
        {
          IsoResourceProxy proxy;
          if (_isoUsages.TryGetValue(key, out proxy))
            lock (proxy.SyncObj)
              return IsoResourceAccessor.IsResource(proxy.DiskFileSystem, path);
        }
        catch (Exception)
        {
          return false;
        }
      }

      // ... if not, test the resource in a new disk file system instance
      using (Stream underlayingStream = baseResourceAccessor.OpenRead())
      {
        try
        {
          IFileSystem diskFileSystem = IsoResourceProxy.GetFileSystem(underlayingStream);
          using (diskFileSystem as IDisposable)
            return IsoResourceAccessor.IsResource(diskFileSystem, path);
        }
        catch
        {
          return false;
        }
      }
    }

    #endregion
  }
}

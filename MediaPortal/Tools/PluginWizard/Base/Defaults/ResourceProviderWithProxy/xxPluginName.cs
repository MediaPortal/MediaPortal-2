#region Copyright (C) 2007-xxCurrentYear Team MediaPortal

/*
    Copyright (C) 2007-xxCurrentYear Team MediaPortal
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

namespace MediaPortal.Extensions.ResourceProviders.xxPluginName
{
  /// <summary>
  /// Resource provider implementation for xxx files.
  /// </summary>
  /// <remarks>
  /// Provider paths used by this resource provider are in standard provider path form and thus can be processed by
  /// the methods in <see cref="ProviderPathHelper"/>.
  /// </remarks>
  public class xxPluginName : IChainedResourceProvider
  {
    #region Consts

    protected const string RESOURCE_PROVIDER_ID_STR = "{xxPluginId}";
    public static Guid RESOURCE_PROVIDER_ID = new Guid(RESOURCE_PROVIDER_ID_STR);

    protected const string RES_RESOURCE_PROVIDER_NAME = "[xxPluginName.Name]";
    protected const string RES_RESOURCE_PROVIDER_DESCRIPTION = "[xxPluginName.Description]";

    #endregion

    #region Protected fields

    protected ResourceProviderMetadata _metadata;

    protected object _syncObj = new object();
    internal IDictionary<string, xxxResourceProxy> _proxyUsages = new Dictionary<string, xxxResourceProxy>(); // Keys to proxy objects

    #endregion

    #region Ctor

    public xxPluginName()
    {
      _metadata = new ResourceProviderMetadata(RESOURCE_PROVIDER_ID, RES_RESOURCE_PROVIDER_NAME, RES_RESOURCE_PROVIDER_DESCRIPTION, false);
    }

    #endregion

    void OnResourceProxyOrphaned(xxxResourceProxy proxy)
    {
      lock (_syncObj)
      {
        if (proxy.UsageCount > 0)
          // Double check if the proxy was reused when the lock was not set
          return;
        _proxyUsages.Remove(proxy.Key);
        proxy.Dispose();
      }
    }

    internal xxxResourceProxy CreateResourceProxy(string key, IResourceAccessor xxxFileResourceAccessor)
    {
      xxxResourceProxy result = new xxxResourceProxy(key, xxxFileResourceAccessor);
      result.Orphaned += OnResourceProxyOrphaned;
      return result;
    }

    #region IResourceProvider implementation

    public ResourceProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    #endregion

    #region IChainedResourceProvider implementation

    public bool TryChainUp(IResourceAccessor potentialBaseResourceAccessor, string path, out IResourceAccessor resultResourceAccessor)
    {
      resultResourceAccessor = null;
      string resourcePathName = potentialBaseResourceAccessor.ResourcePathName;
      if (string.IsNullOrEmpty(resourcePathName) || !potentialBaseResourceAccessor.IsFile ||
          !".xxx".Equals(DosPathHelper.GetExtension(resourcePathName), StringComparxxxn.OrdinalIgnoreCase))
        return false;

      lock (_syncObj)
      {
        string key = potentialBaseResourceAccessor.CanonicalLocalResourcePath.Serialize();
        try
        {
          xxxResourceProxy proxy;
          if (!_proxyUsages.TryGetValue(key, out proxy))
            _proxyUsages.Add(key, proxy = CreateResourceProxy(key, potentialBaseResourceAccessor));
          resultResourceAccessor = new xxxResourceAccessor(this, proxy, path);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("xxPluginName: Error chaining up to '{0}'", e, potentialBaseResourceAccessor.CanonicalLocalResourcePath);
          return false;
        }
        return true;
      }
    }

    public bool IsResource(IResourceAccessor baseResourceAccessor, string path)
    {
      string resourceName = baseResourceAccessor.ResourceName;
      if (string.IsNullOrEmpty(resourceName) || !baseResourceAccessor.IsFile)
        return false;

      // Test if we have already an xxx proxy for that xxx file...
      lock (_syncObj)
      {
        string key = baseResourceAccessor.CanonicalLocalResourcePath.Serialize();
        try
        {
          xxxResourceProxy proxy;
          if (_proxyUsages.TryGetValue(key, out proxy))
            return xxxResourceAccessor.IsResource(proxy.DiskFileSystem, path);
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
          IFileSystem diskFileSystem = xxxResourceProxy.GetFileSystem(underlayingStream);
          using (diskFileSystem as IDisposable)
            return xxxResourceAccessor.IsResource(diskFileSystem, path);
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

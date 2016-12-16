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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using ICSharpCode.SharpZipLib.Zip;

namespace MediaPortal.Extensions.ResourceProviders.ZipResourceProvider
{
  /// <summary>
  /// Resource provider implementation for the ZIP files.
  /// </summary>
  /// <remarks>
  /// Provider paths used by this resource provider are in standard provider path form and thus can be processed by
  /// the methods in <see cref="ProviderPathHelper"/>.
  /// </remarks>
  public class ZipResourceProvider : IChainedResourceProvider
  {
    #region Consts

    /// <summary>
    /// GUID string for the ZIP resource provider.
    /// </summary>
    protected const string ZIP_RESOURCE_PROVIDER_ID_STR = "{6B042DB8-69AD-4B57-B869-1BCEA4E43C77}";

    /// <summary>
    /// ZIP resource provider GUID.
    /// </summary>
    public static Guid ZIP_RESOURCE_PROVIDER_ID = new Guid(ZIP_RESOURCE_PROVIDER_ID_STR);

    protected const string RES_RESOURCE_PROVIDER_NAME = "[ZipResourceProvider.Name]";
    protected const string RES_RESOURCE_PROVIDER_DESCRIPTION = "[ZipResourceProvider.Description]";

    #endregion

    #region Protected fields

    protected ResourceProviderMetadata _metadata;

    protected object _syncObj = new object();
    internal IDictionary<string, ZipResourceProxy> _zipUsages = new Dictionary<string, ZipResourceProxy>(); // Keys to proxy objects

    #endregion

    #region Ctor

    public ZipResourceProvider()
    {
      _metadata = new ResourceProviderMetadata(ZIP_RESOURCE_PROVIDER_ID, RES_RESOURCE_PROVIDER_NAME, RES_RESOURCE_PROVIDER_DESCRIPTION, false, false);
    }

    #endregion

    void OnZipResourceProxyOrphaned(ZipResourceProxy proxy)
    {
      lock (_syncObj)
      {
        if (proxy.UsageCount > 0)
          // Double check if the proxy was reused when the lock was not set
          return;
        _zipUsages.Remove(proxy.Key);
        proxy.Dispose();
      }
    }

    internal ZipResourceProxy CreateZipResourceProxy(string key, IFileSystemResourceAccessor zipFileResourceAccessor)
    {
      ZipResourceProxy result = new ZipResourceProxy(key, zipFileResourceAccessor);
      result.Orphaned += OnZipResourceProxyOrphaned;
      return result;
    }

    #region IResourceProvider implementation

    /// <summary>
    /// Metadata descriptor for this resource provider.
    /// </summary>
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
          !".zip".Equals(DosPathHelper.GetExtension(resourcePathName), StringComparison.OrdinalIgnoreCase))
        return false;

      lock (_syncObj)
      {
        string key = potentialBaseResourceAccessor.CanonicalLocalResourcePath.Serialize();
        try
        {
          ZipResourceProxy proxy;
          if (!_zipUsages.TryGetValue(key, out proxy))
            _zipUsages.Add(key, proxy = CreateZipResourceProxy(key, potentialBaseResourceAccessor));
          resultResourceAccessor = new ZipResourceAccessor(this, proxy, path);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("ZipResourceProvider: Error chaining up to '{0}'", e, potentialBaseResourceAccessor.CanonicalLocalResourcePath);
          return false;
        }
        return true;
      }
    }

    public bool IsResource(IFileSystemResourceAccessor baseResourceAccessor, string path)
    {
      string entryPath = ZipResourceAccessor.ToEntryPath(path);

      lock (_syncObj)
      {
        string key = baseResourceAccessor.CanonicalLocalResourcePath.Serialize();
        try
        {
          ZipResourceProxy proxy;
          if (_zipUsages.TryGetValue(key, out proxy))
            return path.Equals("/") || ZipResourceAccessor.IsResource(proxy.ZipFile, entryPath);
        }
        catch (Exception)
        {
          return false;
        }
      }

      using (Stream resourceStream = baseResourceAccessor.OpenRead()) // Not sure if the ZipFile will close the stream so we dispose it here
        try
        {
          using (ZipFile zFile = new ZipFile(resourceStream))
            return path.Equals("/") || ZipResourceAccessor.IsResource(zFile, entryPath);
        }
        catch (Exception)
        {
          return false;
        }
    }

    #endregion
  }
}

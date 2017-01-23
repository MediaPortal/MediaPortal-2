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
using System.Collections.Concurrent;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider.NeighborhoodBrowser;
using MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider.Settings;
using MediaPortal.Utilities.Xml;

namespace MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider
{
  public class NetworkNeighborhoodResourceProvider : IBaseResourceProvider, IDisposable
  {
    #region Consts

    public const string NETWORK_NEIGHBORHOOD_RESOURCE_PROVIDER_ID_STR = "{03DD2DA6-4DA8-4D3E-9E55-80E3165729A3}";
    public static readonly Guid NETWORK_NEIGHBORHOOD_RESOURCE_PROVIDER_ID = new Guid(NETWORK_NEIGHBORHOOD_RESOURCE_PROVIDER_ID_STR);

    internal const string ROOT_PROVIDER_PATH = "/";

    protected const string RES_RESOURCE_PROVIDER_NAME = "[NetworkNeighborhoodResourceProvider.Name]";
    protected const string RES_RESOURCE_PROVIDER_DESCRIPTION = "[NetworkNeighborhoodResourceProvider.Description]";
    protected const ResourceProviderMetadata.SystemAffinity DEFAULT_SYSTEM_AFFINITY = ResourceProviderMetadata.SystemAffinity.Server | ResourceProviderMetadata.SystemAffinity.DetachedClient;
    protected static readonly SerializableNetworkCredential NETWORK_SERVICE_CREDENTIAL = new SerializableNetworkCredential
    {
      UserName = "NETWORK SERVICE",
      Domain = "NT AUTHORITY",
      Password = ""
    };

    #endregion

    #region Protected fields

    protected ResourceProviderMetadata _metadata;
    protected readonly INeighborhoodBrowserSerivce _browserService;
    protected readonly SettingsChangeWatcher<NetworkNeighborhoodResourceProviderSettings> _settings;
    protected readonly ConcurrentBag<ResourcePath> _registeredPaths;

    #endregion

    #region Ctor

    public NetworkNeighborhoodResourceProvider()
    {
      _metadata = new ResourceProviderMetadata(NETWORK_NEIGHBORHOOD_RESOURCE_PROVIDER_ID, RES_RESOURCE_PROVIDER_NAME, RES_RESOURCE_PROVIDER_DESCRIPTION, false, true, DEFAULT_SYSTEM_AFFINITY);
      _browserService = new NeighborhoodBrowserService();
      _settings =  new SettingsChangeWatcher<NetworkNeighborhoodResourceProviderSettings>();
      _registeredPaths = new ConcurrentBag<ResourcePath>();
      RegisterCredentials();
      _settings.SettingsChanged += (sender, args) =>
      {
        UnregisterCredentials();
        RegisterCredentials();
      };
    }

    #endregion

    #region Public properties

    public INeighborhoodBrowserSerivce BrowserService
    {
      get { return _browserService; }
    }

    public static ResourcePath RootPath
    {
      get { return ResourcePath.BuildBaseProviderPath(NETWORK_NEIGHBORHOOD_RESOURCE_PROVIDER_ID, ROOT_PROVIDER_PATH); }
    }

    #endregion

    #region Private methods

    private void RegisterCredentials()
    {
      foreach (var kvp in _settings.Settings.NetworkCredentials)
      {
        ResourcePath path;
        try
        {
          path = ResourcePath.Deserialize(kvp.Key);
        }
        catch (Exception)
        {
          ServiceRegistration.Get<ILogger>().Error("NetworkNeighborhoodResourceProvider: Malformed ResourcePath in NetworkNeighborhoodResourceProviderSettings: '{0}'", kvp.Key);
          continue;
        }
        if(ServiceRegistration.Get<IImpersonationService>().TryRegisterCredential(path, kvp.Value))
          _registeredPaths.Add(path);
        else
          ServiceRegistration.Get<ILogger>().Warn("NetworkNeighborhoodResourceProvider: Could not register credentials for ResourcePath: '{0}'", kvp.Key);
      }

      if (!_registeredPaths.Contains(RootPath))
      {
        // If there was no credential registered for the root path of the NetworkNeighborhoodResourceProvider,
        // we use the Network Service account as fallback. This is required in particular because the
        // WNetEnumNeighborhoodBrowser cannot enumerate computers in the network under the LocalSystem account.
        ServiceRegistration.Get<ILogger>().Info("NetworkNeighborhoodResourceProvider: No credential registered for the root path; using Network Service account as fallback");
        if (ServiceRegistration.Get<IImpersonationService>().TryRegisterCredential(RootPath, NETWORK_SERVICE_CREDENTIAL))
          _registeredPaths.Add(RootPath);
        else
          ServiceRegistration.Get<ILogger>().Warn("NetworkNeighborhoodResourceProvider: Could not register credentials for ResourcePath: '{0}'", RootPath);
      }
    }

    private void UnregisterCredentials()
    {
      ResourcePath path;
      while(_registeredPaths.TryTake(out path))
        ServiceRegistration.Get<IImpersonationService>().TryUnregisterCredential(path);
    }

    #endregion

    #region IBaseResourceProvider implementation

    public ResourceProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool IsResource(string path)
    {
      return NetworkNeighborhoodResourceAccessor.IsResource(path);
    }

    public bool TryCreateResourceAccessor(string path, out IResourceAccessor result)
    {
      if (!IsResource(path))
      {
        result = null;
        return false;
      }
      result = new NetworkNeighborhoodResourceAccessor(this, path);
      return true;
    }

    public ResourcePath ExpandResourcePathFromString(string pathStr)
    {
      if (string.IsNullOrEmpty(pathStr))
        return null;
      // The input string is given by the user. We can cope with three formats:
      // 1) A resource provider path which can be interpreted by the choosen resource provider itself (i.e. a path without the
      //    starting resource provider GUID)
      // 2) A resource path in the resource path syntax (i.e. {[Base-Provider-Id]}://[Base-Provider-Path])
      // 3) A dos path
      if (IsResource(pathStr))
        return new ResourcePath(new[]
          {
              new ProviderPathSegment(_metadata.ResourceProviderId, pathStr, true)
          });
      string providerPath = LocalFsResourceProviderBase.ToProviderPath(pathStr);
      if (IsResource(providerPath))
        return new ResourcePath(new[]
          {
              new ProviderPathSegment(_metadata.ResourceProviderId, providerPath, true)
          });
      try
      {
        return ResourcePath.Deserialize(pathStr);
      }
      catch (ArgumentException)
      {
        return null;
      }
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      _browserService.Dispose();
      _settings.Dispose();
      UnregisterCredentials();
    }

    #endregion
  }
}

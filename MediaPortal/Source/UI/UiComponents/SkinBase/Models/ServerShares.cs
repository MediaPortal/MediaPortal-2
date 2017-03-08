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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.SystemResolver;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Shares;
using MediaPortal.UiComponents.SkinBase.General;
using RelocationMode=MediaPortal.Common.MediaManagement.RelocationMode;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  public class ServerShares : SharesProxy
  {
    protected static ICollection<Share> _serverSharesCache = null;

    public ServerShares() : base(ShareEditMode.AddShare)
    {
      _serverSharesCache = null;
    }

    public ServerShares(Share share) : base(ShareEditMode.EditShare)
    {
      IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      string nativeSystem = serverConnectionManager.LastHomeServerName;
      if (nativeSystem == null)
      {
        SystemName systemName = systemResolver.GetSystemNameForSystemId(serverConnectionManager.HomeServerSystemId);
        if (systemName != null)
          nativeSystem = systemName.HostName;
      }
      if (nativeSystem == null)
        nativeSystem = serverConnectionManager.HomeServerSystemId;
      InitializePropertiesWithShare(share, nativeSystem);
    }

    public override string ConfigShareTitle
    {
      get { return _editMode == ShareEditMode.AddShare ? Consts.RES_ADD_SHARE_TITLE : Consts.RES_EDIT_SHARE_TITLE; }
    }

    public override bool ResourceProviderSupportsResourceTreeNavigation
    {
      get
      {
        IResourceInformationService ris = GetResourceInformationService();
        return ris.DoesResourceProviderSupportTreeListing(BaseResourceProvider.ResourceProviderId);
      }
    }

    protected static IContentDirectory GetContentDirectoryService()
    {
      IContentDirectory contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (contentDirectory != null)
        return contentDirectory;
      throw new NotConnectedException();
    }

    protected static IResourceInformationService GetResourceInformationService()
    {
      IResourceInformationService ris = ServiceRegistration.Get<IServerConnectionManager>().ResourceInformationService;
      if (ris != null)
        return ris;
      throw new NotConnectedException();
    }

    public static IEnumerable<Share> GetShares()
    {
      IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
      IContentDirectory contentDirectory = GetContentDirectoryService();
      _serverSharesCache = new List<Share>(contentDirectory.GetShares(serverConnectionManager.HomeServerSystemId, SharesFilter.All));
      return _serverSharesCache;
    }

    public static void RemoveShares(IEnumerable<Share> shares)
    {
      IContentDirectory contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (contentDirectory == null)
      {
        if (new List<Share>(shares).Count > 0)
          throw new NotConnectedException();
        return;
      }
      foreach (Share share in shares)
        contentDirectory.RemoveShare(share.ShareId);
      _serverSharesCache = null;
    }

    public override void AddShare()
    {
      IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
      IContentDirectory contentDirectory = GetContentDirectoryService();
      Share share = Share.CreateNewShare(serverConnectionManager.HomeServerSystemId, ChoosenResourcePath, ShareName, UseShareWatcher, MediaCategories);
      contentDirectory.RegisterShare(share);
      _serverSharesCache = null;
    }

    public override void UpdateShare(RelocationMode relocationMode)
    {
      IContentDirectory contentDirectory = GetContentDirectoryService();
      contentDirectory.UpdateShare(_origShare.ShareId, ChoosenResourcePath, ShareName, UseShareWatcher, GetMediaCategoriesCleanedUp(), relocationMode);
      _serverSharesCache = null;
    }

    public override void ReImportShare()
    {
      IContentDirectory contentDirectory = GetContentDirectoryService();
      contentDirectory.ReImportShare(_origShare.ShareId);
    }

    protected override string SuggestShareName()
    {
      IResourceInformationService ris = GetResourceInformationService();
      return ris.GetResourceDisplayName(ChoosenResourcePath);
    }

    protected override ResourcePath ExpandResourcePathFromString(string path)
    {
      IResourceInformationService ris = GetResourceInformationService();
      return ris.ExpandResourcePathFromString(BaseResourceProvider.ResourceProviderId, path);
    }

    protected override bool GetIsPathValid(ResourcePath path)
    {
      IResourceInformationService ris = GetResourceInformationService();
      return ris.DoesResourceExist(path);
    }

    protected override bool ShareNameExists(string shareName)
    {
      if (_serverSharesCache == null)
        GetShares();
      Guid origShareId = _origShare == null ? Guid.Empty : _origShare.ShareId;
      return _serverSharesCache.Any(share => share.ShareId != origShareId && share.Name == shareName);
    }

    protected override bool SharePathExists(ResourcePath sharePath)
    {
      if (_serverSharesCache == null)
        GetShares();
      Guid origShareId = _origShare == null ? Guid.Empty : _origShare.ShareId;
      return _serverSharesCache.Any(serverShare => serverShare.ShareId != origShareId && serverShare.BaseResourcePath == sharePath);
    }

    public override string GetResourcePathDisplayName(ResourcePath path)
    {
      return GetServerResourcePathDisplayName(path);
    }

    protected override IEnumerable<ResourcePathMetadata> GetChildDirectoriesData(ResourcePath path)
    {
      IResourceInformationService ris = GetResourceInformationService();
      return ris.GetChildDirectoriesData(path);
    }

    protected override IDictionary<string, MediaCategory> GetAllAvailableCategories()
    {
      IResourceInformationService ris = GetResourceInformationService();
      return new Dictionary<string, MediaCategory>(
          ris.GetMediaCategoriesFromMetadataExtractors().ToDictionary(mediaCategory => mediaCategory.CategoryName));
    }

    public static string GetServerResourcePathDisplayName(ResourcePath path)
    {
      try
      {
      IResourceInformationService ris = GetResourceInformationService();
        return ris.GetResourcePathDisplayName(path);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Problem updating display name of choosen path '{0}'", e, path);
        return string.Empty;
      }
    }

    protected override IEnumerable<ResourceProviderMetadata> GetAvailableBaseResourceProviders()
    {
      IResourceInformationService ris = GetResourceInformationService();
      return new List<ResourceProviderMetadata>(ris.GetAllBaseResourceProviderMetadata());
    }

    protected override ResourceProviderMetadata GetResourceProviderMetadata(Guid resourceProviderId)
    {
      return GetServerResourceProviderMetadata(resourceProviderId);
    }

    public static ResourceProviderMetadata GetServerResourceProviderMetadata(Guid resourceProviderId)
    {
      IResourceInformationService ris = GetResourceInformationService();
      return ris.GetResourceProviderMetadata(resourceProviderId);
    }
  }
}

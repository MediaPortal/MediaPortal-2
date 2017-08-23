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
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemResolver;
using MediaPortal.UI.Shares;
using MediaPortal.UiComponents.SkinBase.General;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  public class LocalShares : SharesProxy
  {
    public LocalShares() : base(ShareEditMode.AddShare) { }

    public LocalShares(Share share)
      : base(ShareEditMode.EditShare)
    {
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      string localSystemName = systemResolver.GetSystemNameForSystemId(systemResolver.LocalSystemId).HostName ?? systemResolver.LocalSystemId;
      InitializePropertiesWithShare(share, localSystemName);
    }

    public override string ConfigShareTitle
    {
      get { return _editMode == ShareEditMode.AddShare ? Consts.RES_ADD_SHARE_TITLE : Consts.RES_EDIT_SHARE_TITLE; }
    }

    public override bool ResourceProviderSupportsResourceTreeNavigation
    {
      get
      {
        ResourceProviderMetadata rpm = BaseResourceProvider;
        if (rpm == null)
          return false;
        IResourceAccessor rootAccessor;
        if (!GetResourceProvider(rpm.ResourceProviderId).TryCreateResourceAccessor("/", out rootAccessor))
          return false;
        using (rootAccessor)
          return rootAccessor is IFileSystemResourceAccessor;
      }
    }

    public static IEnumerable<Share> GetShares()
    {
      ILocalSharesManagement sharesManagement = ServiceRegistration.Get<ILocalSharesManagement>();
      return sharesManagement.Shares.Values;
    }

    public static bool RemoveShares(IEnumerable<Share> shares)
    {
      ILocalSharesManagement sharesManagement = ServiceRegistration.Get<ILocalSharesManagement>();
      foreach (Share share in shares)
        sharesManagement.RemoveShare(share.ShareId);
      return true;
    }

    public override void AddShare()
    {
      ILocalSharesManagement sharesManagement = ServiceRegistration.Get<ILocalSharesManagement>();
      sharesManagement.RegisterShare(ChoosenResourcePath, ShareName, UseShareWatcher, MediaCategories);
    }

    protected static IBaseResourceProvider GetResourceProvider(Guid resourceProviderId)
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      IResourceProvider result;
      if (!mediaAccessor.LocalResourceProviders.TryGetValue(resourceProviderId, out result))
        return null;
      return result as IBaseResourceProvider;
    }

    protected override IEnumerable<ResourceProviderMetadata> GetAvailableBaseResourceProviders()
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      return mediaAccessor.LocalBaseResourceProviders.Select(resourceProvider => resourceProvider.Metadata);
    }

    protected override ResourceProviderMetadata GetResourceProviderMetadata(Guid resourceProviderId)
    {
      return GetLocalResourceProviderMetadata(resourceProviderId);
    }

    public static ResourceProviderMetadata GetLocalResourceProviderMetadata(Guid resourceProviderId)
    {
      IResourceProvider result = GetResourceProvider(resourceProviderId);
      return result == null ? null : result.Metadata;
    }

    protected override ResourcePath ExpandResourcePathFromString(string pathStr)
    {
      ResourceProviderMetadata rpm = BaseResourceProvider;
      IBaseResourceProvider rp = GetResourceProvider(rpm.ResourceProviderId);
      return rp.ExpandResourcePathFromString(pathStr);
    }

    protected override bool GetIsPathValid(ResourcePath path)
    {
      ResourcePath rp = path;
      if (rp == null)
        return false;
      IResourceAccessor ra;
      if (rp.TryCreateLocalResourceAccessor(out ra))
        using (ra)
          return true;
      return false;
    }

    protected override bool ShareNameExists(string shareName)
    {
      ILocalSharesManagement sharesManagement = ServiceRegistration.Get<ILocalSharesManagement>();
      return sharesManagement.Shares.Values.Any(share => (_origShare == null || share.ShareId != _origShare.ShareId) && share.Name == shareName);
    }

    protected override bool SharePathExists(ResourcePath sharePath)
    {
      ILocalSharesManagement sharesManagement = ServiceRegistration.Get<ILocalSharesManagement>();
      return sharesManagement.Shares.Values.Any(share => (_origShare == null || share.ShareId != _origShare.ShareId) && share.BaseResourcePath == sharePath);
    }

    public override string GetResourcePathDisplayName(ResourcePath path)
    {
      return GetLocalResourcePathDisplayName(path);
    }

    public static string GetLocalResourcePathDisplayName(ResourcePath path)
    {
      if (path == null)
        return string.Empty;
      IResourceAccessor ra;
      if (path.TryCreateLocalResourceAccessor(out ra))
        using (ra)
          return ra.ResourcePathName;
      ServiceRegistration.Get<ILogger>().Warn("LocalShares: Cannot access resource path '{0}' for updating display name", path);
      return string.Empty;
    }

    protected override IEnumerable<ResourcePathMetadata> GetChildDirectoriesData(ResourcePath path)
    {
      IResourceAccessor ra;
      if (path.TryCreateLocalResourceAccessor(out ra))
      {
        using (ra)
        {
          IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
          if (fsra == null)
            yield break;
          ICollection<IFileSystemResourceAccessor> res = FileSystemResourceNavigator.GetChildDirectories(fsra, false);
          if (res != null)
            foreach (IFileSystemResourceAccessor childAccessor in res)
              using (childAccessor)
              {
                yield return new ResourcePathMetadata
                  {
                    ResourceName = childAccessor.ResourceName,
                    HumanReadablePath = childAccessor.ResourcePathName,
                    ResourcePath = childAccessor.CanonicalLocalResourcePath
                  };
              }
        }
      }
      else
        ServiceRegistration.Get<ILogger>().Warn("LocalShares: Cannot access resource path '{0}' for getting child directories", path);
    }

    protected override IDictionary<string, MediaCategory> GetAllAvailableCategories()
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      return mediaAccessor.MediaCategories;
    }

    protected override string SuggestShareName()
    {
      IResourceAccessor ra;
      if (ChoosenResourcePath.TryCreateLocalResourceAccessor(out ra))
        using (ra)
          return ra.ResourceName;
      ServiceRegistration.Get<ILogger>().Warn("LocalShares: Cannot access resource path '{0}' for suggesting share name", ChoosenResourcePath);
      return string.Empty;
    }

    public override void UpdateShare(RelocationMode relocationMode)
    {
      ILocalSharesManagement sharesManagement = ServiceRegistration.Get<ILocalSharesManagement>();
      sharesManagement.UpdateShare(_origShare.ShareId, ChoosenResourcePath, ShareName, UseShareWatcher, GetMediaCategoriesCleanedUp(), relocationMode);
    }

    public override void ReImportShare()
    {
      ILocalSharesManagement sharesManagement = ServiceRegistration.Get<ILocalSharesManagement>();
      sharesManagement.ReImportShare(_origShare.ShareId);
    }
  }
}

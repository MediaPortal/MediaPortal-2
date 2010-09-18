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
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.UI.Shares;
using MediaPortal.Utilities;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  public class LocalShares : SharesProxy
  {
    #region Consts

    public const string ADD_SHARE_TITLE_RES = "[SharesConfig.AddLocalShare]";
    public const string EDIT_SHARE_TITLE_RES = "[SharesConfig.EditLocalShare]";

    #endregion

    public LocalShares() : base(ShareEditMode.AddShare) { }

    public LocalShares(Share share) : base(ShareEditMode.EditShare)
    {
      InitializePropertiesWithShare(share);
    }

    public override string ConfigShareTitle
    {
      get { return _editMode == ShareEditMode.AddShare ? ADD_SHARE_TITLE_RES : EDIT_SHARE_TITLE_RES; }
    }

    public override bool MediaProviderSupportsResourceTreeNavigation
    {
      get
      {
        MediaProviderMetadata mpm = BaseMediaProvider;
        if (mpm == null)
          return false;
        IResourceAccessor rootAccessor = GetMediaProvider(mpm.MediaProviderId).CreateMediaItemAccessor("/");
        try
        {
          return rootAccessor is IFileSystemResourceAccessor;
        }
        finally
        {
          rootAccessor.Dispose();
        }
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
      sharesManagement.RegisterShare(ChoosenResourcePath, ShareName, MediaCategories);
    }

    protected static IBaseMediaProvider GetMediaProvider(Guid mediaProviderId)
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      IMediaProvider result;
      if (!mediaAccessor.LocalMediaProviders.TryGetValue(mediaProviderId, out result))
        return null;
      return result as IBaseMediaProvider;
    }

    protected override IEnumerable<MediaProviderMetadata> GetAvailableBaseMediaProviders()
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      foreach (IBaseMediaProvider mediaProvider in mediaAccessor.LocalBaseMediaProviders)
        yield return mediaProvider.Metadata;
    }

    protected override MediaProviderMetadata GetMediaProviderMetadata(Guid mediaProviderId)
    {
      return GetLocalMediaProviderMetadata(mediaProviderId);
    }

    public static MediaProviderMetadata GetLocalMediaProviderMetadata(Guid mediaProviderId)
    {
      IMediaProvider result = GetMediaProvider(mediaProviderId);
      return result == null ? null : result.Metadata;
    }

    protected override ResourcePath ExpandResourcePathFromString(string pathStr)
    {
      MediaProviderMetadata mpm = BaseMediaProvider;
      IBaseMediaProvider mp = GetMediaProvider(mpm.MediaProviderId);
      return mp.ExpandResourcePathFromString(pathStr);
    }

    protected override bool GetIsPathValid(ResourcePath path)
    {
      ResourcePath rp = path;
      if (rp == null)
        return false;
      try
      {
        // Check if we can create an item accessor - if we get an exception, the path is not valid
        IResourceAccessor ra = rp.CreateLocalMediaItemAccessor();
        ra.Dispose();
        return true;
      }
      catch (Exception) // No logging necessary - exception is used to determine an invalid path
      {
        return false;
      }
    }

    public override string GetResourcePathDisplayName(ResourcePath path)
    {
      return GetLocalResourcePathDisplayName(path);
    }

    public static string GetLocalResourcePathDisplayName(ResourcePath path)
    {
      if (path == null)
        return string.Empty;
      try
      {
        IResourceAccessor ra = path.CreateLocalMediaItemAccessor();
        try
        {
          return ra.ResourcePathName;
        }
        finally
        {
          ra.Dispose();
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Problem updating display name of choosen path '{0}'", e, path);
        return string.Empty;
      }
    }

    protected override IEnumerable<ResourcePathMetadata> GetChildDirectoriesData(ResourcePath path)
    {
      IResourceAccessor accessor = path.CreateLocalMediaItemAccessor();
      ICollection<IFileSystemResourceAccessor> res = FileSystemResourceNavigator.GetChildDirectories(accessor);
      if (res != null)
        foreach (IFileSystemResourceAccessor childAccessor in res)
          yield return new ResourcePathMetadata
          {
            ResourceName = childAccessor.ResourceName,
            HumanReadablePath = childAccessor.ResourcePathName,
            ResourcePath = childAccessor.LocalResourcePath
          };
    }

    protected override IEnumerable<string> GetAllAvailableCategories()
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      ICollection<string> result = new HashSet<string>();
      foreach (IMetadataExtractor me in mediaAccessor.LocalMetadataExtractors.Values)
      {
        MetadataExtractorMetadata metadata = me.Metadata;
        CollectionUtils.AddAll(result, metadata.ShareCategories);
      }
      return result;
    }

    protected override string SuggestShareName()
    {
      try
      {
        IResourceAccessor ra = ChoosenResourcePath.CreateLocalMediaItemAccessor();
        try
        {
          return ra.ResourceName;
        }
        finally
        {
          ra.Dispose();
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Problem generating suggestion for share name for path '{0}'", e, ChoosenResourcePath);
        return string.Empty;
      }
    }

    public override void UpdateShare(RelocationMode relocationMode)
    {
      ILocalSharesManagement sharesManagement = ServiceRegistration.Get<ILocalSharesManagement>();
      sharesManagement.UpdateShare(_origShare.ShareId, ChoosenResourcePath, ShareName, MediaCategories, relocationMode);
    }

    public override void ReImportShare()
    {
      ILocalSharesManagement sharesManagement = ServiceRegistration.Get<ILocalSharesManagement>();
      sharesManagement.ReImportShare(_origShare.ShareId);
    }
  }
}

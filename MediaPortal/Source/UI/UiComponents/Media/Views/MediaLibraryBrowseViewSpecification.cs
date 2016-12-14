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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.Utilities.DB;
using UPnP.Infrastructure.CP;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.Settings;

namespace MediaPortal.UiComponents.Media.Views
{
  /// <summary>
  /// View which is based on a media library query.
  /// </summary>
  public class MediaLibraryBrowseViewSpecification : ViewSpecification
  {
    #region Consts

    protected static IEnumerable<Guid> DIRECTORY_MIA_ID_ENUMERATION = new Guid[]
        {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          DirectoryAspect.ASPECT_ID,
        };

    protected static IEnumerable<Guid> EMPTY_ID_ENUMERATION = new Guid[] { };

    #endregion

    #region Protected fields

    protected Guid _directoryId;
    protected string _systemId;
    protected ResourcePath _basePath;
    protected int? _absNumItems;

    #endregion

    #region Ctor

    public MediaLibraryBrowseViewSpecification(string viewDisplayName, Guid directoryId,
        string systemId, ResourcePath basePath,
        IEnumerable<Guid> necessaryMIATypeIDs, IEnumerable<Guid> optionalMIATypeIDs) :
        base(viewDisplayName, necessaryMIATypeIDs, optionalMIATypeIDs)
    {
      _directoryId = directoryId;
      _systemId = systemId;
      _basePath = basePath;
    }

    #endregion

    public Guid DirectoryId
    {
      get { return _directoryId; }
    }

    public string SystemId
    {
      get { return _systemId; }
    }

    public ResourcePath BasePath
    {
      get { return _basePath; }
    }

    /// <summary>
    /// Can be set to provide the overall number of all items and child items in this view.
    /// </summary>
    public override int? AbsNumItems
    {
      get { return _absNumItems; }
    }

    public override bool CanBeBuilt
    {
      get
      {
        IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
        return scm.IsHomeServerConnected;
      }
    }

    public override IViewChangeNotificator CreateChangeNotificator()
    {
      return new ServerConnectionChangeNotificator();
    }

    public override IEnumerable<MediaItem> GetAllMediaItems()
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return new List<MediaItem>();

      MediaItemQuery query = new MediaItemQuery(
          _necessaryMIATypeIds,
          _optionalMIATypeIds,
          new BooleanCombinationFilter(BooleanOperator.And,
              new IFilter[]
              {
                new RelationalFilter(ProviderResourceAspect.ATTR_SYSTEM_ID, RelationalOperator.EQ, _systemId),
                new LikeFilter(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, SqlUtils.LikeEscape(_basePath.Serialize(), '\\') + "%", '\\', true)
              }));

      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;

      return cd.Search(query, false, userProfile, ShowVirtualSetting.ShowVirtualMedia(_necessaryMIATypeIds));
    }

    protected internal override void ReLoadItemsAndSubViewSpecifications(out IList<MediaItem> mediaItems, out IList<ViewSpecification> subViewSpecifications)
    {
      mediaItems = null;
      subViewSpecifications = null;
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return;

      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;

      try
      {
        bool showVirtual = ShowVirtualSetting.ShowVirtualMedia(_necessaryMIATypeIds);
        mediaItems = new List<MediaItem>(cd.Browse(_directoryId, _necessaryMIATypeIds, _optionalMIATypeIds, userProfile, showVirtual));
        ICollection<MediaItem> childDirectories = cd.Browse(_directoryId, DIRECTORY_MIA_ID_ENUMERATION, EMPTY_ID_ENUMERATION, userProfile, showVirtual);
        subViewSpecifications = new List<ViewSpecification>(childDirectories.Count);
        foreach (MediaItem childDirectory in childDirectories)
        {
          SingleMediaItemAspect ma = null;
          MediaItemAspect.TryGetAspect(childDirectory.Aspects, MediaAspect.Metadata, out ma);
          IList<MultipleMediaItemAspect> pras = null;

          MediaItemAspect.TryGetAspects(childDirectory.Aspects, ProviderResourceAspect.Metadata, out pras);
          foreach (MultipleMediaItemAspect pra in pras)
          {
            MediaLibraryBrowseViewSpecification subViewSpecification = new MediaLibraryBrowseViewSpecification(
                (string)ma.GetAttributeValue(MediaAspect.ATTR_TITLE), childDirectory.MediaItemId,
                (string)pra.GetAttributeValue(ProviderResourceAspect.ATTR_SYSTEM_ID),
                ResourcePath.Deserialize((string)pra.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH)),
                _necessaryMIATypeIds, _optionalMIATypeIds);
            subViewSpecifications.Add(subViewSpecification);
          }
        }
      }
      catch (UPnPRemoteException e)
      {
        ServiceRegistration.Get<ILogger>().Error("SimpleTextSearchViewSpecification.ReLoadItemsAndSubViewSpecifications: Error requesting server", e);
        mediaItems = null;
        subViewSpecifications = null;
      }
    }
  }
}

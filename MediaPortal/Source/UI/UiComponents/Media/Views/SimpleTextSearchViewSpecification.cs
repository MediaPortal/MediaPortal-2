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
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UI.ServerCommunication;
using UPnP.Infrastructure.CP;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.Settings;

namespace MediaPortal.UiComponents.Media.Views
{
  /// <summary>
  /// View which is represents the results of a simple text search at the media library.
  /// </summary>
  public class SimpleTextSearchViewSpecification : ViewSpecification
  {
    #region Protected fields

    protected IFilter _filter;
    protected bool _excludeCLOBs;
    protected bool _onlyOnline;

    #endregion

    #region Ctor

    public SimpleTextSearchViewSpecification(string viewDisplayName, IFilter filter,
        IEnumerable<Guid> necessaryMIATypeIds, IEnumerable<Guid> optionalMIATypeIds, bool excludeCLOBs, bool onlyOnline) :
      base(viewDisplayName, necessaryMIATypeIds, optionalMIATypeIds)
    {
      _filter = filter;
      _excludeCLOBs = excludeCLOBs;
      _onlyOnline = onlyOnline;
    }

    #endregion

    public bool OnlyOnline
    {
      get { return _onlyOnline; }
    }

    public bool ExcludeCLOBs
    {
      get { return _excludeCLOBs; }
    }

    public IFilter Filter
    {
      get { return _filter; }
    }

    public override bool CanBeBuilt
    {
      get
      {
        IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
        return scm.IsHomeServerConnected;
      }
    }

    protected internal override void ReLoadItemsAndSubViewSpecifications(out IList<MediaItem> mediaItems, out IList<ViewSpecification> subViewSpecifications)
    {
      mediaItems = null;
      subViewSpecifications = new List<ViewSpecification>();
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return;

      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;

      try
      {
        MediaItemQuery query = new MediaItemQuery(_necessaryMIATypeIds, _optionalMIATypeIds, _filter)
        {
          Limit = Consts.MAX_NUM_ITEMS_VISIBLE
        };
        mediaItems = cd.Search(query, true, userProfile, ShowVirtualSetting.ShowVirtualMedia(_necessaryMIATypeIds));
      }
      catch (UPnPRemoteException e)
      {
        ServiceRegistration.Get<ILogger>().Error("SimpleTextSearchViewSpecification.ReLoadItemsAndSubViewSpecifications: Error requesting server", e);
        mediaItems = new List<MediaItem>();
      }
    }
  }
}

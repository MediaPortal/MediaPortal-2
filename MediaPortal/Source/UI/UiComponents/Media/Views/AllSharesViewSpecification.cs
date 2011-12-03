#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.UiComponents.Media.Views
{
  /// <summary>
  /// View implementation which presents a list of of all shares registered at the media library, one sub view for each share.
  /// </summary>
  public class AllSharesViewSpecification : ViewSpecification
  {
    #region Consts

    protected static IEnumerable<Guid> DIRECTORY_MIA_ID_ENUMERATION = new Guid[]
        {
          DirectoryAspect.ASPECT_ID,
          ProviderResourceAspect.ASPECT_ID,
        };

    protected static IEnumerable<Guid> EMPTY_ID_ENUMERATION = new Guid[] { };

    #endregion

    #region Ctor

    public AllSharesViewSpecification(string viewDisplayName,
        IEnumerable<Guid> necessaryMIATypeIds, IEnumerable<Guid> optionalMIATypeIds) :
        base(viewDisplayName, necessaryMIATypeIds, optionalMIATypeIds) { }

    #endregion

    #region Base overrides

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
      mediaItems = new List<MediaItem>();
      subViewSpecifications = new List<ViewSpecification>();
      IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
      IContentDirectory cd = scm.ContentDirectory;
      if (cd == null)
        return;
      foreach (Share share in cd.GetShares(null, SharesFilter.All))
      {
        MediaItem parentDirectory = cd.LoadItem(share.SystemId, share.BaseResourcePath, DIRECTORY_MIA_ID_ENUMERATION, EMPTY_ID_ENUMERATION);
        if (parentDirectory == null)
          continue;
        MediaItemAspect pra = parentDirectory.Aspects[ProviderResourceAspect.ASPECT_ID];
        subViewSpecifications.Add(new MediaLibraryBrowseViewSpecification(share.Name, parentDirectory.MediaItemId,
            (string) pra.GetAttributeValue(ProviderResourceAspect.ATTR_SYSTEM_ID),
            ResourcePath.Deserialize((string) pra.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH)),
            _necessaryMIATypeIds, _optionalMIATypeIds));
      }
    }

    #endregion
  }
}

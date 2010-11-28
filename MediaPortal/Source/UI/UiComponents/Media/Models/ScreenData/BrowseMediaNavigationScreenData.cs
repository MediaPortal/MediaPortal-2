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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Views;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.Utilities.DB;

namespace MediaPortal.UiComponents.Media.Models.ScreenData
{
  public class BrowseMediaNavigationScreenData : AbstractItemsScreenData
  {
    protected bool _onlyOnline;

    public BrowseMediaNavigationScreenData(PlayableItemCreatorDelegate playableItemCreator, bool onlyOnline) :
        base(Consts.SCREEN_BROWSE_MEDIA_NAVIGATION, null, Consts.RES_BROWSE_MEDIA_NAVIGATION_NAVBAR_DISPLAY_LABEL,
        playableItemCreator, true)
    {
      _onlyOnline = onlyOnline;
    }

    public override IEnumerable<MediaItem> GetAllMediaItems()
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return new List<MediaItem>();
      MediaLibraryBrowseViewSpecification vs = (MediaLibraryBrowseViewSpecification) _navigationData.BaseViewSpecification;
      MediaItemQuery query = new MediaItemQuery(
          _navigationData.BaseViewSpecification.NecessaryMIATypeIds,
          _navigationData.BaseViewSpecification.OptionalMIATypeIds,
          new BooleanCombinationFilter(BooleanOperator.And,
              new IFilter[]
              {
                new RelationalFilter(ProviderResourceAspect.ATTR_SYSTEM_ID, RelationalOperator.EQ, vs.SystemId),
                new LikeFilter(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, SqlUtils.LikeEscape(vs.BasePath.Serialize(), '\\') + "%", '\\', true)
              }));
      return cd.Search(query, _onlyOnline);
    }

    public override AbstractItemsScreenData Derive()
    {
      return new BrowseMediaNavigationScreenData(PlayableItemCreator, _onlyOnline);
    }
  }
}
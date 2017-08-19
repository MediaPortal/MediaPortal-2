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

using MediaPortal.Common;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.Localization;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.UiComponents.Media.FilterCriteria
{
  public class FilterByAlbumCompilationCriterion : SimpleMLFilterCriterion
  {
    public FilterByAlbumCompilationCriterion(MediaItemAspectMetadata.AttributeSpecification attributeType)
      : base(attributeType)
    {
    }

    #region Base overrides

    public override ICollection<FilterValue> GetAvailableValues(IEnumerable<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        throw new NotConnectedException("The MediaLibrary is not connected");

      List<Guid> necessaryAlbumMIATypeIds = necessaryMIATypeIds.ToList();
      if(necessaryAlbumMIATypeIds.Contains(AudioAspect.ASPECT_ID))
        necessaryAlbumMIATypeIds.Remove(AudioAspect.ASPECT_ID);
      necessaryAlbumMIATypeIds.Add(AudioAlbumAspect.ASPECT_ID);

      Guid? userProfile = null;
      bool applyUserRestrictions = false;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
      {
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;
        applyUserRestrictions = userProfileDataManagement.ApplyUserRestriction;
      }
      bool showVirtual = ShowVirtualSetting.ShowVirtualMedia(necessaryMIATypeIds);

      IFilter emptyFilter = new EmptyFilter(AudioAlbumAspect.ATTR_COMPILATION);
      IFilter compiledFilter = new RelationalFilter(AudioAlbumAspect.ATTR_COMPILATION, RelationalOperator.EQ, true);
      IFilter uncompiledFilter = new RelationalFilter(AudioAlbumAspect.ATTR_COMPILATION, RelationalOperator.EQ, false);
      int numEmptyItems = cd.CountMediaItems(necessaryAlbumMIATypeIds, emptyFilter, true, userProfile, showVirtual, applyUserRestrictions);
      int numCompiledItems = cd.CountMediaItems(necessaryAlbumMIATypeIds, compiledFilter, true, userProfile, showVirtual, applyUserRestrictions);
      int numUncompiledItems = cd.CountMediaItems(necessaryAlbumMIATypeIds, uncompiledFilter, true, userProfile, showVirtual, applyUserRestrictions);
      return new List<FilterValue>(new FilterValue[]
        {
            new FilterValue(Consts.RES_VALUE_EMPTY_TITLE, emptyFilter, null, numEmptyItems, this),
            new FilterValue(Consts.RES_COMPILATION_FILTER_COMPILED, compiledFilter, null, numCompiledItems, this),
            new FilterValue(Consts.RES_COMPILATION_FILTER_UNCOMPILED, uncompiledFilter, null, numUncompiledItems, this),
        }.Where(fv => !fv.NumItems.HasValue || fv.NumItems.Value > 0));
    }

    public override ICollection<FilterValue> GroupValues(ICollection<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      return null;
    }

    #endregion
  }
}

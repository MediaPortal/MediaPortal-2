﻿#region Copyright (C) 2007-2017 Team MediaPortal

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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.MediaServer.Profiles;
using MediaPortal.Common.MediaManagement.MLQueries;
using System.Collections.Generic;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  internal class MediaLibraryRecentContainer : MediaLibraryContainer
  {
    public MediaLibraryRecentContainer(string id, Guid[] necessaryMiaTypeIds, Guid[] optionalMiaTypeIds, EndPointSettings client)
      : base(id, "Recently Added", necessaryMiaTypeIds, optionalMiaTypeIds, null, client)
    {
      _query = new MediaItemQuery(_necessaryMiaTypeIds, _optionalMiaTypeIds, AppendUserFilter(null, necessaryMiaTypeIds))
      {
        Limit = 10,
        SortInformation = new List<ISortInformation>() { new AttributeSortInformation(ImporterAspect.ATTR_DATEADDED, SortDirection.Descending) }
      };
    }
  }
}

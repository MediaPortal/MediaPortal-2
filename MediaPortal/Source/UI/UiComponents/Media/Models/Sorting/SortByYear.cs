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
using MediaPortal.Common.MediaManagement;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.Sorting
{
  /// <summary>
  /// In fact, this comparer does the same than <see cref="SortByDate"/>, but the <see cref="DisplayName"/> shows something like
  /// <c>"Sort by year"</c>.
  /// </summary>
  public class SortByYear : SortByDate
  {
    public override string DisplayName
    {
      get { return Consts.RES_COMMON_BY_YEAR_MENU_ITEM; }
    }

    public override string GroupByDisplayName
    {
      get { return Consts.RES_COMMON_BY_YEAR_MENU_ITEM; }
    }

    public override object GetGroupByValue(MediaItem item)
    {
      var dt = (DateTime?) base.GetGroupByValue(item);
      if (!dt.HasValue)
        return null;
      return dt.Value.Year;
    }
  }
}

#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.Sorting
{
  public class SortByTitle : AbstractSortByComparableObjectAttribute<string>
  {
    public SortByTitle() : base(Consts.RES_COMMON_BY_TITLE_MENU_ITEM, Consts.RES_COMMON_BY_TITLE_MENU_ITEM, MediaAspect.ATTR_TITLE)
    {
      _includeMias = new[] { MediaAspect.ASPECT_ID };
      _excludeMias = new[] { PersonAspect.ASPECT_ID, CompanyAspect.ASPECT_ID, SeasonAspect.ASPECT_ID, EpisodeAspect.ASPECT_ID, AudioAspect.ASPECT_ID };
    }
  }
}

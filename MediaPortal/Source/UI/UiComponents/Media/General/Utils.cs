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

using MediaPortal.Common.Localization;

namespace MediaPortal.UiComponents.Media.General
{
  public class Utils
  {
    public static string BuildNumItemsStr(int numItems, int? total)
    {
      if (numItems == 0)
        return Consts.RES_NO_ITEMS;
      if (numItems == 1)
        if (!total.HasValue)
          return Consts.RES_ONE_ITEM;
        else if (total == 1)
          return Consts.RES_ONE_OF_ONE_ITEM;
      if (total.HasValue)
        return LocalizationHelper.Translate(Consts.RES_N_OF_M_ITEMS, numItems, total.Value);
      return LocalizationHelper.Translate(Consts.RES_N_ITEMS, numItems);
    }
  }
}

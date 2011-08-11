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

using MediaPortal.Core.Localization;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  /// <summary>
  /// Base class for each ListItem used as navigation item in the Media plugin
  /// </summary>
  public class NavigationItem : ListItem
  {
    public string SimpleTitle
    {
      get { return this[Consts.KEY_SIMPLE_TITLE]; }
      set { SetLabel(Consts.KEY_SIMPLE_TITLE, value); }
    }

    public string SortString
    {
      get
      {
        IResourceString sortStr;
        if (_labels.TryGetValue(Consts.KEY_SORT_STRING, out sortStr))
          return sortStr.Evaluate();
        return SimpleTitle;
      }
      set { SetLabel(Consts.KEY_SORT_STRING, value); }
    }
  }
}
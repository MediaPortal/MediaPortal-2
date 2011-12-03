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

using MediaPortal.Common.Settings;

namespace MediaPortal.UiComponents.Media.Settings
{
  public enum LayoutType
  {
    ListLayout,
    GridLayout,
  }

  public enum LayoutSize
  {
    Small,
    Medium,
    Large,
  }

  public class ViewSettings
  {
    protected const LayoutType DEFAULT_LAYOUT_TYPE = LayoutType.ListLayout;
    protected const LayoutSize DEFAULT_LAYOUT_SIZE = LayoutSize.Small;

    protected LayoutType _layoutType = DEFAULT_LAYOUT_TYPE;
    protected LayoutSize _layoutSize = DEFAULT_LAYOUT_SIZE;

    [Setting(SettingScope.User, 1)]
    public LayoutType LayoutType
    {
      get { return _layoutType; }
      set { _layoutType = value; }
    }

    [Setting(SettingScope.User, 1)]
    public LayoutSize LayoutSize
    {
      get { return _layoutSize; }
      set { _layoutSize = value; }
    }
  }
}

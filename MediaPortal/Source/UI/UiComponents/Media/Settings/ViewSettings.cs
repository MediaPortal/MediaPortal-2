#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using MediaPortal.Utilities.Xml;

namespace MediaPortal.UiComponents.Media.Settings
{
  public enum LayoutType
  {
    ListLayout,
    GridLayout,
    CoverLayout,
    // TODO: CoverFlowLayout,
  }

  public enum LayoutSize
  {
    Small,
    Medium,
    Large,
  }

  public struct ScreenConfig
  {
    public string Sorting { get; set; }
    public LayoutType LayoutType { get; set; }
    public LayoutSize LayoutSize { get; set; }
  }

  public class ViewSettings
  {
    public const LayoutType DEFAULT_LAYOUT_TYPE = LayoutType.ListLayout;
    public const LayoutSize DEFAULT_LAYOUT_SIZE = LayoutSize.Small;

    public ViewSettings()
    {
      ScreenConfigs = new SerializableDictionary<string, ScreenConfig>();
      ScreenHierarchy = new SerializableDictionary<string, string>();
    }

    [Setting(SettingScope.User)]
    public SerializableDictionary<string, ScreenConfig> ScreenConfigs { get; set; }

    [Setting(SettingScope.User)]
    public SerializableDictionary<string, string> ScreenHierarchy { get; set; }
  }
}

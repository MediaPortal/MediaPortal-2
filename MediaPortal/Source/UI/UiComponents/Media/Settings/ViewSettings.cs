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
    public string Grouping { get; set; }
    public LayoutType LayoutType { get; set; }
    public LayoutSize LayoutSize { get; set; }
  }

  /// <summary>
  /// <see cref="MediaDictionary{TKey,TValue}"/> implements a local type that is valid for XML serialization. It is used only for providing
  /// a type that is defined inside the same assembly as the <see cref="ViewSettings"/> class.
  /// It is a workaround for serialization issues, if the serializing type (MediaPortal.Utilities dictionary) has no reference to the used type
  /// (Media library settings).
  /// </summary>
  public class MediaDictionary<TKey, TValue> : SerializableDictionary<TKey, TValue>
  { }

  public class ViewSettings
  {
    public const LayoutType DEFAULT_LAYOUT_TYPE = LayoutType.ListLayout;
    public const LayoutSize DEFAULT_LAYOUT_SIZE = LayoutSize.Small;

    public ViewSettings()
    {
      ScreenConfigs = new MediaDictionary<string, ScreenConfig>();
      ScreenHierarchy = new MediaDictionary<string, string>();
    }

    [Setting(SettingScope.User)]
    public MediaDictionary<string, ScreenConfig> ScreenConfigs { get; set; }

    [Setting(SettingScope.User)]
    public MediaDictionary<string, string> ScreenHierarchy { get; set; }

    /// <summary>
    /// Default setting for showing virtual series related media items.
    /// </summary>
    [Setting(SettingScope.User, DefaultValue = false)]
    public bool ShowVirtualSeriesMedia { get; set; }

    /// <summary>
    /// Default setting for showing virtual movie related media items.
    /// </summary>
    [Setting(SettingScope.User, DefaultValue = false)]
    public bool ShowVirtualMovieMedia { get; set; }

    /// <summary>
    /// Default setting for showing virtual audio related media items.
    /// </summary>
    [Setting(SettingScope.User, DefaultValue = false)]
    public bool ShowVirtualAudioMedia { get; set; }
  }
}

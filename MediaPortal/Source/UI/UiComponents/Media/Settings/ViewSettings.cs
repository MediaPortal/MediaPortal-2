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
    private static readonly MediaDictionary<string, ScreenConfig> DEFAULT_SCREEN_CONFIGS = new MediaDictionary<string, ScreenConfig>
    {
      {
        typeof(Models.ScreenData.SeriesFilterByNameScreenData).FullName,
        new ScreenConfig
        {
          LayoutSize = LayoutSize.Large,
          LayoutType = LayoutType.GridLayout,
          Sorting = typeof(Models.Sorting.SeriesSortByEpisode).FullName
        }
      },
      {
        typeof(Models.ScreenData.SeriesFilterBySeasonScreenData).FullName,
        new ScreenConfig
        {
          LayoutSize = LayoutSize.Large,
          LayoutType = LayoutType.GridLayout,
          Sorting = typeof(Models.Sorting.SeriesSortByEpisode).FullName
        }
      },
      {
        typeof(Models.ScreenData.SeriesShowItemsScreenData).FullName,
        new ScreenConfig
        {
          LayoutSize = LayoutSize.Large,
          LayoutType = LayoutType.GridLayout,
          Sorting = typeof(Models.Sorting.SeriesSortByEpisode).FullName
        }
      },
      {
        typeof(Models.ScreenData.MoviesShowItemsScreenData).FullName,
        new ScreenConfig
        {
          LayoutSize = LayoutSize.Large,
          LayoutType = LayoutType.GridLayout,
          Sorting = typeof(Models.Sorting.SortByTitle).FullName
        }
      }
    };

    private static readonly MediaDictionary<string, string> DEFAULT_SCREEN_HIERARCHY = new MediaDictionary<string, string>
    {
      { typeof(Models.ScreenData.SeriesFilterByNameScreenData).FullName, typeof(Models.ScreenData.SeriesFilterBySeasonScreenData).FullName }
    };

    private MediaDictionary<string, ScreenConfig> _screenConfigs;
    private MediaDictionary<string, string> _screenHierarchy;
    public const LayoutType DEFAULT_LAYOUT_TYPE = LayoutType.GridLayout;
    public const LayoutSize DEFAULT_LAYOUT_SIZE = LayoutSize.Large;

    [Setting(SettingScope.User)]
    public MediaDictionary<string, ScreenConfig> ScreenConfigs
    {
      get { return _screenConfigs ?? DEFAULT_SCREEN_CONFIGS; }
      set { _screenConfigs = value; }
    }

    [Setting(SettingScope.User)]
    public MediaDictionary<string, string> ScreenHierarchy
    {
      get { return _screenHierarchy ?? DEFAULT_SCREEN_HIERARCHY; }
      set { _screenHierarchy = value; }
    }

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

    /// <summary>
    /// Default setting for showing genre names localized.
    /// </summary>
    [Setting(SettingScope.User, DefaultValue = true)]
    public bool UseLocalizedGenres { get; set; }
  }
}

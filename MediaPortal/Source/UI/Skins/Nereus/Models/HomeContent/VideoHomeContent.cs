#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using System.Collections.Generic;
using MediaPortal.UI.Presentation.Models;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  public class VideoHomeContent : AbstractHomeContent
  {
    public VideoHomeContent()
    {
      _availableLists.Add(new LatestVideoList());
      _availableLists.Add(new ContinuePlayVideoList());
      _availableLists.Add(new FavoriteVideoList());
      _availableLists.Add(new FavoriteVideoList());
    }

    protected override void PopulateBackingList()
    {
      // Wrap each group of tiles to show in an ItemsListWrapper and add
      // the wrapper to the backing list.
      // For the media home content, the firat item is a list of shortcuts
      // to media filters, followed by a group of tiles for each media list.

      // Add the video media filters as the first tile group
      _backingList.Add(new MediaShortcutListWrapper(new List<ListItem>
      {
        new VideoYearShortcut(),
        new VideoLocationShortcut(),
        new VideoSystemShortcut(),
        new VideoSizeShortcut(),
        new VideoSearchShortcut()
      }));
    }

    protected override IContentListModel GetContentListModel()
    {
      return GetMediaListModel();
    }
  }

  // Add a wrapper for each video media list, we use a separate type for each wrapper
  // Separate classes for each type of media shortcut and media list to
  // allow automatic template selection when displayed in an ItemsControl.

  public class LatestVideoList : MediaListItemsListWrapper
  {
    public LatestVideoList()
      : base("LatestVideo", "[Nereus.Home.LatestAdded]")
    { }
  }

  public class ContinuePlayVideoList : MediaListItemsListWrapper
  {
    public ContinuePlayVideoList()
      : base("ContinuePlayVideo", "[Nereus.Home.ContinuePlayed]")
    { }
  }

  public class FavoriteVideoList : MediaListItemsListWrapper
  {
    public FavoriteVideoList()
      : base("FavoriteVideo", "[Nereus.Home.Favorites]")
    { }
  }

  public class UnplayedVideoList : MediaListItemsListWrapper
  {
    public UnplayedVideoList()
      : base("UnplayedVideo", "[Nereus.Home.Unplayed]")
    { }
  }

  public class VideoYearShortcut : YearShortcutItem
  {
    public VideoYearShortcut()
      : base(Consts.WF_STATE_ID_VIDEOS_NAVIGATION_ROOT, typeof(VideosFilterByYearScreenData))
    { }
  }

  public class VideoLocationShortcut : MediaScreenShortcutItem
  {
    public VideoLocationShortcut()
    {
      // ToDo: Add video location filter...
    }
  }

  public class VideoSystemShortcut : MediaScreenShortcutItem
  {
    public VideoSystemShortcut()
      : base(Consts.WF_STATE_ID_VIDEOS_NAVIGATION_ROOT, typeof(VideosFilterBySystemScreenData))
    { }
  }

  public class VideoSizeShortcut : MediaScreenShortcutItem
  {
    public VideoSizeShortcut()
    {
      // ToDo: Add video size filter...
    }
  }

  public class VideoSearchShortcut : SearchShortcutItem
  {
    public VideoSearchShortcut()
      : base(Consts.WF_STATE_ID_VIDEOS_NAVIGATION_ROOT, typeof(VideosSimpleSearchScreenData))
    { }
  }
}

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

using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.Models;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  public class VideoHomeContent : AbstractHomeContent
  {
    protected override void PopulateBackingList()
    {
      MediaListModel mlm = GetMediaListModel();

      _backingList.Add(new MediaShortcutListWrapper(new List<ListItem>
      {
        new VideoYearShortcut(),
        new VideoLocationShortcut(),
        new VideoSystemShortcut(),
        new VideoSizeShortcut(),
        new VideoSearchShortcut()
      }));

      _backingList.Add(new LatestVideoList(mlm.Lists["LatestVideo"].AllItems));
      _backingList.Add(new ContinuePlayVideoList(mlm.Lists["ContinuePlayVideo"].AllItems));
      _backingList.Add(new FavoriteVideoList(mlm.Lists["FavoriteVideo"].AllItems));
      _backingList.Add(new UnplayedVideoList(mlm.Lists["UnplayedVideo"].AllItems));
    }
  }

  public class LatestVideoList : ItemsListWrapper
  {
    public LatestVideoList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.LatestAdded]")
    { }
  }

  public class ContinuePlayVideoList : ItemsListWrapper
  {
    public ContinuePlayVideoList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.ContinuePlayed]")
    { }
  }

  public class FavoriteVideoList : ItemsListWrapper
  {
    public FavoriteVideoList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.Favorites]")
    { }
  }

  public class UnplayedVideoList : ItemsListWrapper
  {
    public UnplayedVideoList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.Unplayed]")
    { }
  }

  public class VideoYearShortcut : YearShortcutItem
  { }

  public class VideoLocationShortcut : MediaShortcutItem
  { }

  public class VideoSystemShortcut : MediaShortcutItem
  { }

  public class VideoSizeShortcut : MediaShortcutItem
  { }

  public class VideoSearchShortcut : SearchShortcutItem
  { }
}

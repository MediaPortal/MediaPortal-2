﻿#region Copyright (C) 2007-2018 Team MediaPortal

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
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  public class ImageHomeContent : AbstractHomeContent
  {
    protected override void PopulateBackingList()
    {
      MediaListModel mlm = GetMediaListModel();

      _backingList.Add(new MediaShortcutListWrapper(new List<ListItem>
      {
        new ImageYearShortcut(),
        new ImageLocationShortcut(),
        new ImageSystemShortcut(),
        new ImageSizeShortcut(),
        new ImageSearchShortcut()
      }));

      _backingList.Add(new LatestImageList(mlm.Lists["LatestImages"].AllItems));
      _backingList.Add(new FavoriteImageList(mlm.Lists["FavoriteImages"].AllItems));
      _backingList.Add(new UnplayedImageList(mlm.Lists["UnplayedImages"].AllItems));
    }
  }

  public class LatestImageList : ItemsListWrapper
  {
    public LatestImageList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.LatestAdded]")
    { }
  }

  public class FavoriteImageList : ItemsListWrapper
  {
    public FavoriteImageList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.Favorites]")
    { }
  }

  public class UnplayedImageList : ItemsListWrapper
  {
    public UnplayedImageList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.Unplayed]")
    { }
  }

  public class ImageYearShortcut : YearShortcutItem
  {
    public ImageYearShortcut()
      : base(Consts.WF_STATE_ID_IMAGES_NAVIGATION_ROOT, typeof(ImagesFilterByYearScreenData))
    { }
  }

  public class ImageLocationShortcut : MediaScreenShortcutItem
  {
    public ImageLocationShortcut()
      : base(Consts.WF_STATE_ID_IMAGES_NAVIGATION_ROOT, typeof(ImagesFilterByCityScreenData))
    { }
  }

  public class ImageSystemShortcut : MediaScreenShortcutItem
  {
    public ImageSystemShortcut()
      : base(Consts.WF_STATE_ID_IMAGES_NAVIGATION_ROOT, typeof(ImagesFilterBySystemScreenData))
    { }
  }

  public class ImageSizeShortcut : MediaScreenShortcutItem
  {
    public ImageSizeShortcut()
      : base(Consts.WF_STATE_ID_IMAGES_NAVIGATION_ROOT, typeof(ImagesFilterBySizeScreenData))
    { }
  }

  public class ImageSearchShortcut : SearchShortcutItem
  {
    public ImageSearchShortcut()
      : base(Consts.WF_STATE_ID_IMAGES_NAVIGATION_ROOT, typeof(ImagesSimpleSearchScreenData))
    { }
  }
}

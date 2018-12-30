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

using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using System.Collections;

namespace MediaPortal.UiComponents.Nereus.Models.HomeTiles
{
  /// <summary>
  /// Container for a group of <see cref="HomeTile"/>s to be displayed on the home screen.
  /// </summary>
  public class HomeTileGroup : ListItem
  {
    public const int DEFAULT_TILE_GROUP_SIZE = 6;

    protected ItemsList _tiles;
    protected int _tileCount;

    protected ItemsList _attachedList;

    public HomeTileGroup()
      : this(DEFAULT_TILE_GROUP_SIZE)
    {
    }

    public HomeTileGroup(int tileCount)
    {
      _tileCount = tileCount;
      _tiles = new ItemsList();
      for (int i = 0; i < tileCount; i++)
        _tiles.Add(new HomeTile());
    }

    public void AttachToItemsList(ItemsList list)
    {
      DetachFromItemsList();
      _attachedList = list;
      if (list != null)
        list.ObjectChanged += OnAttachedItemsChanged;
      OnAttachedItemsChanged(list);
    }

    public void DetachFromItemsList()
    {
      ItemsList items = _attachedList;
      _attachedList = null;
      if (items != null)
        items.ObjectChanged -= OnAttachedItemsChanged;
    }

    private void OnAttachedItemsChanged(IObservable observable)
    {
      ItemsList items = observable as ItemsList;
      for (int i = 0; i < _tiles.Count; i++)
        ((HomeTile)_tiles[i]).UpdateItem(items != null && items.Count > i ? items[i] : null);
    }

    public ItemsList Tiles
    {
      get { return _tiles; }
    }

    public int TileCount
    {
      get { return _tileCount; }
    }
  }

  /// <summary>
  /// Container for 4 tiles to be displayed in a uniform size.
  /// </summary>
  public class UniformTileGroup : HomeTileGroup
  {

  }

  /// <summary>
  /// Container for 2 poster tiles above a banner tile.
  /// </summary>
  public class PosterBannerGroup : HomeTileGroup
  {

  }

  /// <summary>
  /// Container for a banner tile above 2 poster tiles.
  /// </summary>
  public class BannerPosterGroup : HomeTileGroup
  {

  }

  /// <summary>
  /// Container for 2 large poster tiles.
  /// </summary>
  public class PosterGroup : HomeTileGroup
  {

  }
}

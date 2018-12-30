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
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UiComponents.Nereus.Models.HomeTiles;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UiComponents.Nereus.Controls
{
  public class TileGroupWrapper : Control
  {
    protected AbstractProperty _itemsSourceProperty;
    protected AbstractProperty _countProperty;
    protected AbstractProperty _tileGroupProperty;

    protected ItemsList _tiles;

    public TileGroupWrapper()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _itemsSourceProperty = new SProperty(typeof(object), null);
      _countProperty = new SProperty(typeof(int), 6);
      _tileGroupProperty = new SProperty(typeof(HomeTileGroup), new HomeTileGroup());
    }

    void Attach()
    {
      _itemsSourceProperty.Attach(OnItemsSourceChanged);
      _countProperty.Attach(OnCountChanged);
    }

    public AbstractProperty ItemsSourceProperty
    {
      get { return _itemsSourceProperty; }
    }

    public ItemsList ItemsSource
    {
      get { return (ItemsList)_itemsSourceProperty.GetValue(); }
      set { _itemsSourceProperty.SetValue(value); }
    }

    public AbstractProperty CountProperty
    {
      get { return _countProperty; }
    }

    public int Count
    {
      get { return (int)_countProperty.GetValue(); }
      set { _countProperty.SetValue(value); }
    }

    public AbstractProperty TileGroupProperty
    {
      get { return _tileGroupProperty; }
    }

    public HomeTileGroup TileGroup
    {
      get { return (HomeTileGroup)_tileGroupProperty.GetValue(); }
      protected set { _tileGroupProperty.SetValue(value); }
    }

    private void OnItemsSourceChanged(AbstractProperty property, object oldValue)
    {
      TileGroup.AttachToItemsList(ItemsSource);
    }

    private void OnCountChanged(AbstractProperty property, object oldValue)
    {
      int count = Count;
      HomeTileGroup tileGroup = TileGroup;
      if (count > tileGroup.TileCount)
      {
        tileGroup.DetachFromItemsList();
        TileGroup = tileGroup = new HomeTileGroup(count);
        tileGroup.AttachToItemsList(ItemsSource);
      }
    }

    public override void Dispose()
    {
      base.Dispose();
      TileGroup.DetachFromItemsList();
    }
  }
}

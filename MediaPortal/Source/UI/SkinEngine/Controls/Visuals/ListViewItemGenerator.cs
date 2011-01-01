#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using System;
using System.Collections.Generic;
using MediaPortal.UI.SkinEngine.Controls.Panels;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  /// <summary>
  /// Item provider which generates list view items based on a given <see cref="Items"/> list.
  /// To generate the items, the <see cref="ItemContainerStyle"/> is applied to <see cref="ListViewItem"/> instances,
  /// using the <see cref="ItemTemplate"/> as content template.
  /// </summary>
  public class ListViewItemGenerator : IDeepCopyable, IItemProvider
  {
    protected DataTemplate _itemTemplate = null;
    protected Style _itemContainerStyle = null;

    protected FrameworkElement _parent = null;
    protected IList<object> _items = null;
    protected int _populatedStartIndex = -1;
    protected int _populatedEndIndex = -1;
    protected IList<FrameworkElement> _materializedItems = null; // Same size as _items, only parts are populated

    public void Dispose()
    {
      DisposeItems();
    }

    public void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      ListViewItemGenerator icg = (ListViewItemGenerator) source;
      _itemTemplate = copyManager.GetCopy(icg._itemTemplate);
      _itemContainerStyle = copyManager.GetCopy(icg._itemContainerStyle);
      _parent = copyManager.GetCopy(icg._parent);
      if (icg._items == null)
        _items = null;
      else
      {
        _items = new List<object>(icg._items.Count);
        foreach (object item in icg._items)
          _items.Add(copyManager.GetCopy(item));
      }
      _populatedStartIndex = icg._populatedStartIndex;
      _populatedEndIndex = icg._populatedEndIndex;
      if (icg._materializedItems == null)
        _materializedItems = null;
      else
      {
        _materializedItems = new List<FrameworkElement>(icg._materializedItems.Count);
        foreach (FrameworkElement item in icg._materializedItems)
          _materializedItems.Add(copyManager.GetCopy(item));
      }
    }

    public void Initialize(FrameworkElement parent, IEnumerable<object> itemsSource, Style itemContainerStyle, DataTemplate itemTemplate)
    {
      _parent = parent;
      if (_materializedItems != null)
        DisposeItems();
      _items = new List<object>(itemsSource);
      _materializedItems = new List<FrameworkElement>(_items.Count);
      for (int i = 0; i < _items.Count; i++)
        _materializedItems.Add(null);
      _itemContainerStyle = itemContainerStyle;
      _itemTemplate = itemTemplate;
    }

    /// <summary>
    /// Gets the underlaying items list.
    /// </summary>
    public IList<object> Items
    {
      get { return _items; }
    }

    /// <summary>
    /// Gets the Style that is applied to the container element generated for each item.
    /// </summary>
    public Style ItemContainerStyle
    {
      get { return _itemContainerStyle; }
    }

    /// <summary>
    /// Gets the DataTemplate used to display each item.
    /// </summary>
    public DataTemplate ItemTemplate
    {
      get { return _itemTemplate; }
    }

    protected void DisposeItems()
    {
      if (_materializedItems == null)
        return;
      DisposeItems(0, _materializedItems.Count - 1);
      _materializedItems = null;
    }

    protected void DisposeItems(int start, int end)
    {
      if (start < 0)
        start = 0;
      if (end >= _materializedItems.Count)
        end = _materializedItems.Count - 1;
      for (int i = start; i <= end; i++)
      {
        FrameworkElement element = _materializedItems[i];
        _materializedItems[i] = null;
        if (element != null)
          element.CleanupAndDispose();
      }
    }

    protected FrameworkElement PrepareItem(object dataItem, FrameworkElement visualParent)
    {
// ReSharper disable UseObjectOrCollectionInitializer
      ListViewItem result = new ListViewItem
// ReSharper restore UseObjectOrCollectionInitializer
        {
            Context = dataItem,
            Content = dataItem,
            Screen = _parent.Screen,
            VisualParent = visualParent
        };
      // Set this after the other properties have been initialized to avoid duplicate work
      result.Style = ItemContainerStyle;

      // We need to copy the item data template for the child containers, because the
      // data template contains specific data for each container. We need to "personalize" the
      // data template copy by assigning its LogicalParent.
      IEnumerable<IBinding> deferredBindings;
      DataTemplate childItemTemplate = MpfCopyManager.DeepCopyCutLP(ItemTemplate, out deferredBindings);
      childItemTemplate.LogicalParent = result;
      result.ContentTemplate = childItemTemplate;
      MpfCopyManager.ActivateBindings(deferredBindings);
      return result;
    }

    public int NumItems
    {
      get { return _items.Count; }
    }

    public void Keep(int start, int end)
    {
      if (_populatedStartIndex != -1 && _populatedStartIndex < start)
      {
        int disposeEnd = Math.Min(_populatedEndIndex, start - 1);
        DisposeItems(_populatedStartIndex, disposeEnd);
        _populatedStartIndex = start;
      }
      if (_populatedEndIndex != -1 && _populatedEndIndex > end)
      {
        int disposeStart = Math.Max(_populatedStartIndex, end + 1);
        DisposeItems(disposeStart, _populatedEndIndex);
        _populatedEndIndex = end;
      }
      if (_populatedStartIndex > _populatedEndIndex)
      {
        _populatedStartIndex = -1;
        _populatedEndIndex = -1;
      }
    }

    public FrameworkElement GetOrCreateItem(int index, FrameworkElement visualParent, out bool newCreated)
    {
      FrameworkElement result = _materializedItems[index];
      if (result != null)
      {
        newCreated = false;
        return result;
      }
      newCreated = true;
      result = _materializedItems[index] = PrepareItem(_items[index], visualParent);
      if (_populatedStartIndex == -1 || _populatedEndIndex == -1)
      {
        _populatedStartIndex = index;
        _populatedEndIndex = index;
      }
      else
      {
        if (index < _populatedStartIndex)
          _populatedStartIndex = index;
        else if (index > _populatedEndIndex)
          _populatedEndIndex = index;
      }
      return result;
    }
  }
}
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

using System;
using System.Collections.Generic;
using MediaPortal.UI.SkinEngine.Controls.Panels;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.MarkupExtensions;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  /// <summary>
  /// Item provider which generates list view items based on a given <see cref="Items"/> list.
  /// To generate the items, the <see cref="ItemContainerStyle"/> is applied to <see cref="ListViewItem"/> instances,
  /// using the <see cref="ItemTemplate"/> as content template.
  /// </summary>
  public class ListViewItemGenerator : IDeepCopyable, IGroupedItemProvider, ISkinEngineManagedObject
  {
    protected DataTemplate _itemTemplate = null;
    protected Style _itemContainerStyle = null;

    protected IGroupingValueProvider _groupingValueProvider;
    protected IBinding _groupPropertyBinding;
    protected DataTemplate _groupHeaderTemplate;
    protected Style _groupHeaderContainerStyle;

    protected FrameworkElement _parent = null;
    protected IList<object> _items = null;
    protected int _populatedStartIndex = -1;
    protected int _populatedEndIndex = -1;
    protected IList<FrameworkElement> _materializedItems = null; // Same size as _items, only parts are populated
    protected HeaderItemWrapper[] _materializedGroupHeaders;

    protected class HeaderItemWrapper
    {
      public object GroupingValue { get; private set; }
      public FrameworkElement HeaderItem { get; set; }

      public HeaderItemWrapper(object groupingValue)
      {
        GroupingValue = groupingValue;
      }

      public HeaderItemWrapper GetCopy(ICopyManager copyManager)
      {
        return new HeaderItemWrapper(GroupingValue)
        {
          HeaderItem = copyManager.GetCopy(HeaderItem)
        };
      }

      public void CleanupAndDispose()
      {
        if (HeaderItem != null)
          HeaderItem.CleanupAndDispose();
      }
    }

    public void Dispose()
    {
      DisposeItems();
      MPF.TryCleanupAndDispose(_itemTemplate);
      _itemTemplate = null;
      MPF.TryCleanupAndDispose(_itemContainerStyle);
      _itemContainerStyle = null;

      _groupingValueProvider = null;
      MPF.TryCleanupAndDispose(_groupPropertyBinding);
      _groupPropertyBinding = null;
      MPF.TryCleanupAndDispose(_groupHeaderTemplate);
      _groupHeaderTemplate = null;
      MPF.TryCleanupAndDispose(_groupHeaderContainerStyle);
      _groupHeaderContainerStyle = null;

      MPF.TryCleanupAndDispose(_getValueGroupHeader);
      _getValueGroupHeader = null;
    }

    public void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      ListViewItemGenerator icg = (ListViewItemGenerator)source;
      _itemTemplate = copyManager.GetCopy(icg._itemTemplate);
      _itemContainerStyle = copyManager.GetCopy(icg._itemContainerStyle);

      _groupingValueProvider = icg._groupingValueProvider;
      _groupPropertyBinding = copyManager.GetCopy(icg._groupPropertyBinding);
      _groupHeaderTemplate = copyManager.GetCopy(icg._groupHeaderTemplate);
      _groupHeaderContainerStyle = copyManager.GetCopy(icg._groupHeaderContainerStyle);

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
      if (icg._materializedGroupHeaders == null)
      {
        _materializedGroupHeaders = null;
      }
      else
      {
        _materializedGroupHeaders = new HeaderItemWrapper[icg._materializedGroupHeaders.Length];
        for (int n = 0; n < icg._materializedGroupHeaders.Length; ++n)
        {
          _materializedGroupHeaders[n] = copyManager.GetCopy(icg._materializedGroupHeaders[n]);
        }
      }
    }

    public void Initialize(FrameworkElement parent, IEnumerable<object> itemsSource, Style itemContainerStyle, DataTemplate itemTemplate)
    {
      Initialize(parent, itemsSource, itemContainerStyle, itemTemplate, null, null, null, null);
    }

    public void Initialize(FrameworkElement parent, IEnumerable<object> itemsSource, Style itemContainerStyle, DataTemplate itemTemplate,
      IGroupingValueProvider groupingValueProvider, IBinding groupPropertyBinding, Style groupHeaderContainerStyle, DataTemplate groupHeaderTemplate)
    {
      _parent = parent;

      DisposeItems();
      _items = new List<object>(itemsSource);
      _materializedItems = new List<FrameworkElement>(_items.Count);
      for (int i = 0; i < _items.Count; i++)
        _materializedItems.Add(null);
      _groupInfos = null;

      if ((groupingValueProvider != null && groupingValueProvider.IsGroupingActive) || groupPropertyBinding != null)
      {
        _materializedGroupHeaders = new HeaderItemWrapper[_items.Count];
      }

      MPF.TryCleanupAndDispose(_itemContainerStyle);
      MPF.TryCleanupAndDispose(_itemTemplate);
      // No need to set the LogicalParent at styles or data templates because they don't bind bindings
      _itemContainerStyle = MpfCopyManager.DeepCopyCutLVPs(itemContainerStyle);
      _itemTemplate = MpfCopyManager.DeepCopyCutLVPs(itemTemplate);

      MPF.TryCleanupAndDispose(_groupHeaderContainerStyle);
      MPF.TryCleanupAndDispose(_groupHeaderTemplate);
      MPF.TryCleanupAndDispose(_groupPropertyBinding);
      _groupingValueProvider = groupingValueProvider;
      _groupPropertyBinding = MpfCopyManager.DeepCopyCutLVPs(groupPropertyBinding);
      _groupHeaderContainerStyle = MpfCopyManager.DeepCopyCutLVPs(groupHeaderContainerStyle);
      _groupHeaderTemplate = MpfCopyManager.DeepCopyCutLVPs(groupHeaderTemplate);

      MPF.TryCleanupAndDispose(_getValueGroupHeader);
      _getValueGroupHeader = null;
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

    /// <summary>
    /// Gets the Style that is applied to the container element generated for each item.
    /// </summary>
    public Style GroupHeaderContainerStyle
    {
      get { return _groupHeaderContainerStyle; }
    }

    /// <summary>
    /// Gets the DataTemplate used to display group headers.
    /// </summary>
    public DataTemplate GroupHeaderTemplate
    {
      get { return _groupHeaderTemplate; }
    }

    protected void DisposeItems()
    {
      if (_materializedItems == null)
        return;
      DisposeItems(0, _materializedItems.Count - 1);
      _populatedStartIndex = -1;
      _populatedEndIndex = -1;
      _materializedItems = null;
      _materializedGroupHeaders = null;
    }

    protected void DisposeItems(int start, int end)
    {
      if (_materializedItems != null)
      {
        if (start < 0)
          start = 0;
        if (end >= _materializedItems.Count)
          end = _materializedItems.Count - 1;
      }
      for (int i = start; i <= end; i++)
      {
        if (_materializedItems != null)
        {
          FrameworkElement element = _materializedItems[i];
          _materializedItems[i] = null;
          if (element != null)
            element.CleanupAndDispose();
        }
        if (_materializedGroupHeaders != null)
        {
          var header = _materializedGroupHeaders[i];
          _materializedGroupHeaders[i] = null;
          if (header != null)
            header.CleanupAndDispose();
        }
      }
    }

    protected FrameworkElement PrepareItem(object dataItem, FrameworkElement lvParent, int index)
    {
      // ReSharper disable UseObjectOrCollectionInitializer
      ListViewItem result = new ListViewItem
// ReSharper restore UseObjectOrCollectionInitializer
        {
            Context = dataItem,
            Content = dataItem,
            Screen = _parent.Screen,
            VisualParent = lvParent,
            LogicalParent = lvParent,
            ItemIndex = index
        };
      // Set this after the other properties have been initialized to avoid duplicate work
      // No need to set the LogicalParent because styles and content templates don't bind bindings
      result.Style = MpfCopyManager.DeepCopyCutLVPs(ItemContainerStyle);
      result.ContentTemplate = MpfCopyManager.DeepCopyCutLVPs(ItemTemplate);
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

    public FrameworkElement GetOrCreateItem(int index, FrameworkElement lvParent, out bool newCreated)
    {
      if (_materializedItems == null || index < 0 || index >= _materializedItems.Count)
      {
        newCreated = false;
        return null;
      }
      FrameworkElement result = _materializedItems[index];
      if (result != null)
      {
        newCreated = false;
        return result;
      }
      newCreated = true;
      result = _materializedItems[index] = PrepareItem(_items[index], lvParent, index);
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

    public FrameworkElement GetOrCreateGroupHeader(int itemIndex, bool isFirstVisibleItem, FrameworkElement lvParent, out bool newCreated)
    {
      if (_materializedGroupHeaders == null || itemIndex < 0 || itemIndex >= _materializedGroupHeaders.Length)
      {
        newCreated = false;
        return null;
      }

      var headerWrapper = GetGroupHeader(itemIndex, isFirstVisibleItem);
      if (headerWrapper != null)
        return GetOrCreateGroupHeader(itemIndex, headerWrapper, lvParent, out newCreated);
      newCreated = false;
      return null;
    }

    public bool IsGroupingActive
    {
      get { return _materializedGroupHeaders != null; }
    }

    private List<GroupInfo> _groupInfos;

    private void BuildGroupInfo()
    {
      if (_groupInfos != null)
        return;
      _groupInfos = new List<GroupInfo>();
      if(_items == null || _items.Count > 0)
        return;
      int first = 0;
      var firstHeaderWrapper = GetGroupHeader(0);
      object firstValue = firstHeaderWrapper == null ? null : firstHeaderWrapper.GroupingValue;
      for (int n = 1; n < _items.Count; ++n)
      {
        var thisHeaderWrapper = GetGroupHeader(n);
        object thisValue = thisHeaderWrapper == null ? null : thisHeaderWrapper.GroupingValue;
        if (!Equals(firstValue, thisValue))
        {
          _groupInfos.Add(new GroupInfo(first, n - 1));
          firstValue = thisValue;
          first = n;
        }
      }
      _groupInfos.Add(new GroupInfo(first, _items.Count - 1));
    }

    public int GroupCount
    {
      get
      {
        BuildGroupInfo();
        return _groupInfos.Count;
      }
    }

    public GroupInfo GetGroupInfo(int groupIndex)
    {
      BuildGroupInfo();
      if (groupIndex < 0 || groupIndex >= _groupInfos.Count)
        return new GroupInfo(0, -1);
      return _groupInfos[groupIndex];
    }

    public int GetGroupIndex(int itemIndex)
    {
      BuildGroupInfo();
      // assuming that the items are equally distributed over the groups we guess in which group the item may be and search then from there
      int groupIndex = itemIndex / (_items.Count / _groupInfos.Count);
      while (groupIndex >= 0 && groupIndex < _groupInfos.Count)
      {
        if (itemIndex < _groupInfos[groupIndex].FirstItem)
        {
          if (groupIndex > 0)
          {
            --groupIndex;
          }
          else
          {
            break;
          }
        }
        else if (itemIndex > _groupInfos[groupIndex].LastItem)
        {
          if (groupIndex < _groupInfos.Count - 1)
          {
            ++groupIndex;
          }
          else
          {
            break;
          }
        }
        else
        {
          return groupIndex;
        }
      }
      return -1;
    }

    public GroupInfo GetGroupInfoFromItem(int itemIndex)
    {
      var groupIndex = GetGroupIndex(itemIndex);
      if (groupIndex < 0)
        return new GroupInfo(0, -1);
      return _groupInfos[groupIndex];
    }

    protected FrameworkElement PrepareGroupHeader(GroupHeaderItem headerItem, FrameworkElement lvParent)
    {
      var result = new ListViewGroupHeader()
      {
        Context = headerItem,
        Content = headerItem,
        Screen = _parent.Screen,
        VisualParent = lvParent,
        LogicalParent = lvParent
      };
      // Set this after the other properties have been initialized to avoid duplicate work
      // No need to set the LogicalParent because styles and content templates don't bind bindings
      result.Style = MpfCopyManager.DeepCopyCutLVPs(GroupHeaderContainerStyle);
      result.ContentTemplate = MpfCopyManager.DeepCopyCutLVPs(GroupHeaderTemplate);
      return result;
    }

    private FrameworkElement GetOrCreateGroupHeader(int itemIndex, HeaderItemWrapper headerWrapper, FrameworkElement lvParent, out bool newCreated)
    {
      if (headerWrapper.HeaderItem != null)
      {
        newCreated = false;
        return headerWrapper.HeaderItem;
      }
      var headerItem = new GroupHeaderItem()
      {
        FirstItem = _items[itemIndex],
        GroupingValue = headerWrapper.GroupingValue
      };

      newCreated = true;
      var result = PrepareGroupHeader(headerItem, lvParent);
      headerItem.LogicalParent = result;
      headerWrapper.HeaderItem = result;
      return result;
    }

    private HeaderItemWrapper GetGroupHeader(int itemIndex, bool isFirstVisibleItem)
    {
      var headerWrapper = GetGroupHeader(itemIndex);
      if (isFirstVisibleItem)
      {
        // if the item is the 1st visible item, then we search the header for 1st item in the group
        while (headerWrapper.HeaderItem == null && GroupValueEquals(headerWrapper, itemIndex - 1))
        {
          itemIndex--;
        }
        return GetGroupHeader(itemIndex);
      }
      if (!GroupValueEquals(headerWrapper, itemIndex - 1))
      {
        return headerWrapper;
      }
      return null;
    }

    private HeaderItemWrapper GetGroupHeader(int itemIndex)
    {
      var headerWrapper = _materializedGroupHeaders[itemIndex];
      if (headerWrapper != null)
      {
        return headerWrapper;
      }

      if (_groupingValueProvider != null)
      {
        headerWrapper = new HeaderItemWrapper(_groupingValueProvider.GetGroupingValue(_items[itemIndex]));
      }
      else
      {
        // to get the grouping value of the item we use a dummy header item and apply the DataContext and group value binding to it
        if (_getValueGroupHeader == null)
        {
          _getValueGroupHeader = new GroupHeaderItem();
          var dd = new SimplePropertyDataDescriptor(_getValueGroupHeader, typeof(GroupHeaderItem).GetProperty("GroupingValue"));
          var binding = MpfCopyManager.DeepCopyCutLVPs(_groupPropertyBinding);
          binding.SetTargetDataDescriptor(dd);
          binding.Activate();
        }

        _getValueGroupHeader.DataContext = new BindingExtension()
        {
          Source = _items[itemIndex],
          Path = "."
        };
        // then we create the actual header item and apply the value to it
        headerWrapper = new HeaderItemWrapper(_getValueGroupHeader.GroupingValue);

        // finally cleanup the datacontext binding
        MPF.TryCleanupAndDispose(_getValueGroupHeader.DataContext);
        _getValueGroupHeader.DataContext = null;
      }
      _materializedGroupHeaders[itemIndex] = headerWrapper;
      return headerWrapper;
    }

    private bool GroupValueEquals(HeaderItemWrapper headerWrapperA, int itemIndexB)
    {
      if (itemIndexB < 0)
        return false;
      var headerWrapperB = GetGroupHeader(itemIndexB);
      return headerWrapperA != null && headerWrapperB != null && Equals(headerWrapperA.GroupingValue, headerWrapperB.GroupingValue);
    }

    private GroupHeaderItem _getValueGroupHeader;
  }
}

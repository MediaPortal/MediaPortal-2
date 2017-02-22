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

using System.Collections.Generic;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.MediaManagement;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.SecondaryFilter;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Exceptions;
using System;
using System.Linq;

namespace MediaPortal.UiComponents.Media.Models.ScreenData
{
  public abstract class AbstractScreenData
  {
    #region Protected fields

    protected string _menuItemLabel;
    protected string _screen;

    // Lazily initialized
    protected ItemsList _items = null;
    protected ItemsList _originalList = null;
    protected AbstractProperty _numItemsStrProperty = null;
    protected AbstractProperty _numItemsProperty = null;
    protected AbstractProperty _totalNumItemsProperty = null;
    protected AbstractProperty _isFilteredProperty;
    protected AbstractProperty _isItemsValidProperty = null;
    protected AbstractProperty _isItemsEmptyProperty = null;
    protected AbstractProperty _tooManyItemsProperty = null;
    protected AbstractProperty _showListProperty = null;
    protected AbstractProperty _showListHintProperty = null;
    protected AbstractProperty _listHintProperty = null;
    protected NavigationData _navigationData = null;
    protected IItemsFilter _filter;
    protected IEnumerable<Guid> _filteredMias;
    protected IEnumerable<Guid> _availableMias;

    protected object _syncObj = new object();

    #endregion

    protected AbstractScreenData(string screen, string menuItemLabel)
    {
      _screen = screen;
      _menuItemLabel = menuItemLabel;
      _filter = new RemoteNumpadFilter();
    }

    /// <summary>
    /// Item label to be shown in the menu which leads to this screen.
    /// </summary>
    public string MenuItemLabel
    {
      get { return _menuItemLabel; }
    }

    /// <summary>
    /// Gets the screen which is currently being shown by this model.
    /// </summary>
    public string Screen
    {
      get { return _screen; }
    }

    /// <summary>
    /// Returns a hint text that more than our maximum number of shown items should be preented.
    /// Can be overridden in sub classes to modify the text to be shown.
    /// </summary>
    public virtual string MoreThanMaxItemsHint
    {
      get { return LocalizationHelper.Translate(Consts.RES_MORE_THAN_MAX_ITEMS_HINT, Consts.MAX_NUM_ITEMS_VISIBLE); }
    }

    /// <summary>
    /// Returns a hint text notifying the user that the items list is currently being built (and not ready yet).
    /// Can be overridden in sub classes to modify the text to be shown.
    /// </summary>
    public virtual string ListBeingBuiltHint
    {
      get { return Consts.RES_LIST_BEING_BUILT_HINT; }
    }

    #region Lazy initialized properties

    /// <summary>
    /// In case the current screen shows browse local media, browse media library, audio, videos or images, this property provides a list
    /// with the sub views and media items of the current view. In case the current screen shows a list of filter
    /// value of a choosen filter criteria, this property provides a list of available filter values.
    /// </summary>
    public ItemsList Items
    {
      get { return _items; }
    }

    public AbstractProperty NumItemsStrProperty
    {
      get { return _numItemsStrProperty; }
    }

    /// <summary>
    /// Gets a string like "No items", "One item" or "10 items". The number reflects the number of of items in the
    /// <see cref="Items"/> list.
    /// </summary>
    public string NumItemsStr
    {
      get { return (string) _numItemsStrProperty.GetValue(); }
      protected set { _numItemsStrProperty.SetValue(value); }
    }

    public AbstractProperty NumItemsProperty
    {
      get { return _numItemsProperty; }
    }

    /// <summary>
    /// Gets the absolute number of items in the <see cref="Items"/> list.
    /// </summary>
    public int NumItems
    {
      get { return (int) _numItemsProperty.GetValue(); }
      protected set { _numItemsProperty.SetValue(value); }
    }

    public AbstractProperty TotalNumItemsProperty
    {
      get { return _totalNumItemsProperty; }
    }

    /// <summary>
    /// Gets the total number of all items that are affected by current list.
    /// </summary>
    public int? TotalNumItems
    {
      get { return (int?)_totalNumItemsProperty.GetValue(); }
      protected set { _totalNumItemsProperty.SetValue(value); }
    }

    public AbstractProperty IsFilteredProperty
    {
      get { return _isFilteredProperty; }
    }

    /// <summary>
    /// Gets the information whether the current view is filtered by a secondary filter.
    /// </summary>
    public bool IsFiltered
    {
      get { return (bool)_isFilteredProperty.GetValue(); }
      protected set { _isFilteredProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets the information whether the current view is valid, i.e. its content could be built and <see cref="Items"/>
    /// contains the items of the current view.
    /// </summary>
    public bool IsItemsValid
    {
      get { return (bool) _isItemsValidProperty.GetValue(); }
      protected set { _isItemsValidProperty.SetValue(value); }
    }

    public AbstractProperty ShowListProperty
    {
      get { return _showListProperty; }
    }

    /// <summary>
    /// Gets the information whether the skin should show the items list.
    /// </summary>
    public bool ShowList
    {
      get { return (bool) _showListProperty.GetValue(); }
      protected set { _showListProperty.SetValue(value); }
    }

    public AbstractProperty ShowListHintProperty
    {
      get { return _showListHintProperty; }
    }

    /// <summary>
    /// Gets the information whether the skin should show a hint text.
    /// </summary>
    public bool ShowListHint
    {
      get { return (bool) _showListHintProperty.GetValue(); }
      protected set { _showListHintProperty.SetValue(value); }
    }

    public AbstractProperty ListHintProperty
    {
      get { return _listHintProperty; }
    }

    /// <summary>
    /// Gets a string like "Blabla too many items, please add filters, blabla" to be shown by the skin if
    /// <see cref="ShowListHint"/> is set to <c>true</c>.
    /// </summary>
    public string ListHint
    {
      get { return (string) _listHintProperty.GetValue(); }
      protected set { _listHintProperty.SetValue(value); }
    }

    public AbstractProperty IsItemsEmptyProperty
    {
      get { return _isItemsEmptyProperty; }
    }

    /// <summary>
    /// Gets the information whether the current view is empty, i.e. <see cref="Items"/>'s count is <c>0</c>.
    /// </summary>
    public bool IsItemsEmpty
    {
      get { return (bool) _isItemsEmptyProperty.GetValue(); }
      protected set { _isItemsEmptyProperty.SetValue(value); }
    }

    public AbstractProperty TooManyItemsProperty
    {
      get { return _tooManyItemsProperty; }
    }

    /// <summary>
    /// Gets the information whether there are too many items in the current view.
    /// </summary>
    public bool TooManyItems
    {
      get { return (bool) _tooManyItemsProperty.GetValue(); }
      protected set { _tooManyItemsProperty.SetValue(value); }
    }

    #endregion

    public bool IsEnabled
    {
      get { return _navigationData != null; }
    }

    public Sorting.Sorting CurrentSorting
    {
      get
      {
        Sorting.Sorting result;
        lock (_syncObj)
        {
          NavigationData nd = _navigationData;
          result = nd == null ? null : nd.CurrentSorting;
        }
        return result;
      }
    }

    /// <summary>
    /// Returns the minimum mias that are guaranteed to be present in the items displayed by this screen.
    /// </summary>
    public IEnumerable<Guid> FilteredMias
    {
      get { return _filteredMias; }
    }

    /// <summary>
    /// Returns the mias that this screen has available.
    /// </summary>
    public IEnumerable<Guid> AvailableMias
    {
      get { return _availableMias; }
    }

    /// <summary>
    /// Invalidates the underlaying view and reloads all sub views and items.
    /// </summary>
    public abstract void Reload();

    /// <summary>
    /// Rebuilds the items list without invalidating the underlaying view.
    /// </summary>
    public abstract void UpdateItems();

    /// <summary>
    /// Whether this screen can filter items shown by the <paramref name="parentScreen"/>.
    /// The default implementation checks whether at least one of the <see cref="FilteredMias"/> is present in the <paramref name="parentScreen"/>'s <see cref="FilteredMias"/>
    /// or whether <see cref="FilteredMias"/> is null on this or the parent screen.
    /// Can be overriden in derived classes.
    /// </summary>
    /// <param name="parentScreen">The screen that is currently shown.</param>
    /// <returns>True if this screen can handle items shown by the <paramref name="parentScreen"/></returns>
    public virtual bool CanFilter(AbstractScreenData parentScreen)
    {
      return _filteredMias == null || parentScreen == null || parentScreen.FilteredMias == null || _filteredMias.Intersect(parentScreen.FilteredMias).Count() > 0;
    }

    /// <summary>
    /// Allows a secondary filter of the already loaded <see cref="Items"/> by the given <paramref name="search"/> term.
    /// </summary>
    /// <param name="search">Search term (or key)</param>
    public virtual void ApplySecondaryFilter(string search)
    {
      IItemsFilter filter = _filter;
      if (filter == null)
        return;

      // Init backup list
      if (_originalList == null)
      {
        _originalList = new ItemsList();
        CollectionUtils.AddAll(_originalList, _items);
      }
      filter.Filter(_items, _originalList, search);

      IsFiltered = filter.IsFiltered;
      if (IsFiltered)
        // Filter defined by class
        NumItemsStr = filter.Text;
      else
        // Restore default text
        NumItemsStr = Utils.BuildNumItemsStr(NumItems, TotalNumItems);
    }

    /// <summary>
    /// Updates all data which is needed by the skin. That is all properties in the region "Lazy initialized properties"
    /// and all properties from sub classes. After calling this method, the UI screen will be shown.
    /// </summary>
    public virtual void CreateScreenData(NavigationData navigationData)
    {
      if (IsEnabled)
        throw new IllegalCallException("Screen data is already initialized");
      _navigationData = navigationData;
      _numItemsStrProperty = new WProperty(typeof(string), string.Empty);
      _numItemsProperty = new WProperty(typeof(int), 0);
      _totalNumItemsProperty = new WProperty(typeof(int?), 0);
      _isFilteredProperty = new WProperty(typeof(bool), false);
      _isItemsValidProperty = new WProperty(typeof(bool), true);
      _isItemsEmptyProperty = new WProperty(typeof(bool), true);
      _tooManyItemsProperty = new WProperty(typeof(bool), false);
      _showListProperty = new WProperty(typeof(bool), true);
      _showListHintProperty = new WProperty(typeof(bool), false);
      _listHintProperty = new WProperty(typeof(string), string.Empty);
    }

    /// <summary>
    /// Releases all data which was created in method <see cref="CreateScreenData"/>. This method will be called when the
    /// screen is no longer needed. After calling this method, <see cref="CreateScreenData"/> might be called again to
    /// re-create the screen.
    /// </summary>
    public virtual void ReleaseScreenData()
    {
      _navigationData = null;
      _numItemsStrProperty = null;
      _numItemsProperty = null;
      _isItemsValidProperty = null;
      _isItemsEmptyProperty = null;
      _tooManyItemsProperty = null;
      _showListProperty = null;
      _showListHintProperty = null;
      _listHintProperty = null;
      if (_items != null)
        _items.Clear();
      _items = null;
    }

    /// <summary>
    /// Returns all media items of the current screen and all sub-screens recursively.
    /// </summary>
    /// <returns>Enumeration of media items.</returns>
    public IEnumerable<MediaItem> GetAllMediaItems()
    {
      List<MediaItem> result = new List<MediaItem>(GetAllMediaItemsOverride());
      Sorting.Sorting sorting = CurrentSorting;
      if (sorting != null)
        result.Sort(sorting);
      return result;
    }

    protected virtual IEnumerable<MediaItem> GetAllMediaItemsOverride()
    {
      // Actually, this method doesn't need to be virtual because the code here is very generic -
      // depending on the base view specification of the current screen, we collect all items.
      // But screens like the search screen modify their list of items dynamically. Such screens must
      // return their dynamically generated items list.
      return _navigationData.BaseViewSpecification.GetAllMediaItems();
    }

    protected virtual void Display_ListBeingBuilt()
    {
      ShowList = false;
      TooManyItems = false;
      IsItemsEmpty = false;
      ListHint = ListBeingBuiltHint;
      ShowListHint = true;
      IsItemsValid = true;
      NumItemsStr = "?";
      NumItems = 0;
      TotalNumItems = null;
      IsFiltered = false;
    }

    protected virtual void Display_TooManyItems(int numItems)
    {
      ShowList = false;
      TooManyItems = true;
      IsItemsEmpty = false;
      ListHint = MoreThanMaxItemsHint;
      ShowListHint = true;
      IsItemsValid = true;
      NumItemsStr = Utils.BuildNumItemsStr(numItems, null);
      NumItems = numItems;
      TotalNumItems = null;
      IsFiltered = false;
    }

    protected virtual void Display_Normal(int numItems, int? total)
    {
      ShowList = true;
      TooManyItems = false;
      if (numItems == 0)
      {
        IsItemsEmpty = true;
        ListHint = Consts.RES_VIEW_EMPTY;
        ShowListHint = true;
      }
      else
      {
        IsItemsEmpty = false;
        ListHint = string.Empty;
        ShowListHint = false;
      }
      IsItemsValid = true;

      NumItemsStr = Utils.BuildNumItemsStr(numItems, total);

      NumItems = numItems;
      TotalNumItems = total;
      IsFiltered = false;
    }

    protected virtual void Display_ItemsInvalid()
    {
      IsItemsValid = false;
      IsItemsEmpty = false;
      TooManyItems = false;
      ShowList = false;
      ShowListHint = false;
      ListHint = string.Empty;
      NumItemsStr = "-";
      NumItems = 0;
      TotalNumItems = null;
      IsFiltered = false;
    }
  }
}

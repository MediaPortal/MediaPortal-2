#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.UiComponents.Media.Models.ScreenData
{
  public abstract class AbstractScreenData
  {
    #region Protected fields

    protected string _menuItemLabel;
    protected string _screen;

    // Lazily initialized
    protected ItemsList _items = null;
    protected AbstractProperty _numItemsStrProperty = null;
    protected AbstractProperty _isItemsValidProperty = null;
    protected AbstractProperty _isItemsEmptyProperty = null;
    protected AbstractProperty _tooManyItemsProperty = null;
    protected AbstractProperty _showListProperty = null;
    protected AbstractProperty _showListHintProperty = null;
    protected AbstractProperty _listHintProperty = null;
    protected NavigationData _navigationData = null;

    #endregion

    protected AbstractScreenData(string screen, string menuItemLabel)
    {
      _screen = screen;
      _menuItemLabel = menuItemLabel;
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
    /// In case the current screen shows local media, music, movies or pictures, this property provides a list
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

    public AbstractProperty IsItemsValidProperty
    {
      get { return _isItemsValidProperty; }
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

    public abstract void Reload();

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
    public virtual IEnumerable<MediaItem> GetAllMediaItems()
    {
      // Normally, this method doesn't need to be virtual because the code here is very generic -
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
    }
  }
}
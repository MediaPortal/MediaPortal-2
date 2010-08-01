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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core.General;
using MediaPortal.Core.Localization;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.UiComponents.Media.Models.ScreenData
{
  public abstract class AbstractScreenData
  {
    public const int MAX_NUM_ITEMS = 500;

    #region Protected fields

    protected string _menuItemLabel;
    protected string _screen;

    // Lazily initialized
    protected ItemsList _items = null;
    protected AbstractProperty _numItemsStrProperty = null;
    protected AbstractProperty _isItemsValidProperty = null;
    protected AbstractProperty _isItemsEmptyProperty = null;
    protected AbstractProperty _tooManyItemsProperty = null;
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

    public string MoreThanMaxItems
    {
      get { return LocalizationHelper.Translate(Consts.MORE_THAN_MAX_ITEMS_RES, MAX_NUM_ITEMS); }
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

    public AbstractProperty TooManyTimesProperty
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

    /// <summary>
    /// Updates all data which is needed by the skin. That is all properties in the region "Lazy initialized properties"
    /// and all properties from sub classes. After calling this method, the UI screen will be shown.
    /// </summary>
    public virtual void CreateScreenData(NavigationData navigationData)
    {
      if (_navigationData != null)
        throw new IllegalCallException("Screen data is already initialized");
      _navigationData = navigationData;
      _numItemsStrProperty = new WProperty(typeof(string), string.Empty);
      _isItemsValidProperty = new WProperty(typeof(bool), true);
      _isItemsEmptyProperty = new WProperty(typeof(bool), true);
      _tooManyItemsProperty = new WProperty(typeof(bool), false);
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
      if (_items != null)
        _items.Clear();
      _items = null;
    }
  }
}
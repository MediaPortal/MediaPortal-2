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
using MediaPortal.UiComponents.SkinBase.General;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  /// <summary>
  /// Container for a group of <see cref="ListItem"/>s to be displayed on the home screen.
  /// </summary>
  public class ItemsListWrapper : ListItem
  {
    protected AbstractProperty _hasItemsProperty = new WProperty(typeof(bool), false);

    protected AbstractProperty _item1Property = new WProperty(typeof(object), null);
    protected AbstractProperty _item2Property = new WProperty(typeof(object), null);
    protected AbstractProperty _item3Property = new WProperty(typeof(object), null);
    protected AbstractProperty _item4Property = new WProperty(typeof(object), null);
    protected AbstractProperty _item5Property = new WProperty(typeof(object), null);
    protected AbstractProperty _item6Property = new WProperty(typeof(object), null);

    protected AbstractProperty[] _itemProperties;

    protected IList<ListItem> _itemsList;
    protected IObservable _observable;

    public ItemsListWrapper(IList<ListItem> itemsList, string name)
      : base(Consts.KEY_NAME, name)
    {
      _itemProperties = new[] { _item1Property, _item2Property, _item3Property, _item4Property, _item5Property, _item6Property };
      AttachToItemsList(itemsList);
    }

    #region GUI Properties

    public AbstractProperty HasItemsProperty
    {
      get { return _hasItemsProperty; }
    }

    public bool HasItems
    {
      get { return (bool)_hasItemsProperty.GetValue(); }
      set { _hasItemsProperty.SetValue(value); }
    }

    public AbstractProperty Item1Property
    {
      get { return _item1Property; }
    }

    public object Item1
    {
      get { return _item1Property.GetValue(); }
      set { _item1Property.SetValue(value); }
    }

    public AbstractProperty Item2Property
    {
      get { return _item2Property; }
    }

    public object Item2
    {
      get { return _item2Property.GetValue(); }
      set { _item2Property.SetValue(value); }
    }

    public AbstractProperty Item3Property
    {
      get { return _item3Property; }
    }

    public object Item3
    {
      get { return _item3Property.GetValue(); }
      set { _item3Property.SetValue(value); }
    }

    public AbstractProperty Item4Property
    {
      get { return _item4Property; }
    }

    public object Item4
    {
      get { return _item4Property.GetValue(); }
      set { _item4Property.SetValue(value); }
    }

    public AbstractProperty Item5Property
    {
      get { return _item5Property; }
    }

    public object Item5
    {
      get { return _item5Property.GetValue(); }
      set { _item5Property.SetValue(value); }
    }

    public AbstractProperty Item6Property
    {
      get { return _item6Property; }
    }

    public object Item6
    {
      get { return _item6Property.GetValue(); }
      set { _item6Property.SetValue(value); }
    }

    #endregion

    public void AttachToItemsList(IList<ListItem> list)
    {
      DetachFromItemsList();

      _itemsList = list;
      IObservable observable = list as IObservable;
      if (observable != null)
        observable.ObjectChanged += OnAttachedItemsChanged;

      UpdateItemProperties();
    }

    public void DetachFromItemsList()
    {
      IObservable observable = _itemsList as IObservable;
      _itemsList = null;
      if (observable != null)
        observable.ObjectChanged -= OnAttachedItemsChanged;
    }

    private void OnAttachedItemsChanged(IObservable observable)
    {
      UpdateItemProperties();
    }

    protected void UpdateItemProperties()
    {
      IList<ListItem> items = _itemsList;
      bool hasItems = items != null && items.Count > 0;

      for (int i = 0; i < _itemProperties.Length; i++)
        _itemProperties[i].SetValue(hasItems && items.Count > i ? items[i] : null);

      HasItems = hasItems;
    }
  }
}

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

    public ItemsListWrapper(IList<ListItem> itemsList)
      : this(itemsList, string.Empty)
    { }

    public ItemsListWrapper(IList<ListItem> itemsList, string name)
      : base(Consts.KEY_NAME, name)
    {
      AdditionalProperties["SubItems"] = itemsList;
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

    #endregion

    public void AttachToItemsList(IList<ListItem> list)
    {
      IObservable observable = list as IObservable;
      if (observable != null)
        observable.ObjectChanged += OnAttachedItemsChanged;

      UpdateHasItemsProperty(list);
    }

    private void OnAttachedItemsChanged(IObservable observable)
    {
      UpdateHasItemsProperty(observable as IList<ListItem>);
    }

    protected void UpdateHasItemsProperty(IList<ListItem> items)
    {
      HasItems = items != null && items.Count > 0;
    }
  }
}

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

namespace MediaPortal.Common.PluginManager
{
  public delegate void ItemRegistrationChangedDlgt(string location, ICollection<PluginItemMetadata> items);

  /// <summary>
  /// General implementation of an <see cref="IItemRegistrationChangeListener"/> which provides delegates for methods
  /// <see cref="IItemRegistrationChangeListener.ItemsWereAdded"/> and <see cref="IItemRegistrationChangeListener.ItemsWereRemoved"/>.
  /// </summary>
  public class DefaultItemRegistrationChangeListener : IItemRegistrationChangeListener
  {
    protected string _usageDescription;
    public ItemRegistrationChangedDlgt _itemsWereAdded = null;
    public ItemRegistrationChangedDlgt _itemsWereRemoved = null;

    public DefaultItemRegistrationChangeListener(string usageDescription)
    {
      _usageDescription = usageDescription;
    }

    public ItemRegistrationChangedDlgt ItemsWereAdded
    {
      get { return _itemsWereAdded; }
      set { _itemsWereAdded = value; }
    }

    public ItemRegistrationChangedDlgt ItemsWereRemoved
    {
      get { return _itemsWereRemoved; }
      set { _itemsWereRemoved = value; }
    }

    public string UsageDescription
    {
      get { return _usageDescription; }
    }

    #region IItemRegistrationChangeListener implementation

    void IItemRegistrationChangeListener.ItemsWereAdded(string location, ICollection<PluginItemMetadata> items)
    {
      ItemRegistrationChangedDlgt dlgt = ItemsWereAdded;
      if (dlgt != null)
        dlgt(location, items);
    }

    void IItemRegistrationChangeListener.ItemsWereRemoved(string location, ICollection<PluginItemMetadata> items)
    {
      ItemRegistrationChangedDlgt dlgt = ItemsWereRemoved;
      if (dlgt != null)
        dlgt(location, items);
    }

    #endregion
  }
}
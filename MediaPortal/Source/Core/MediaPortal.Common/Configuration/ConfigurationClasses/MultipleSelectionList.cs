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

using System.Collections.Generic;
using MediaPortal.Common.Localization;

namespace MediaPortal.Common.Configuration.ConfigurationClasses
{
  /// <summary>
  /// Base class for configuration setting classes for configuring the selection of items in a
  /// predefined list of (localizable) strings.
  /// </summary>
  public abstract class MultipleSelectionList : ConfigItemList
  {
    #region Protected fields

    protected List<int> _selected = new List<int>();

    #endregion

    #region Public properties

    /// <summary>
    /// Gets or sets all indices of selected items.
    /// </summary>
    public IList<int> SelectedIndices
    {
      get { return _selected; }
      set
      {
        _selected.Clear();
        _selected.AddRange(value);
        NotifyChange();
      }
    }

    /// <summary>
    /// Gets all selected items.
    /// </summary>
    public IList<IResourceString> SelectedItems
    {
      get
      {
        List<IResourceString> o = new List<IResourceString>(_selected.Count);
        foreach (int i in _selected)
          o.Add(_items[i]);
        return o.AsReadOnly();
      }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Adds an index to the selection.
    /// </summary>
    /// <param name="index"></param>
    public void AddToSelection(int index)
    {
      if (!_selected.Contains(index))
        _selected.Add(index);
      NotifyChange();
    }

    /// <summary>
    /// Removes an index from the selection.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool RemoveFromSelection(int index)
    {
      bool result = _selected.Remove(index);
      NotifyChange();
      return result;
    }

    /// <summary>
    /// Clears the selection.
    /// </summary>
    public void ClearSelection()
    {
      _selected.Clear();
      NotifyChange();
    }

    #endregion
  }
}

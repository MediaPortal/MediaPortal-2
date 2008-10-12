#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using System.Collections.Generic;

namespace MediaPortal.Configuration.Settings
{
  public abstract class PreferenceList : ConfigItemList
  {
    #region Variables

    protected IList<int> _ranking = new List<int>(0);

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the ranking of the items (indices).
    /// </summary>
    public IList<int> Ranking
    {
      get { return _ranking; }
      set
      {
        _ranking = value;
        NotifyChange();
      }
    }

    /// <summary>
    /// Gets an enumerator to enumerate through the sorted and localised items.
    /// </summary>
    public IEnumerable<string> SortedItems
    {
      get
      {
        IList<string> items = new List<string>(_items.Count);
        lock (_ranking)
        {
          lock (_items)
          {
            foreach (int i in _ranking)       // add ordered items
              items.Add(_items[i].Evaluate());
            if (items.Count < _items.Count)   // add remaining items, if any
            {
              for (int i = 0; i < _items.Count; i++)
              {
                if (!_ranking.Contains(i))
                  items.Add(_items[i].Evaluate());
              }
            }
          }
        }
        return items;
      }
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Further intializes the base where needed.
    /// Always call this after initialization of the inheriting class,
    /// to make sure everything is correctly initialized.
    /// </summary>
    protected void InitializeBase()
    {
      IList<int> ranking = new List<int>();
      // Filter all values to make them unique
      for (int i = _ranking.Count - 1; i >= 0; i--)
      {
        if (_ranking[i] < _items.Count && !ranking.Contains(_ranking[i]))
          ranking.Insert(0, _ranking[i]);
        _ranking.RemoveAt(i);
      }
      // Check if the number of ranked values equals the total amount of values
      // If there is a difference: ranking.Count will always be less, because of the filter
      if (ranking.Count < _items.Count)
      {
        for (int i = 0; i < _items.Count; i++)
        {
          if (!ranking.Contains(i))
            ranking.Add(i);
        }
      }
      _ranking = ranking;
    }

    #endregion
  }
}

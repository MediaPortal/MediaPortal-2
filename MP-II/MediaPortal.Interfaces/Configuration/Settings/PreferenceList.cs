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
  public class PreferenceList : ConfigItemList
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
        this._ranking = value;
        base.NotifyChange();
      }
    }

    /// <summary>
    /// Gets an enumerator to enumerate through the sorted and localised items.
    /// </summary>
    public IEnumerable<string> SortedItems
    {
      get
      {
        List<string> items = new List<string>(base._items.Count);
        lock (this._ranking)
        {
          lock (_items)
          {
            foreach (int i in _ranking)       // add ordered items
              items.Add(base._items[i].ToString());
            if (items.Count < base._items.Count)   // add remaining items, if any
            {
              for (int i = 0; i < _items.Count; i++)
              {
                if (!this._ranking.Contains(i))
                  items.Add(base._items[i].ToString());
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
      List<int> ranking = new List<int>();
      // Filter all values to make them unique
      for (int i = this._ranking.Count - 1; i >= 0; i--)
      {
        if (this._ranking[i] < base._items.Count && !ranking.Contains(this._ranking[i]))
          ranking.Insert(0, this._ranking[i]);
        this._ranking.RemoveAt(i);
      }
      // Check if the number of ranked values equals the total amount of values
      // If there is a difference: ranking.Count will always be less, because of the filter
      if (ranking.Count < base._items.Count)
      {
        for (int i = 0; i < base._items.Count; i++)
        {
          if (!ranking.Contains(i))
            ranking.Add(i);
        }
      }
      this._ranking = ranking;
    }

    #endregion

  }
}

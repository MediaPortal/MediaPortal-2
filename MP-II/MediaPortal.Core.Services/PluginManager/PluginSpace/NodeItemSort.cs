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
 *  Code modified from SharpDevelop AddIn code
 *  Thanks goes to: Mike Krüger
 */

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Logging;

namespace MediaPortal.Services.PluginManager.PluginSpace
{
  /// <summary>
  /// Description of PluginTreeNodeSort.
  /// </summary>
  internal class NodeItemSort
  {
    List<NodeItem> _items;
    bool[] _visited;
    List<NodeItem> _sortedItems;
    Dictionary<string, int> _indexOfName;

    public NodeItemSort(List<NodeItem> items)
    {
      this._items = items;
      _visited = new bool[items.Count];
      _sortedItems = new List<NodeItem>(items.Count);
      _indexOfName = new Dictionary<string, int>(items.Count);
      // initialize visited to false and fill the indexOfName dictionary
      for (int i = 0; i < items.Count; ++i)
      {
        _visited[i] = false;
        _indexOfName[items[i].Id] = i;
      }
    }

    void InsertEdges()
    {
      // add the InsertBefore to the corresponding InsertAfter
      for (int i = 0; i < _items.Count; ++i)
      {
        string before = _items[i].InsertBefore;
        if (!String.IsNullOrEmpty(before))
        {
          if (_indexOfName.ContainsKey(before))
          {
            string after = _items[_indexOfName[before]].InsertAfter;
            if (String.IsNullOrEmpty(after))
            {
              _items[_indexOfName[before]].InsertAfter = _items[i].Id;
            }
            else
            {
              _items[_indexOfName[before]].InsertAfter = after + ',' + _items[i].Id;
            }
          }
          else
          {
            ServiceScope.Get<ILogger>().Warn("Codon ({0}) specified in the insertbefore of the {1} codon does not exist!", before, _items[i]);
          }
        }
      }
    }

    public List<NodeItem> Execute()
    {
      InsertEdges();

      // Visit all codons
      for (int i = 0; i < _items.Count; ++i)
      {
        Visit(i);
      }
      return _sortedItems;
    }

    void Visit(int itemIndex)
    {
      if (_visited[itemIndex])
      {
        return;
      }
      string[] after = _items[itemIndex].InsertAfter.Split(new char[] { ',' });
      foreach (string s in after)
      {
        if (s == null || s.Length == 0)
        {
          continue;
        }
        if (_indexOfName.ContainsKey(s))
        {
          Visit(_indexOfName[s]);
        }
        else
        {
          ServiceScope.Get<ILogger>().Warn("Codon ({0}) specified in the insertafter of the {1} codon does not exist!", _items[itemIndex].InsertAfter, _items[itemIndex]);
        }
      }
      _sortedItems.Add(_items[itemIndex]);
      _visited[itemIndex] = true;
    }
  }
}

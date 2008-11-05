#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Diagnostics;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Media.MetaData;
using MediaPortal.Media.MediaManagement;
using MediaPortal.Media.MediaManagement.Views;

namespace Components.Services.MediaManager.Views
{
  public class ViewNavigator
  {
    private View _view;
    private int _currentSubView;
    private readonly IList<IMediaItem> _history;

    public ViewNavigator(View view)
    {
      _view = view;
      _history = new List<IMediaItem>();
    }

    public View View
    {
      get
      {
        return _view;
      }
    }

    /// <summary>
    /// Gets the index of the current subview.
    /// </summary>
    /// <value>The index of the current subview.</value>
    public int CurrentViewIndex
    {
      get { return _currentSubView; }
    }

    /// <summary>
    /// Loads the first subview and returns a list of all items found.
    /// </summary>
    /// <returns>a list containing all items found</returns>
    public IList<IAbstractMediaItem> Get(IRootContainer root, IRootContainer parent)
    {
      //select first subview and clear the history
      _currentSubView = 0;
      _history.Clear();
      return Get(null, root, parent);
    }

    /// <summary>
    /// Loads the next subview for the selected item and
    /// returns a list of all item found.
    /// </summary>
    /// <param name="item">The selected item.</param>
    /// <returns>
    /// a list containing all item found
    /// </returns>
    public IList<IAbstractMediaItem> Get(IMediaItem item, IRootContainer root, IRootContainer parent)
    {
      if (_view.Type == "shares")
      {
        return GetShares(root, parent);
      }
      if (item == null)
        Trace.WriteLine("Get(root)");
      else
        Trace.WriteLine("Get " + item.Title);
      if (item != null)
      {
        foreach (IAbstractMediaItem his in _history)
        {
          if (his.FullPath == item.FullPath)
          {
            item = null;
            break;
          }
        }
      }
      //if item is null, we query the previous view
      if (item == null)
      {
        if (_currentSubView > 0)
        {
          _currentSubView--;
          _history.RemoveAt(_history.Count - 1);
        }
      }
      else if (parent != null && parent.Parent != null && item.FullPath == parent.Parent.FullPath)
      {
        if (_currentSubView > 0)
        {
          _currentSubView--;
          _history.RemoveAt(_history.Count - 1);
        }
      }
      else if (_currentSubView + 1 < _view.SubViews.Count)
      {
        //get the next subview...
        _currentSubView++;
        _history.Add(item);
      }

      bool lastSubView = (_currentSubView == _view.SubViews.Count - 1);
      IList<IAbstractMediaItem> items = null;
      if (_currentSubView > 0)
      {
        // make a new dynamic query which includes the query of the subview
        // and a filter for the selected path
        Query q = new Query();
        Expression e = new Expression(_view.SubViews[_currentSubView].Query, Operator.And, q);
        for (int i = 0; i < _history.Count; ++i)
        {
          string key = _view.SubViews[i].Query.Key;
          Query sq;
          // if the selected item did came from a multifield attribute
          object keyValue = "";
          if (_history[i].MetaData.ContainsKey(key))
            keyValue = _history[i].MetaData[key];
          if (_history[i].MetaData.ContainsKey("multifield"))
          {
            //the use a Operator.Like and insert the | as a separator to find multiple fields
            keyValue = String.Format("%|{0}|%", keyValue);
            sq = new Query(key, Operator.Like, keyValue);
          }
          else
          {
            //otherwise use a Operator.Same
            sq = new Query(key, Operator.Same, keyValue);
          }
          q.SubQueries.Add(new Expression(sq));
        }
        View v = new View("temp");
        v.IsLastSubView = lastSubView;
        v.Query = (new Query("", e));
        v.Query.Key = _view.SubViews[_currentSubView].Query.Key;
        v.Databases = _view.Databases;
        v.MappingTable = _view.SubViews[_currentSubView].MappingTable;
        items = GetView(v, root, parent);
      }
      else
      {
        _view.SubViews[_currentSubView].Databases = _view.Databases;
        _view.SubViews[_currentSubView].IsLastSubView = lastSubView;
        items = GetView(_view.SubViews[_currentSubView], root, parent);
      }
      return items;
    }

    IList<IAbstractMediaItem> GetShares(IRootContainer root, IRootContainer parent)
    {
      IList<IRootContainer> items = ServiceScope.Get<IMediaManager>().RootContainers;

      IList<IAbstractMediaItem> returnItems = new List<IAbstractMediaItem>();
      IMetaDataMappingCollection mapColl = ServiceScope.Get<IMetadataMappingProvider>().Get(_view.SubViews[0].MappingTable);
      foreach (IRootContainer container in items)
      {
        container.Mapping = mapColl;
        container.Parent = parent;
        returnItems.Add(container);
      }
      return returnItems;
    }

    IList<IAbstractMediaItem> GetView(IView view, IRootContainer root, IRootContainer parent)
    {
      IMetaDataMappingCollection mapColl = ServiceScope.Get<IMetadataMappingProvider>().Get(view.MappingTable);
      IMediaManager manager = ServiceScope.Get<IMediaManager>();
      foreach (IMediaProvider provider in manager.MediaProviders)
      {
        List<IAbstractMediaItem> items = provider.GetView(view, root, parent);
        if (items != null && items.Count > 0)
        {
          if (_currentSubView + 1 >= _view.SubViews.Count)
          {
            return items;
          }
          IList<IAbstractMediaItem> returnItems = new List<IAbstractMediaItem>();
          foreach (IAbstractMediaItem abstractItem in items)
          {
            ViewContainer container = new ViewContainer(this, root, parent, (IMediaItem)abstractItem);
            container.Mapping = mapColl;
            returnItems.Add(container);
          }
          return returnItems;
        }
      }
      return null;
    }
  }
}

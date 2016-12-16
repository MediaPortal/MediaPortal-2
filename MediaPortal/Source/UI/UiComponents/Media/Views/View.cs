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

using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.UiComponents.Media.Views
{
  /// <summary>
  /// A view is a named collection of media items and sub views. It stores the materialized elements which
  /// are defined by its <see cref="View.Specification"/>.
  /// </summary>
  public class View
  {
    #region Protected fields

    protected IList<MediaItem> _items = null; // Initialized = _items != null
    protected IList<View> _subViews = null; // Initialized = _subViews != null
    protected int? _absNumItems = null;

    protected ViewSpecification _viewSpecification;
    protected string _displayName;

    #endregion

    #region Ctor

    internal View(ViewSpecification viewSpecification)
    {
      _viewSpecification = viewSpecification;
      _displayName = viewSpecification.ViewDisplayName;
      _absNumItems = viewSpecification.AbsNumItems;
    }

    #endregion

    /// <summary>
    /// Returns the display name of this view.
    /// </summary>
    public string DisplayName
    {
      get { return _displayName; }
    }

    /// <summary>
    /// Returns the information whether this view can be built (i.e. if all of its providers are present).
    /// An invalid view might become valid later and vice-versa.
    /// </summary>
    public bool IsValid
    {
      get { return _viewSpecification.CanBeBuilt; }
    }

    /// <summary>
    /// Returns the information whether this view is empty, i.e. doesn't contain any contents.
    /// </summary>
    public bool IsEmpty
    {
      get { return MediaItems.Count == 0 && SubViews.Count == 0; }
    }

    /// <summary>
    /// Returns the specification which creates this view.
    /// </summary>
    public ViewSpecification Specification
    {
      get { return _viewSpecification; }
    }

    /// <summary>
    /// Returns the IDs of media item aspects which are present in all media items of this view.
    /// </summary>
    public ICollection<Guid> NecessaryMIATypeIds
    {
      get { return _viewSpecification.NecessaryMIATypeIds; }
    }

    /// <summary>
    /// Returns the IDs of media item aspects which may be present in media items of this view.
    /// </summary>
    public ICollection<Guid> OptionalMIATypeIds
    {
      get { return _viewSpecification.OptionalMIATypeIds; }
    }

    /// <summary>
    /// Returns all media item which contain the media item aspects specified by
    /// <see cref="ViewSpecification.NecessaryMIATypeIds"/>.
    /// </summary>
    public IList<MediaItem> MediaItems
    {
      get
      {
        if (!IsLoaded)
          RefreshItemsAndSubViews();
        return _items;
      }
    }

    /// <summary>
    /// Returns all sub views of this view. The returned list will be created on demand.
    /// </summary>
    public IList<View> SubViews
    {
      get
      {
        if (!IsLoaded)
          RefreshItemsAndSubViews();
        return _subViews;
      }
    }

    /// <summary>
    /// Returns the number of items and child items of this view, if present. If this value cannot easily be evaluated, returns <c>null</c>.
    /// </summary>
    public int? AbsNumItems
    {
      get { return _absNumItems; }
    }

    /// <summary>
    /// Returns the information if the (lazily initialized) items list and sub views collection already has been filled.
    /// </summary>
    protected bool IsLoaded
    {
      get { return _items != null && _subViews != null; }
    }

    /// <summary>
    /// Refreshes the item list and the list of sub views of this view, i.e. re-queries the item list and
    /// maybe creates the sub views dynamically.
    /// </summary>
    public void Refresh()
    {
      Invalidate();
      RefreshItemsAndSubViews();
    }

    public void RefreshItemsAndSubViews()
    {
      IList<ViewSpecification> subViewSpecifications;
      _viewSpecification.ReLoadItemsAndSubViewSpecifications(out _items, out subViewSpecifications);
      if (_items == null || subViewSpecifications == null)
        // Reload failed
        return;
      _subViews = new List<View>();
      foreach (ViewSpecification vs in subViewSpecifications)
      {
        View subView = new View(vs) {_displayName = vs.ViewDisplayName};
        _subViews.Add(subView);
      }
    }

    /// <summary>
    /// Invalidates this view, so it's items and sub views get re-loaded the next time they are requested.
    /// </summary>
    public void Invalidate()
    {
      _items = null;
      _subViews = null;
    }

    public IViewChangeNotificator CreateViewChangeNotificator()
    {
      return _viewSpecification.CreateChangeNotificator();
    }

    public override string ToString()
    {
      return _displayName;
    }
  }
}

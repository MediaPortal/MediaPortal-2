#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using System.Collections.Generic;
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.UI.Views
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

    protected ViewSpecification _viewSpecification;
    protected View _parentView;
    protected string _displayName;

    #endregion

    #region Ctor

    internal View(View parentView, ViewSpecification viewSpecification)
    {
      _parentView = parentView;
      _viewSpecification = viewSpecification;
      _displayName = viewSpecification.ViewDisplayName;
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
    /// Returns the parent view of this view.
    /// </summary>
    public View ParentView
    {
      get { return _parentView; }
    }

    /// <summary>
    /// Returns the media item aspects whose data is contained in this view.
    /// </summary>
    public ICollection<Guid> MediaItemAspectIds
    {
      get { return _viewSpecification.MediaItemAspectIds; }
    }

    /// <summary>
    /// Returns all media item which contain the media item aspects specified by
    /// <see cref="ViewSpecification.MediaItemAspectIds"/>.
    /// </summary>
    public IList<MediaItem> MediaItems
    {
      get
      {
        if (!IsItemsInitialized)
          RefreshItems();
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
        if (!IsSubViewsInitialized)
          RefreshSubViews();
        return _subViews;
      }
    }

    /// <summary>
    /// Returns the information if the (lazily initialized) sub views collection already has been initialized.
    /// </summary>
    protected bool IsSubViewsInitialized
    {
      get { return _subViews != null; }
    }

    /// <summary>
    /// Returns the information if the (lazily initialized) items collection already has been initialized.
    /// </summary>
    protected bool IsItemsInitialized
    {
      get { return _items != null; }
    }

    /// <summary>
    /// Refreshes the item list and the list of sub views of this view, i.e. re-queries the item list and
    /// maybe creates the sub views dynamically.
    /// </summary>
    public void Refresh()
    {
      if (!IsValid)
        return;
      RefreshItems();
      RefreshSubViews();
    }

    public void RefreshItems()
    {
      if (!IsValid)
        return;
      _items = new List<MediaItem>(_viewSpecification.ReLoadItems());
    }

    public void RefreshSubViews()
    {
      if (!IsValid)
        return;
      _subViews = new List<View>();
      foreach (ViewSpecification vs in _viewSpecification.ReLoadSubViewSpecifications())
      {
        View subView = new View(this, vs);
        subView._displayName = vs.ViewDisplayName;
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
  }
}

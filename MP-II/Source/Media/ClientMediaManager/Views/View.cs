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
using System.Collections.Generic;
using System.Xml.Serialization;
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Media.ClientMediaManager.Views
{
  /// <summary>
  /// Holds the building instructions for creating a collection of media items and
  /// sub views, and at the same time, it can store the materialized structure.
  /// </summary>
  /// <remarks>
  /// A view is a named collection of media items and sub views. It can be implemented in multiple ways.
  /// It can specify a list of media items by a database query, or by a hard disc location.
  /// The view contents may be ordered or not.<br/>
  /// For loading the view's media items and creating the sub views, this view needs
  /// the information about whole the sub structure beginning from its own point.
  /// When persisting views to a settings file, the view's hierarchy will be written from the
  /// root view on until the first view is reached which generates its contents dynamically,
  /// for example by a file system or database query.
  /// <para>
  /// Note: This class and its subclasses are serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public abstract class View
  {
    #region Protected fields

    protected IList<MediaItem> _items = null; // Transient data
    protected IList<View> _subViews = null; // Transient data

    protected View _parentView;

    #endregion

    #region Ctor

    internal View(View parentView, IEnumerable<Guid> mediaItemAspectIds)
    {
      _parentView = parentView;
    }

    #endregion

    /// <summary>
    /// Will be called after this view's data have been loaded from the persistent storage.
    /// </summary>
    /// <remarks>
    /// This method should be overridden by subclasses to do initialization which could not be
    /// done in the (empty) deserialization constructor. At least for static sub views, which have
    /// also been loaded, their method <see cref="Loaded()"/> should also be called by subclasses.
    /// </remarks>
    /// <param name="parentView">The parent view which contains this view.</param>
    internal virtual void Loaded(View parentView)
    {
      _parentView = parentView;
    }

    /// <summary>
    /// Returns the display name of this view.
    /// </summary>
    [XmlIgnore]
    public abstract string DisplayName { get; }

    /// <summary>
    /// Returns the parent view of this view.
    /// </summary>
    [XmlIgnore]
    public View ParentView
    {
      get { return _parentView; }
    }

    /// <summary>
    /// Returns the media item aspects whose data is contained in this view.
    /// </summary>
    [XmlIgnore]
    public abstract ICollection<Guid> MediaItemAspectIds { get; }

    /// <summary>
    /// Returns all media item aspects specified by this view.
    /// </summary>
    [XmlIgnore]
    public IList<MediaItem> MediaItems
    {
      get
      {
        if (_items == null)
          Refresh();
        return _items;
      }
    }

    /// <summary>
    /// Returns all sub views of this view. The returned list will be created on demand.
    /// </summary>
    [XmlIgnore]
    public IList<View> SubViews
    {
      get
      {
        if (_subViews == null)
          Refresh();
        return _subViews;
      }
    }

    /// <summary>
    /// Refreshes the item list and the list of sub views of this view, i.e. re-queries the item list and
    /// maybe creates the sub views dynamically.
    /// </summary>
    public void Refresh()
    {
      _items = ReLoadItems();
      _subViews = ReLoadSubViews();
    }

    /// <summary>
    /// Invalidates this view, so it's items and sub views get re-loaded the next time they are requested.
    /// </summary>
    public void Invalidate()
    {
      _items = null;
      _subViews = null;
    }

    /// <summary>
    /// Loads or reloads the items of this view. This will re-request the database or datastore for
    /// this view.
    /// </summary>
    /// <remarks>
    /// This method will load the media items of this view. It will load all of the specified
    /// media item aspects which are available for the media items.
    /// <i>Hint:</i>
    /// The uppercase L of the name is no spelling error; it denotes that this method is for
    /// Loading and Reloading of media items.
    /// </remarks>
    /// <returns>List of media items in this view.</returns>
    protected abstract IList<MediaItem> ReLoadItems();

    /// <summary>
    /// Loads or reloads the sub views of this view. This will re-request the database or datastore for
    /// this view.
    /// </summary>
    /// <remarks>
    /// <i>Hint:</i>
    /// The uppercase L of the name is no spelling error; it denotes that this method is for
    /// Loading and Reloading of sub views.
    /// </remarks>
    /// <returns>List of sub views of this view.</returns>
    protected abstract IList<View> ReLoadSubViews();

    #region Additional members for the XML serialization

    internal View() { }

    #endregion
  }
}

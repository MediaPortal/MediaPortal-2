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

using System.Collections.Generic;
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Media.ClientMediaManager.Views
{
  /// <summary>
  /// Stores the data of a (materialized) view. A view is a (named) collection of media items. The contents
  /// are specified by the metadata of the view.
  /// </summary>
  /// <remarks>
  /// A view is a collection of media items which can be implemented in multiple ways.
  /// It can contain a list of media items specified by a database query, or a list of
  /// media items from a hard disc location. A view may be ordered or not.
  /// </remarks>
  public abstract class View
  {
    #region Protected fields

    protected IList<MediaItem> _items = null;
    protected ViewMetadata _metadata;

    #endregion

    #region Ctor

    internal View(ViewMetadata metadata)
    {
      _metadata = metadata;
    }

    #endregion

    /// <summary>
    /// Returns all media items specified by this view.
    /// </summary>
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
    /// Returns the metadata descriptor of this view.
    /// </summary>
    public ViewMetadata Metadata
    {
      get { return _metadata; }
    }

    /// <summary>
    /// Refreshes the item list of this view, i.e. re-queries the item list.
    /// </summary>
    public void Refresh()
    {
      _items = ReLoadItems();
    }

    /// <summary>
    /// Invalidates this view, so it gets re-loaded the next time items are requested.
    /// </summary>
    public void Invalidate()
    {
      _items = null;
    }

    /// <summary>
    /// Loads or reloads the items of this view. This will re-request the database or datastore for
    /// this view.
    /// </summary>
    /// <remarks>
    /// This method will load the media items of this view. It will load all of the specified
    /// media item aspects which are available for the media items.
    /// </remarks>
    /// <returns>List of media items in this view.</returns>
    protected abstract IList<MediaItem> ReLoadItems();
  }
}

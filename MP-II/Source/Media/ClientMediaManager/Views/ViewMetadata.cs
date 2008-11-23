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
using MediaPortal.Core.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Media.ClientMediaManager.Views
{
  /// <summary>
  /// Holds all metadata of a view.
  /// This class will be subclassed for concrete view descriptions querying the media library or
  /// local shares.
  /// </summary>
  /// <remarks>
  /// A view is a client-only concept of specifying a collection of media items.
  /// </remarks>
  public abstract class ViewMetadata
  {
    #region Protected fields

    protected Guid _viewId;
    protected string _displayName;
    protected Guid? _parentViewId;
    protected ICollection<Guid> _mediaItemAspectIds;
    protected ICollection<Guid> _subViews;

    #endregion

    protected ViewMetadata(Guid viewId, string displayName, Guid? parentViewId,
        ICollection<Guid> mediaItemAspectIds)
    {
      if (!mediaItemAspectIds.Contains(ProviderResourceAspect.ASPECT_ID))
        mediaItemAspectIds.Add(ProviderResourceAspect.ASPECT_ID);
      _viewId = viewId;
      _displayName = displayName;
      _parentViewId = parentViewId;
      _mediaItemAspectIds = mediaItemAspectIds;
    }

    /// <summary>
    /// Returns the id of the view.
    /// </summary>
    public Guid ViewId
    {
      get { return _viewId; }
    }

    /// <summary>
    /// Returns the name shown for the view in the GUI.
    /// </summary>
    public string DisplayName
    {
      get { return _displayName; }
    }

    /// <summary>
    /// Returns the id of the parent view.
    /// </summary>
    public Guid? ParentViewId
    {
      get { return _parentViewId; }
      set { _parentViewId = value; }
    }

    /// <summary>
    /// Returns the media item aspects whose data is contained in this view.
    /// Changing the returned collection of media item aspects will change the associated media item aspects
    /// of this metadata instance.
    /// </summary>
    public ICollection<Guid> MediaItemAspectIds
    {
      get { return _mediaItemAspectIds; }
    }

    /// <summary>
    /// Returns the ids of all sub views of the view.
    /// Changing the returned collection of subviews will change the subviews for the view
    /// described by this instance.
    /// </summary>
    public ICollection<Guid> SubViewIds
    {
      get { return _subViews; }
    }
  }
}

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
  /// View which is based on a media library query.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class MediaLibraryView : View
  {
    #region Protected fields

    protected MediaLibraryQueryHierarchyNode _queryHierarchyNode;

    #endregion

    #region Ctor

    internal MediaLibraryView(MediaLibraryQueryHierarchyNode queryHierarchyNode,
        View parentView) :
      base(parentView, queryHierarchyNode.MediaItemAspectIds)
    {
      _queryHierarchyNode = queryHierarchyNode;
    }

    #endregion

    public override string DisplayName
    {
      get { return _queryHierarchyNode.DisplayName; }
    }

    [XmlIgnore]
    public override bool IsValid
    {
      get
      {
        // TODO (Albert 2009-01-10): Return if the media library is present
        return false;
      }
    }

    [XmlIgnore]
    public override ICollection<Guid> MediaItemAspectIds
    {
      get { return _queryHierarchyNode.MediaItemAspectIds; }
    }

    public override bool IsBasedOnShare(Guid shareId)
    {
        // TODO (Albert 2009-01-10): Maybe check the query if it is based on the specified view
      return false;
    }

    protected override IList<MediaItem> ReLoadItems()
    {
      // TODO (Albert, 2008-11-15): Load view contents from the media library, if connected
      return new List<MediaItem>();
    }

    protected override IList<View> ReLoadSubViews()
    {
      IList<View> result = new List<View>();
      foreach (MediaLibraryQueryHierarchyNode node in _queryHierarchyNode.SubQueryNodes)
        result.Add(new MediaLibraryView(node, this));
      return result;
    }

    #region Additional members for the XML serialization

    // Serialization of media library views works like this:
    // The top media library view serializes the query hierarchy. The sub views are
    // rebuilt dynamically.

    internal MediaLibraryView() { }

    /// <summary>
    /// Returns the media library query this view is based on.
    /// </summary>
    [XmlIgnore]
    public MediaLibraryQueryHierarchyNode QueryHierarchyNode
    {
      get { return _queryHierarchyNode; }
      set { _queryHierarchyNode = value; }
    }

    #endregion
  }
}

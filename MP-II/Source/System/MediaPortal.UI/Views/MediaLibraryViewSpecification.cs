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
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Utilities;

namespace MediaPortal.Views
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
  public class MediaLibraryViewSpecification : ViewSpecification
  {
    #region Protected fields

    protected MediaItemQuery _query;
    protected List<MediaLibraryViewSpecification> _subViews;

    #endregion

    #region Ctor

    public MediaLibraryViewSpecification(string viewDisplayName, MediaItemQuery query, IEnumerable<Guid> mediaItemAspectIds) :
        base(viewDisplayName, mediaItemAspectIds)
    {
      _query = query;
    }

    #endregion

    /// <summary>
    /// Returns a list of all sub query view specifications of this view specification.
    /// </summary>
    [XmlIgnore]
    public IList<MediaLibraryViewSpecification> SubViewSpecifications
    {
      get { return _subViews; }
    }

    [XmlIgnore]
    public override bool CanBeBuilt
    {
      get
      {
        // TODO (Albert 2009-01-10): Return true if the media library is present
        return false;
      }
    }

    internal override IEnumerable<MediaItem> ReLoadItems()
    {
      // TODO (Albert, 2008-11-15): Load view contents from the media library, if connected
      yield break;
    }

    internal override IEnumerable<ViewSpecification> ReLoadSubViewSpecifications()
    {
      IList<ViewSpecification> result = new List<ViewSpecification>(_subViews.Count);
      CollectionUtils.AddAll(result, _subViews);
      return result;
    }

    #region Additional members for the XML serialization

    // Serialization of media library views works like this:
    // The top media library view serializes the query hierarchy. The sub views are
    // rebuilt dynamically.

    internal MediaLibraryViewSpecification() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("Query", IsNullable = false)]
    public MediaItemQuery XML_Query
    {
      get { return _query; }
      set { _query = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("SubViews", IsNullable = false)]
    public List<MediaLibraryViewSpecification> XML_SubViews
    {
      get { return _subViews; }
      set { _subViews = value; }
    }

    #endregion
  }
}

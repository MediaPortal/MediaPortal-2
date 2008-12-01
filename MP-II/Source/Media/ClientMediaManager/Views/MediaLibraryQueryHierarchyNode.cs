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
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Utilities;

namespace MediaPortal.Media.ClientMediaManager.Views
{
  public class MediaLibraryQueryHierarchyNode
  {
    #region Protected fields

    protected string _displayName;
    protected IQuery _query;
    protected List<MediaLibraryQueryHierarchyNode> _subQueryNodes;
    protected HashSet<Guid> _mediaItemAspectIds = new HashSet<Guid>();

    #endregion

    #region Ctor

    public MediaLibraryQueryHierarchyNode(string displayName, IQuery query,
        IEnumerable<Guid> mediaItemAspectIds)
    {
      _displayName = displayName;
      _query = query;
      CollectionUtils.AddAll(_mediaItemAspectIds, mediaItemAspectIds);
      if (!_mediaItemAspectIds.Contains(ProviderResourceAspect.ASPECT_ID))
        _mediaItemAspectIds.Add(ProviderResourceAspect.ASPECT_ID);
    }

    #endregion

    /// <summary>
    /// Returns the display name for the view materialized with this query hierarchy node.
    /// </summary>
    [XmlIgnore]
    public string DisplayName
    {
      get { return _displayName; }
    }

    /// <summary>
    /// Returns a list of all sub query-nodes of this query node.
    /// </summary>
    [XmlIgnore]
    public IList<MediaLibraryQueryHierarchyNode> SubQueryNodes
    {
      get { return _subQueryNodes; }
    }

    /// <summary>
    /// Returns all media item aspects specified by this query.
    /// </summary>
    [XmlIgnore]
    public ICollection<Guid> MediaItemAspectIds
    {
      get { return _mediaItemAspectIds; }
    }

    #region Additional members for the XML serialization

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("DisplayName", IsNullable = false)]
    public string XML_DisplayName
    {
      get { return _displayName; }
      set { _displayName = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("QueryString", IsNullable = false)]
    public string XML_QueryString
    {
      // TODO: Implement
      get { return "Not implemented yet"; }
      set
      {
        // TODO: Rebuild query from query string
      }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("SubQueryNodes", IsNullable = false)]
    public List<MediaLibraryQueryHierarchyNode> XML_SubQueryNodes
    {
      get { return _subQueryNodes; }
      set { _subQueryNodes = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("MediaItemAspectIds")]
    public HashSet<Guid> XML_MediaItemAspectIds
    {
      get { return _mediaItemAspectIds; }
      set { _mediaItemAspectIds = value; }
    }

    #endregion
  }
}
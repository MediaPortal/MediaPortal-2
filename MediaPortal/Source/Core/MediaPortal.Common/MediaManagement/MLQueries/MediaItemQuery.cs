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
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using MediaPortal.Utilities;

namespace MediaPortal.Common.MediaManagement.MLQueries
{
  public enum SortDirection
  {
    Ascending,
    Descending
  }

  public class SortInformation
  {
    protected MediaItemAspectMetadata.AttributeSpecification _attributeType;
    protected SortDirection _sortDirection;

    public SortInformation(MediaItemAspectMetadata.AttributeSpecification attributeType, SortDirection sortDirection)
    {
      _attributeType = attributeType;
      _sortDirection = sortDirection;
    }

    [XmlIgnore]
    public MediaItemAspectMetadata.AttributeSpecification AttributeType
    {
      get { return _attributeType; }
      set { _attributeType = value; }
    }

    public SortDirection Direction
    {
      get { return _sortDirection; }
      set { _sortDirection = value; }
    }

    #region Additional members for the XML serialization

    internal SortInformation() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("AttributeType", IsNullable=false)]
    public string XML_AttributeType
    {
      get { return SerializationHelper.SerializeAttributeTypeReference(_attributeType); }
      set { _attributeType = SerializationHelper.DeserializeAttributeTypeReference(value); }
    }

    #endregion
  }

  /// <summary>
  /// Class to be used for XML serialization of <see cref="IFilter"/> values.
  /// </summary>
  public class FilterWrapper
  {
    protected IFilter _filter;

    public FilterWrapper(IFilter filter)
    {
      _filter = filter;
    }

    [XmlIgnore]
    public IFilter Filter
    {
      get { return _filter; }
      set { _filter = value; }
    }

    #region Additional members for the XML serialization

    internal FilterWrapper() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("BetweenFilter", typeof(BetweenFilter))]
    [XmlElement("BooleanCombinationFilter", typeof(BooleanCombinationFilter))]
    [XmlElement("InFilter", typeof(InFilter))]
    [XmlElement("LikeFilter", typeof(LikeFilter))]
    [XmlElement("NotFilter", typeof(NotFilter))]
    [XmlElement("RelationalFilter", typeof(RelationalFilter))]
    [XmlElement("Empty", typeof(EmptyFilter))]
    [XmlElement("RelationalUserDataFilter", typeof(RelationalUserDataFilter))]
    [XmlElement("EmptyUserData", typeof(EmptyUserDataFilter))]
    [XmlElement("False", typeof(FalseFilter))]
    [XmlElement("MediaItemIds", typeof(MediaItemIdFilter))]
    [XmlElement("Relationship", typeof(RelationshipFilter))]
    [XmlElement("FilterRelationship", typeof(FilteredRelationshipFilter))]
    public object XML_Filter
    {
      get { return _filter; }
      set { _filter = value as IFilter; }
    }

    #endregion
  }

  /// <summary>
  /// Encapsulates a query for media items. Holds selected media item aspect types and a filter criterion.
  /// </summary>
  public class MediaItemQuery
  {
    #region Protected fields

    protected IFilter _filter;
    protected HashSet<Guid> _necessaryRequestedMIATypeIDs;
    protected HashSet<Guid> _optionalRequestedMIATypeIDs = null;
    protected List<SortInformation> _sortInformation = null;
    protected uint? _offset = null;
    protected uint? _limit = null;

    // We could use some cache for this instance, if we would have one...
    protected static XmlSerializer _xmlSerializer = null; // Lazy initialized
    protected static XmlSerializer _xmlFilterSerializer = null; // Lazy initialized

    #endregion

    #region Ctor

    public MediaItemQuery(IEnumerable<Guid> necessaryRequestedMIATypeIDs, IFilter filter)
    {
      SetNecessaryRequestedMIATypeIDs(necessaryRequestedMIATypeIDs);
      SetOptionalRequestedMIATypeIDs(null);
      _filter = filter;
    }

    public MediaItemQuery(IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs,
        IFilter filter)
    {
      SetNecessaryRequestedMIATypeIDs(necessaryRequestedMIATypeIDs);
      SetOptionalRequestedMIATypeIDs(optionalRequestedMIATypeIDs);
      _filter = filter;
    }

    public MediaItemQuery(MediaItemQuery other)
    {
      _filter = other.Filter;
      _necessaryRequestedMIATypeIDs = new HashSet<Guid>(other._necessaryRequestedMIATypeIDs);
      _optionalRequestedMIATypeIDs = new HashSet<Guid>(other._optionalRequestedMIATypeIDs);
      _sortInformation = other._sortInformation;
      _limit = other.Limit;
      _offset = other.Offset;
    }

    #endregion

    #region Public properties

    [XmlIgnore]
    public ICollection<Guid> NecessaryRequestedMIATypeIDs
    {
      get { return _necessaryRequestedMIATypeIDs; }
      set { SetNecessaryRequestedMIATypeIDs(value); }
    }

    [XmlIgnore]
    public ICollection<Guid> OptionalRequestedMIATypeIDs
    {
      get { return _optionalRequestedMIATypeIDs; }
      set { SetOptionalRequestedMIATypeIDs(value); }
    }

    [XmlIgnore]
    public IFilter Filter
    {
      get { return _filter; }
      set { _filter = value; }
    }

    [XmlIgnore]
    public IList<SortInformation> SortInformation
    {
      get { return _sortInformation; }
      set { _sortInformation = value != null ? new List<SortInformation>(value) : null; }
    }

    /// <summary>
    /// Optional offset to return items from a specific starting position from query.
    /// </summary>
    public uint? Offset
    {
      get { return _offset; }
      set { _offset = value; }
    }

    /// <summary>
    /// Optional limit to return only a specific number of items from query.
    /// </summary>
    public uint? Limit
    {
      get { return _limit; }
      set { _limit = value; }
    }

    #endregion

    public void SetNecessaryRequestedMIATypeIDs(IEnumerable<Guid> value)
    {
      _necessaryRequestedMIATypeIDs = value == null ? new HashSet<Guid>() : new HashSet<Guid>(value);
    }

    public void SetOptionalRequestedMIATypeIDs(IEnumerable<Guid> value)
    {
      _optionalRequestedMIATypeIDs = value == null ? new HashSet<Guid>() : new HashSet<Guid>(value);
    }

    public static void SerializeFilter(XmlWriter writer, IFilter filter)
    {
      FilterWrapper wrapper = new FilterWrapper(filter);
      XmlSerializer xs = GetOrCreateXMLFilterSerializer();
      xs.Serialize(writer, wrapper);
    }

    public static IFilter DeserializeFilter(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLFilterSerializer();
      FilterWrapper wrapper = xs.Deserialize(reader) as FilterWrapper;
      return wrapper == null ? null : wrapper.Filter;
    }

    public void Serialize(XmlWriter writer)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      xs.Serialize(writer, this);
    }

    public static MediaItemQuery Deserialize(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      return xs.Deserialize(reader) as MediaItemQuery;
    }

    #region Base overrides

    public override string ToString()
    {
      StringBuilder result = new StringBuilder();
      result.Append("MediaItemQuery: NecessaryRequestedMIATypes: [");
      result.Append(StringUtils.Join(", ", _necessaryRequestedMIATypeIDs));
      result.Append("], OptionalRequestedMIATypes: [");
      result.Append(StringUtils.Join(", ", _optionalRequestedMIATypeIDs));
      result.Append("], Filter: [");
      result.Append(_filter);
      result.Append("], SortInformation: [");
      result.Append(StringUtils.Join(", ", _sortInformation));
      result.Append("]");
      if (Limit.HasValue)
        result.AppendFormat(" LIMIT {0}", Limit.Value);
      if (Offset.HasValue)
        result.AppendFormat(" OFFSET {0}", Offset.Value);
      return result.ToString();
    }

    #endregion

    #region Additional members for the XML serialization

    internal MediaItemQuery() { }

    protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      return _xmlSerializer ??
          (_xmlSerializer = new XmlSerializer(typeof(MediaItemQuery), new Type[] {typeof(FilterWrapper)}));
    }

    protected static XmlSerializer GetOrCreateXMLFilterSerializer()
    {
      if (_xmlFilterSerializer == null)
        _xmlFilterSerializer = new XmlSerializer(typeof(FilterWrapper));
      return _xmlFilterSerializer;
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("NecessaryMIATypes")]
    [XmlArrayItem("TypeId")]
    public HashSet<Guid> XML_NecessaryRequestedMIATypeIDs
    {
      get { return _necessaryRequestedMIATypeIDs; }
      set { _necessaryRequestedMIATypeIDs = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("OptionalMIATypes")]
    [XmlArrayItem("TypeId")]
    public HashSet<Guid> XML_OptionalRequestedMIATypeIDs
    {
      get { return _optionalRequestedMIATypeIDs; }
      set { _optionalRequestedMIATypeIDs = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("BetweenFilter", typeof(BetweenFilter))]
    [XmlElement("BooleanCombinationFilter", typeof(BooleanCombinationFilter))]
    [XmlElement("InFilter", typeof(InFilter))]
    [XmlElement("LikeFilter", typeof(LikeFilter))]
    [XmlElement("NotFilter", typeof(NotFilter))]
    [XmlElement("RelationalFilter", typeof(RelationalFilter))]
    [XmlElement("Empty", typeof(EmptyFilter))]
    [XmlElement("RelationalUserDataFilter", typeof(RelationalUserDataFilter))]
    [XmlElement("EmptyUserData", typeof(EmptyUserDataFilter))]
    [XmlElement("False", typeof(FalseFilter))]
    [XmlElement("MediaItemIds", typeof(MediaItemIdFilter))]
    [XmlElement("Relationship", typeof(RelationshipFilter))]
    [XmlElement("FilterRelationship", typeof(FilteredRelationshipFilter))]
    // Necessary to have an object here, else the serialization algorithm cannot cope with polymorph values
    public object XML_Filter
    {
      get { return _filter; }
      set { _filter = value as IFilter; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("Sorting")]
    [XmlArrayItem("SortInformation")]
    public List<SortInformation> XML_SortInformation
    {
      get { return _sortInformation; }
      set { _sortInformation = value; }
    }

    #endregion
  }
}

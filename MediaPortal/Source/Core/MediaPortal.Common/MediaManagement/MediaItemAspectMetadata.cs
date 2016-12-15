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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MediaPortal.Common.MediaManagement
{
  public enum Cardinality
  {
    /// <summary>
    /// The attribute is defined at the current media item aspect instance.
    /// </summary>
    /// <remarks>
    /// The attribute will be defined inline in the media item aspect's table.
    /// </remarks>
    Inline,

    /// <summary>
    /// There are multiple values for the attribute, which are all dedicated to the
    /// current media item.
    /// </summary>
    /// <remarks>
    /// The attribute is stored in its own table and the association attribute between the main MIA table
    /// and the attribute's table is defined in the attribute's table.
    /// </remarks>
    OneToMany,

    /// <summary>
    /// There is zero or one associated attribute value per media item, which itself might be assigned to multiple
    /// media item's aspects.
    /// </summary>
    /// <remarks>
    /// The attribute is stored in its own table and the association attribute between the main MIA table
    /// and the attribute's table is defined in the main MIA table.
    /// </remarks>
    ManyToOne,

    /// <summary>
    /// There are multiple values for the attribute per media item, which themselves might be assigned to multiple
    /// media items.
    /// </summary>
    /// <remarks>
    /// The attribute is stored in its own table and the association between the main MIA table
    /// and the attribute's table is defined in its own N:M table.
    /// </remarks>
    ManyToMany
  }

  /// <summary>
  /// Metadata descriptor for a <see cref="MediaItemAspect"/>.
  /// Once a <see cref="MediaItemAspectMetadata"/> is released with a given <see cref="AspectId"/>, it must not
  /// be changed any more. Updating a <see cref="MediaItemAspectMetadata"/> must lead to a change of the
  /// <see cref="AspectId"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  [XmlInclude(typeof(SingleMediaItemAspectMetadata))]
  [XmlInclude(typeof(MultipleMediaItemAspectMetadata))]
  public abstract class MediaItemAspectMetadata
  {
    #region Constants

    /// <summary>
    /// Contains a collection of supported basic types. Together with those basic types, the <see cref="String"/> type
    /// is also supported.
    /// </summary>
    /// <remarks>
    /// The following types are supported:
    /// <list>
    /// <item><see cref="DateTime"/></item>
    /// <item><see cref="Char"/></item>
    /// <item><see cref="Boolean"/></item>
    /// <item><see cref="Single"/></item>
    /// <item><see cref="Double"/></item>
    /// <item><see cref="Int32"/></item>
    /// <item><see cref="Int64"/></item>
    /// <item><see cref="Guid"/></item>
    /// <item><c>byte[]</c></item>
    /// </list>
    /// </remarks>
    public static readonly ICollection<Type> SUPPORTED_BASIC_TYPES = new List<Type>
        {
          typeof(DateTime),
          typeof(Char),
          typeof(Boolean),
          typeof(Single),
          typeof(Double),
          typeof(Int32),
          typeof(Int64),
          typeof(Guid),
          typeof(byte[]),
        };

    #endregion

    /// <summary>
    /// Stores the metadata for one attribute in a media item aspect.
    /// </summary>
    [XmlInclude(typeof(SingleAttributeSpecification))]
    [XmlInclude(typeof(MultipleAttributeSpecification))]
    public abstract class AttributeSpecification
    {
      #region Protected fields

      protected MediaItemAspectMetadata _parentMIAM = null;
      protected string _attributeName;
      protected Type _attributeType;
      protected Cardinality _cardinality;
      protected uint _maxNumChars;
      protected bool _indexed;

      #endregion

      internal AttributeSpecification(string name, Type type, Cardinality cardinality, bool indexed)
      {
        _attributeName = name;
        _attributeType = type;
        _cardinality = cardinality;
        _indexed = indexed;
      }

      internal IList CreateValuesCollection()
      {
        Type collectionType = typeof(List<>).MakeGenericType(_attributeType);
        return (IList) Activator.CreateInstance(collectionType);
      }

      /// <summary>
      /// Gets or sets the media item aspect metadata instance, this attribute type belongs to.
      /// </summary>
      [XmlIgnore]
      public MediaItemAspectMetadata ParentMIAM
      {
        get { return _parentMIAM; }
        internal set { _parentMIAM = value; }
      }

      /// <summary>
      /// Name of the defined attribute. The name is unique among all attributes of the same
      /// media item aspect.
      /// </summary>
      [XmlIgnore]
      public string AttributeName
      {
        get { return _attributeName; }
      }

      /// <summary>
      /// Runtime value type for the attribute instances.
      /// </summary>
      [XmlIgnore]
      public Type AttributeType
      {
        get { return _attributeType; }
      }

      /// <summary>
      /// Gets the cardinality of this attribute. See the docs for the
      /// <see cref="Cardinality"/> class and its members.
      /// </summary>
      [XmlIgnore]
      public Cardinality Cardinality
      {
        get { return _cardinality; }
      }

      /// <summary>
      /// Gets the information if this attribute is indexed in the media library.
      /// </summary>
      [XmlIgnore]
      public bool IsIndexed
      {
        get { return _indexed; }
      }

      /// <summary>
      /// Returns the information if this attribute is a collection attribute, i.e. supports multi-value
      /// entries.
      /// </summary>
      [XmlIgnore]
      public bool IsCollectionAttribute
      {
        get { return _cardinality == Cardinality.ManyToMany || _cardinality == Cardinality.OneToMany; }
      }

      /// <summary>
      /// If this is a string attribute type, this property gets or sets the maximum number of characters which can be
      /// stored in an attribute instance of this type.
      /// </summary>
      [XmlIgnore]
      public uint MaxNumChars
      {
        get { return _maxNumChars; }
        set { _maxNumChars = value; }
      }

      public override bool Equals(object obj)
      {
        AttributeSpecification other = obj as AttributeSpecification;
        if (other == null)
          return false;
        return _parentMIAM.AspectId == other._parentMIAM.AspectId && _attributeName == other._attributeName;
      }

      public override int GetHashCode()
      {
        return _parentMIAM.AspectId.GetHashCode() + _attributeName.GetHashCode();
      }

      public override string ToString()
      {
        return "AttributeSpecification '" + _attributeName + "'";
      }

      #region Additional members for the XML serialization

      internal AttributeSpecification() { }

      /// <summary>
      /// For internal use of the XML serialization system only.
      /// </summary>
      [XmlElement("AttributeName")]
      public string XML_AttributeName
      {
        get { return _attributeName; }
        set { _attributeName = value; }
      }

      /// <summary>
      /// For internal use of the XML serialization system only.
      /// </summary>
      [XmlElement("AttributeType")]
      public string XML_AttributeType
      {
        get { return _attributeType.FullName; }
        set { _attributeType = Type.GetType(value); }
      }

      /// <summary>
      /// For internal use of the XML serialization system only.
      /// </summary>
      [XmlElement("Cardinality")]
      public Cardinality XML_Cardinality
      {
        get { return _cardinality; }
        set { _cardinality = value; }
      }

      /// <summary>
      /// For internal use of the XML serialization system only.
      /// </summary>
      [XmlElement("Indexed")]
      public bool XML_Indexed
      {
        get { return _indexed; }
        set { _indexed = value; }
      }

      /// <summary>
      /// For internal use of the XML serialization system only.
      /// </summary>
      [XmlElement("MaxNumChars")]
      public uint XML_MaxNumChars
      {
        get { return _maxNumChars; }
        set { _maxNumChars = value; }
      }

      #endregion
    }

    public class SingleAttributeSpecification : AttributeSpecification
    {
      public SingleAttributeSpecification(string name, Type type, Cardinality cardinality, bool indexed) : base(name, type, cardinality, indexed)
      {
      }

      /// <summary>
      /// Gets or sets the media item aspect metadata instance, this attribute type belongs to.
      /// </summary>
      [XmlIgnore]
      new public SingleMediaItemAspectMetadata ParentMIAM
      {
        get { return (SingleMediaItemAspectMetadata)_parentMIAM; }
        internal set { _parentMIAM = value; }
      }

      #region Additional members for the XML serialization
      internal SingleAttributeSpecification() { }
      #endregion
    }

    public class MultipleAttributeSpecification : AttributeSpecification
    {
      public MultipleAttributeSpecification(string name, Type type, Cardinality cardinality, bool indexed) : base(name, type, cardinality, indexed)
      {
      }

      /// <summary>
      /// Gets or sets the media item aspect metadata instance, this attribute type belongs to.
      /// </summary>
      [XmlIgnore]
      new public MultipleMediaItemAspectMetadata ParentMIAM
      {
        get { return (MultipleMediaItemAspectMetadata)_parentMIAM; }
        internal set { _parentMIAM = value; }
      }

      #region Additional members for the XML serialization
      internal MultipleAttributeSpecification() { }
      #endregion
    }

    #region Protected fields

    protected string _aspectName;
    protected Guid _aspectId;
    protected IList<String> _refAttributeNames = new List<String>();
    protected Guid _refAspectId = Guid.Empty;
    protected bool _isSystemAspect;
    protected bool _isTransientAspect;
    protected IDictionary<string, AttributeSpecification> _attributeSpecifications =
        new Dictionary<string, AttributeSpecification>();

    // We could use some cache for this instance, if we would have one...
    protected static XmlSerializer _xmlSerializer = null; // Lazy initialized

    #endregion

    /// <summary>
    /// Creates a new aspect metadata instance. An aspect with a given <paramref name="aspectId"/>
    /// SHOULD NOT be initialized from multiple code parts by other modules than the MediaPortal Core,
    /// i.e. plugins creating a specified media item aspect should create and exposed it to the entire system
    /// from a dedicated constant class.
    /// </summary>
    /// <param name="aspectId">Unique id of the new aspect type to create.</param>
    /// <param name="aspectName">Name of the new aspect type. The name should be unique and may be
    /// a localized string.</param>
    /// <param name="attributeSpecifications">Enumeration of specifications for the attribute of the new
    /// aspect type</param>
    protected MediaItemAspectMetadata(Guid aspectId, string aspectName,
        IEnumerable<AttributeSpecification> attributeSpecifications, bool isTransient)
    {
      _aspectId = aspectId;
      _aspectName = aspectName;
      _isTransientAspect = isTransient;
      foreach (AttributeSpecification spec in attributeSpecifications)
        _attributeSpecifications[spec.AttributeName] = spec;
      CorrectParentsInAttributeTypes();
    }

    protected void CorrectParentsInAttributeTypes()
    {
      foreach (AttributeSpecification attributeType in _attributeSpecifications.Values)
        attributeType.ParentMIAM = this;
    }

    /// <summary>
    /// Returns the globally unique ID of this aspect.
    /// </summary>
    [XmlIgnore]
    public Guid AspectId
    {
      get { return _aspectId; }
    }

    /// <summary>
    /// Name of this aspect. Can be shown in the gui, for example. The returned string may be
    /// a localized string label (i.e. "[section.name]").
    /// </summary>
    [XmlIgnore]
    public string Name
    {
      get { return _aspectName; }
    }

    /// <summary>
    /// Gets or sets the information if this aspect is a system aspect. System aspects must not be deleted.
    /// This property can only be set internal.
    /// </summary>
    [XmlIgnore]
    public bool IsSystemAspect
    {
      get { return _isSystemAspect; }
      internal set { _isSystemAspect = value; }
    }

    /// <summary>
    /// Gets the information if this aspect is a transient aspect. Transient aspects are not persisted to 
    /// the database and are only used for passing data in a consistent manner.
    /// </summary>
    [XmlIgnore]
    public bool IsTransientAspect
    {
      get { return _isTransientAspect; }
    }

    /// <summary>
    /// Returns a read-only mapping of names to attribute specifications for all
    /// available attributes of this media item aspect.
    /// </summary>
    [XmlIgnore]
    public IDictionary<string, AttributeSpecification> AttributeSpecifications
    {
      get { return _attributeSpecifications; }
    }

    /// <summary>
    /// Returns a read-only mapping of names to attribute specifications for all
    /// referenced attributes of this media item aspect. Must be in the same order as 
    /// the unique attributes in the referencing aspect (if any).
    /// </summary>
    [XmlIgnore]
    public IDictionary<string, AttributeSpecification> ReferencedAttributeSpecifications
    {
      get
      {
        return _attributeSpecifications.Where(x => _refAttributeNames.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
      }
    }

    /// <summary>
    /// Returns the globally unique ID of the referencing aspect (if any).
    /// </summary>
    [XmlIgnore]
    public Guid? ReferencingAspectId
    {
      get { return _refAspectId == Guid.Empty ? (Guid?)null : _refAspectId; }
    }

    /// <summary>
    /// Creates a specification for a new string attribute which can be used in a new
    /// <see cref="MediaItemAspectMetadata"/> instance.
    /// </summary>
    /// <param name="attributeName">Name of the string attribute to be created.
    /// TODO: Describe constraints on the name value. See also other CreateXXXAttributeSpecification methods.</param>
    /// <param name="maxNumChars">Maximum number of characters to be stored in the attribute to
    /// be created.</param>
    /// <param name="cardinality">Cardinality of the new attribute.</param>
    /// <param name="indexed">Set to <c>true</c> if the new attribute should be indexed in the media library.
    /// This should only be done if the attribute will often occur in filter criteria.</param>
    public static SingleAttributeSpecification CreateSingleStringAttributeSpecification(string attributeName,
        uint maxNumChars, Cardinality cardinality, bool indexed)
    {
      SingleAttributeSpecification result = new SingleAttributeSpecification(attributeName, typeof(string), cardinality, indexed)
        {MaxNumChars = maxNumChars};
      return result;
    }

    /// <summary>
    /// Creates a specification for a new string attribute which can be used in a new
    /// <see cref="MediaItemAspectMetadata"/> instance.
    /// </summary>
    /// <param name="attributeName">Name of the string attribute to be created.
    /// TODO: Describe constraints on the name value. See also other CreateXXXAttributeSpecification methods.</param>
    /// <param name="maxNumChars">Maximum number of characters to be stored in the attribute to
    /// be created.</param>
    /// <param name="cardinality">Cardinality of the new attribute.</param>
    /// <param name="indexed">Set to <c>true</c> if the new attribute should be indexed in the media library.
    /// This should only be done if the attribute will often occur in filter criteria.</param>
    public static MultipleAttributeSpecification CreateMultipleStringAttributeSpecification(string attributeName,
        uint maxNumChars, Cardinality cardinality, bool indexed)
    {
      MultipleAttributeSpecification result = new MultipleAttributeSpecification(attributeName, typeof(string), cardinality, indexed) { MaxNumChars = maxNumChars };
      return result;
    }

    /// <summary>
    /// Creates a specification for a new attribute which can be used in a new
    /// <see cref="MediaItemAspectMetadata"/> instance.
    /// </summary>
    /// <param name="attributeName">Name of the string attribute to be created.
    /// TODO: Describe constraints on the name value. See also other CreateXXXAttributeSpecification methods.</param>
    /// <param name="attributeType">Type of the attribute to be stored in the attribute to be created.
    /// For a list of supported attribute types, see <see cref="SUPPORTED_BASIC_TYPES"/>.</param>
    /// <param name="cardinality">Cardinality of the new attribute.</param>
    /// <param name="indexed">Set to <c>true</c> if the new attribute should be indexed in the media library.
    /// This should only be done if the attribute will often occur in filter criteria.</param>
    public static SingleAttributeSpecification CreateSingleAttributeSpecification(string attributeName,
        Type attributeType, Cardinality cardinality, bool indexed)
    {
      if (!SUPPORTED_BASIC_TYPES.Contains(attributeType))
        throw new ArgumentException(string.Format("Attribute type {0} is not supported for media item aspect attributes",
            attributeType.Name));
      return new SingleAttributeSpecification(attributeName, attributeType, cardinality, indexed);
    }

    /// <summary>
    /// Creates a specification for a new attribute which can be used in a new
    /// <see cref="MediaItemAspectMetadata"/> instance.
    /// </summary>
    /// <param name="attributeName">Name of the string attribute to be created.
    /// TODO: Describe constraints on the name value. See also other CreateXXXAttributeSpecification methods.</param>
    /// <param name="attributeType">Type of the attribute to be stored in the attribute to be created.
    /// For a list of supported attribute types, see <see cref="SUPPORTED_BASIC_TYPES"/>.</param>
    /// <param name="cardinality">Cardinality of the new attribute.</param>
    /// <param name="indexed">Set to <c>true</c> if the new attribute should be indexed in the media library.
    /// This should only be done if the attribute will often occur in filter criteria.</param>
    public static MultipleAttributeSpecification CreateMultipleAttributeSpecification(string attributeName,
        Type attributeType, Cardinality cardinality, bool indexed)
    {
      if (!SUPPORTED_BASIC_TYPES.Contains(attributeType))
        throw new ArgumentException(string.Format("Attribute type {0} is not supported for media item aspect attributes",
            attributeType.Name));
      return new MultipleAttributeSpecification(attributeName, attributeType, cardinality, indexed);
    }

    /// <summary>
    /// Serializes this MediaItem aspect metadata instance to XML.
    /// </summary>
    /// <returns>String containing an XML fragment with this instance's data.</returns>
    public string Serialize()
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      StringBuilder sb = new StringBuilder(); // Will contain the data, formatted as XML
      using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings {OmitXmlDeclaration = true}))
        xs.Serialize(writer, this);
      return sb.ToString();
    }

    /// <summary>
    /// Serializes this MediaItem aspect metadata instance to the given <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">Writer to write the XML serialization to.</param>
    public void Serialize(XmlWriter writer)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      xs.Serialize(writer, this);
    }

    /// <summary>
    /// Deserializes a MediaItem aspect metadata instance from a given XML fragment.
    /// </summary>
    /// <param name="str">XML fragment containing a serialized MediaItem aspect metadata instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static MediaItemAspectMetadata Deserialize(string str)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      using (StringReader reader = new StringReader(str))
        return xs.Deserialize(reader) as MediaItemAspectMetadata;
    }

    /// <summary>
    /// Deserializes a MediaItem aspect metadata instance from a given <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">XML reader containing a MediaItem aspect metadata XML serialization.</param>
    /// <returns>Deserialized instance.</returns>
    public static MediaItemAspectMetadata Deserialize(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      return xs.Deserialize(reader) as MediaItemAspectMetadata;
    }

    public override bool Equals(object obj)
    {
      MediaItemAspectMetadata other = obj as MediaItemAspectMetadata;
      if (other == null)
        return false;
      return other.AspectId == _aspectId;
    }

    public override int GetHashCode()
    {
      return _aspectId.GetHashCode();
    }

    public override string ToString()
    {
      return "MIA Type '" + _aspectName + "' (Id='" + _aspectId + "')";
    }

    #region Additional members for the XML serialization

    internal MediaItemAspectMetadata() { }

    protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      return _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(MediaItemAspectMetadata)));
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("Id")]
    public string XML_AspectId
    {
      get { return _aspectId.ToString(); }
      set { _aspectId = new Guid(value); }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("IsSystemAspect")]
    public bool XML_IsSystemAspect
    {
      get { return _isSystemAspect; }
      set { _isSystemAspect = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("IsTransientAspect")]
    public bool XML_IsTransientAspect
    {
      get { return _isTransientAspect; }
      set { _isTransientAspect = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("Name")]
    public string XML_Name
    {
      get { return _aspectName; }
      set { _aspectName = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("Attributes")]
    [XmlArrayItem("AttributeType")]
    // We use an array here because when using a List, stupid XML serializer only uses the property getter and fills the
    // List with its deserialized entries. When using an array, the property setter is invoked.
    public AttributeSpecification[] XML_AttributeSpecifications
    {
      get
      {
        AttributeSpecification[] result = new AttributeSpecification[_attributeSpecifications.Count];
        _attributeSpecifications.Values.CopyTo(result, 0);
        return result;
      }
      set
      {
        _attributeSpecifications = new Dictionary<string, AttributeSpecification>(value.Length);
        foreach (AttributeSpecification spec in value)
          _attributeSpecifications.Add(spec.AttributeName, spec);
        CorrectParentsInAttributeTypes();
      }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("RefId")]
    public string XML_RefAspectId
    {
      get { return _refAspectId.ToString(); }
      set { _refAspectId = new Guid(value); }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("RefAttributes")]
    [XmlArrayItem("RefAttribute")]
    // We use an array here because when using a List, stupid XML serializer only uses the property getter and fills the
    // List with its deserialized entries. When using an array, the property setter is invoked.
    public String[] XML_RefAttributes
    {
      get
      {
        return _refAttributeNames.ToArray();
      }
      set
      {
        _refAttributeNames.Clear();
        foreach (String refAttributeName in value)
        {
          _refAttributeNames.Add(refAttributeName);
        }
      }
    }

    #endregion
  }

  public class SingleMediaItemAspectMetadata : MediaItemAspectMetadata
  {
    public SingleMediaItemAspectMetadata(Guid aspectId, string aspectName,
        IEnumerable<SingleAttributeSpecification> attributeSpecifications) 
      : this(aspectId, aspectName, attributeSpecifications, false)
    {
    }

    public SingleMediaItemAspectMetadata(Guid aspectId, string aspectName,
        IEnumerable<SingleAttributeSpecification> attributeSpecifications, bool isTransient) 
      : this(aspectId, aspectName, attributeSpecifications, Guid.Empty, null, isTransient)
    {
    }

    public SingleMediaItemAspectMetadata(Guid aspectId, string aspectName,
        IEnumerable<SingleAttributeSpecification> attributeSpecifications,
        Guid referencedAspectId,
        IEnumerable<MultipleAttributeSpecification> referencedAttributeSpecifications) 
      : this(aspectId, aspectName, attributeSpecifications, referencedAspectId, referencedAttributeSpecifications, false)
    {
    }

    public SingleMediaItemAspectMetadata(Guid aspectId, string aspectName,
        IEnumerable<SingleAttributeSpecification> attributeSpecifications,
        Guid referencedAspectId,
        IEnumerable<MultipleAttributeSpecification> referencedAttributeSpecifications, bool isTransient) 
      : base(aspectId, aspectName, attributeSpecifications, isTransient)
    {
      _refAspectId = referencedAspectId;
      if (referencedAttributeSpecifications != null)
      {
        foreach (MultipleAttributeSpecification refAttributeSpecification in referencedAttributeSpecifications)
        {
          _refAttributeNames.Add(refAttributeSpecification.AttributeName);
        }
      }
    }

    #region Additional members for the XML serialization

    internal SingleMediaItemAspectMetadata() { }

    new protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      return _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(SingleMediaItemAspectMetadata)));
    }
    #endregion
  }

  public class MultipleMediaItemAspectMetadata : MediaItemAspectMetadata
  {
    protected IList<String> _uniqueAttributeNames = new List<String>();

    public MultipleMediaItemAspectMetadata(Guid aspectId, string aspectName,
      IEnumerable<MultipleAttributeSpecification> attributeSpecifications)
      : this(aspectId, aspectName, attributeSpecifications, null, false)
    {
    }

    public MultipleMediaItemAspectMetadata(Guid aspectId, string aspectName,
      IEnumerable<MultipleAttributeSpecification> attributeSpecifications, bool isTransient)
      : this(aspectId, aspectName, attributeSpecifications, null, isTransient)
    {
    }

    public MultipleMediaItemAspectMetadata(Guid aspectId, string aspectName,
        IEnumerable<MultipleAttributeSpecification> attributeSpecifications,
        IEnumerable<MultipleAttributeSpecification> uniqueAttributeSpecifications)
      : this(aspectId, aspectName, attributeSpecifications, uniqueAttributeSpecifications, false)
    {
    }

    public MultipleMediaItemAspectMetadata(Guid aspectId, string aspectName,
        IEnumerable<MultipleAttributeSpecification> attributeSpecifications,
        IEnumerable<MultipleAttributeSpecification> uniqueAttributeSpecifications, bool isTransient)
      : this(aspectId, aspectName, attributeSpecifications, uniqueAttributeSpecifications, Guid.Empty, null, isTransient)
    {
    }

    public MultipleMediaItemAspectMetadata(Guid aspectId, string aspectName,
    IEnumerable<MultipleAttributeSpecification> attributeSpecifications,
    IEnumerable<MultipleAttributeSpecification> uniqueAttributeSpecifications,
    Guid referencedAspectId,
    IEnumerable<MultipleAttributeSpecification> referencedAttributeSpecifications)
      : this(aspectId, aspectName, attributeSpecifications, uniqueAttributeSpecifications, referencedAspectId, referencedAttributeSpecifications, false)
    {
    }

    public MultipleMediaItemAspectMetadata(Guid aspectId, string aspectName,
        IEnumerable<MultipleAttributeSpecification> attributeSpecifications,
        IEnumerable<MultipleAttributeSpecification> uniqueAttributeSpecifications,
        Guid referencedAspectId,
        IEnumerable<MultipleAttributeSpecification> referencedAttributeSpecifications, bool isTransient)
      : base(aspectId, aspectName, attributeSpecifications, isTransient)
    {
      if (uniqueAttributeSpecifications != null)
      {
        foreach (MultipleAttributeSpecification uniqueAttributeSpecification in uniqueAttributeSpecifications)
        {
          _uniqueAttributeNames.Add(uniqueAttributeSpecification.AttributeName);
        }
      }
      _refAspectId = referencedAspectId;
      if (referencedAttributeSpecifications != null)
      {
        foreach (MultipleAttributeSpecification refAttributeSpecification in referencedAttributeSpecifications)
        {
          _refAttributeNames.Add(refAttributeSpecification.AttributeName);
        }
      }
    }

    /// <summary>
    /// Returns a read-only mapping of names to attribute specifications for all
    /// unique attributes of this media item aspect. Note that all attributes in the
    /// aspect cannot be unique as this will cause errors in update operations.
    /// </summary>
    [XmlIgnore]
    public IDictionary<string, AttributeSpecification> UniqueAttributeSpecifications
    {
      get
      {
        return _attributeSpecifications.Where(x => _uniqueAttributeNames.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
      }
    }

    #region Additional members for the XML serialization

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("UniqueAttributes")]
    [XmlArrayItem("UniqueAttribute")]
    // We use an array here because when using a List, stupid XML serializer only uses the property getter and fills the
    // List with its deserialized entries. When using an array, the property setter is invoked.
    public String[] XML_UniqueAttributes
    {
      get
      {
        return _uniqueAttributeNames.ToArray();
      }
      set
      {
        _uniqueAttributeNames.Clear();
        foreach(String uniqueAttributeName in value)
        {
          _uniqueAttributeNames.Add(uniqueAttributeName);
        }
      }
    }

    internal MultipleMediaItemAspectMetadata() { }

    new protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      return _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(MultipleMediaItemAspectMetadata)));
    }
    #endregion
  }
}

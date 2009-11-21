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
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Represents a bundle of media item metadata belonging together. Every media item has meta information
  /// from different aspects assigned.
  /// </summary>
  /// <remarks>
  /// Instances of this class are used to store and transport metadata of media items. Each media item typically
  /// has multiple instances of <see cref="MediaItemAspect"/> assigned, each holding data which semantically belong
  /// together. Importers will extract metadata either from the physical media files or gather additional data from
  /// other information sources like the internet.
  /// We do not fix that each media item aspect is only handled by a signle metadata extractor. MEs should complete
  /// each other by possibly setting different attribute values in the same media item aspect. For that reason, a
  /// metadata extractor updating a media item aspect which is already present in the media library for a media item
  /// needs a means to declare that an attribute should be left untouched.
  /// All attribute values of a freshly created <see cref="MediaItemAspect"/> are initialized with this <see cref="IGNORE"/>
  /// value by default. Methods which return attribute values will return <c>null</c> for those ignored values, but
  /// internally, the value <c>null</c> is handled different than the <see cref="IGNORE"/> value. To get the information
  /// if a special value should be ignored, call <see cref="IsIgnore"/>.
  /// </remarks>
  public class MediaItemAspect
  {
    #region Constants

    /// <summary>
    /// Special object to mark an attribute which is to have no defined value. This instance will used by
    /// importers/metadata extractors to explicitly label single attributes to be left unchanged in the media library,
    /// rather than setting them to <c>null</c>, which would clear an already existing value.
    /// </summary>
    public static readonly object IGNORE = new object();

    #endregion

    #region Protected fields

    protected MediaItemAspectMetadata _metadata;
    protected IDictionary<MediaItemAspectMetadata.AttributeSpecification, object> _aspectData =
        new Dictionary<MediaItemAspectMetadata.AttributeSpecification, object>();

    #endregion

    /// <summary>
    /// Creates a new media item aspect instance for the specified media item aspect <paramref name="metadata"/>.
    /// </summary>
    /// <param name="metadata">Media item aspect specification.</param>
    public MediaItemAspect(MediaItemAspectMetadata metadata)
    {
      _metadata = metadata;
      Initialize();
    }

    /// <summary>
    /// Returns the metadata descriptor for this media item aspect.
    /// </summary>
    public MediaItemAspectMetadata Metadata
    {
      get { return _metadata; }
    }

    /// <summary>
    /// Returns the value of this aspect for the given <paramref name="attributeSpecification"/>.
    /// </summary>
    /// <param name="attributeSpecification">Type of the attribute to retrieve.</param>
    /// <value>Value of this aspect for the given type, <c>null</c> if not set, <see cref="IGNORE"/> to transport
    /// an instruction to let the current value of the given type for this aspect unchanged.</value>
    public object this[MediaItemAspectMetadata.AttributeSpecification attributeSpecification]
    {
      get
      {
        object result;
        if (!_aspectData.TryGetValue(attributeSpecification, out result))
          throw new ArgumentException(string.Format("Attribute type '{0}' isn't present in media item aspect of type '{1}'",
              attributeSpecification.AttributeName, Metadata.Name));
        return result;
      }
    }

    public bool IsIgnore(MediaItemAspectMetadata.AttributeSpecification attributeSpecification)
    {
      return ReferenceEquals(_aspectData[attributeSpecification], IGNORE);
    }

    public void Ignore(MediaItemAspectMetadata.AttributeSpecification attributeSpecification)
    {
      _aspectData[attributeSpecification] = IGNORE;
    }

    /// <summary>
    /// Returns the attribute of type <paramref name="attributeSpecification"/> for this media item aspect.
    /// </summary>
    /// <param name="attributeSpecification">Attribute type to return.</param>
    /// <returns>Attribute of this media item aspect which belongs to the specified <paramref name="attributeSpecification"/>.
    /// In case the given attribute should be ignored, <c>null</c> will be returned.</returns>
    /// <typeparam name="T">Expected type of the attribute to return. This type has to be the same type as
    /// specified by <see cref="MediaItemAspectMetadata.AttributeSpecification.AttributeType"/>.</typeparam>
    /// <exception cref="ArgumentException">Thrown if an attribute type is requested which is not defined on this MIA's metadata or
    /// if the requested type <typeparamref name="T"/> doesn't match the defined type in the
    /// <paramref name="attributeSpecification"/>.</exception>
    public T GetAttribute<T>(MediaItemAspectMetadata.AttributeSpecification attributeSpecification)
    {
      CheckAttributeSpecification(attributeSpecification, typeof(T));
      CheckSingleAttribute(attributeSpecification);
      if (IsIgnore(attributeSpecification))
        return default(T);
      return (T) _aspectData[attributeSpecification];
    }

    /// <summary>
    /// Returns the attribute of type <paramref name="attributeSpecification"/> for this media item aspect.
    /// </summary>
    /// <param name="attributeSpecification">Attribute type to return.</param>
    /// <returns>Attribute of this media item aspect which belongs to the specified <paramref name="attributeSpecification"/>.
    /// In case the given attribute should be ignored, <c>null</c> will be returned.</returns>
    /// <exception cref="ArgumentException">Thrown if an attribute type is requested which is not defined on this MIA's metadata.</exception>
    public object GetAttribute(MediaItemAspectMetadata.AttributeSpecification attributeSpecification)
    {
      CheckSingleAttribute(attributeSpecification);
      if (IsIgnore(attributeSpecification))
        return null;
      return _aspectData[attributeSpecification];
    }

    /// <summary>
    /// Returns the collection attribute of type <paramref name="attributeSpecification"/> for this
    /// media item aspect.
    /// </summary>
    /// <param name="attributeSpecification">Attribute type to return.</param>
    /// <returns>Enumeration of attribute values of this media item aspect which belongs to the specified
    /// <paramref name="attributeSpecification"/>. In case the given attribute should be ignored, <c>null</c> will be
    /// returned.</returns>
    /// <typeparam name="T">Expected element type of the collection to return. This type has to be the same type as
    /// specified by <see cref="MediaItemAspectMetadata.AttributeSpecification.AttributeType"/>.</typeparam>
    /// <exception cref="ArgumentException">Thrown if an attribute type is requested which is not defined on this
    /// MIA's metadata or if the requested type <typeparamref name="T"/> doesn't match the defined type in the
    /// <paramref name="attributeSpecification"/>.</exception>
    public IEnumerable<T> GetCollectionAttribute<T>(MediaItemAspectMetadata.AttributeSpecification attributeSpecification)
    {
      CheckAttributeSpecification(attributeSpecification, typeof(T));
      CheckCollectionAttribute(attributeSpecification);
      if (IsIgnore(attributeSpecification))
        return null;
      return (IEnumerable<T>) _aspectData[attributeSpecification];
    }

    /// <summary>
    /// Returns the collection attribute of type <paramref name="attributeSpecification"/> for this
    /// media item aspect.
    /// </summary>
    /// <param name="attributeSpecification">Attribute type to return.</param>
    /// <returns>Enumeration of attribute values of this media item aspect which belongs to the specified
    /// <paramref name="attributeSpecification"/>. In case the given attribute should be ignored, <c>null</c> will be
    /// returned.</returns>
    /// <exception cref="ArgumentException">Thrown if an attribute type is requested which is not defined on this
    /// MIA's metadata.</exception>
    public IEnumerable GetCollectionAttribute(MediaItemAspectMetadata.AttributeSpecification attributeSpecification)
    {
      CheckCollectionAttribute(attributeSpecification);
      if (IsIgnore(attributeSpecification))
        return null;
      return (IEnumerable) _aspectData[attributeSpecification];
    }

    /// <summary>
    /// Sets the attribute of type <paramref name="attributeSpecification"/> for this media item aspect.
    /// </summary>
    /// <remarks>
    /// A call to this method automatically resets the "ignore" flag for the given attribute.
    /// </remarks>
    /// <param name="attributeSpecification">Attribute type to set.</param>
    /// <param name="value">Value to be set to the specified attribute.</param>
    /// <typeparam name="T">Type of the attribute to set. This type has to be the same type as specified
    /// by <see cref="MediaItemAspectMetadata.AttributeSpecification.AttributeType"/>.</typeparam>
    /// <exception cref="ArgumentException">Thrown if an attribute type is specified which is not defined on this MIA's metadata or
    /// if the provided type <typeparamref name="T"/> doesn't match the defined type in the
    /// <paramref name="attributeSpecification"/>.</exception>
    public void SetAttribute<T>(MediaItemAspectMetadata.AttributeSpecification attributeSpecification, T value)
    {
      CheckAttributeSpecification(attributeSpecification, typeof(T));
      CheckSingleAttribute(attributeSpecification);
      _aspectData[attributeSpecification] = value;
    }

    /// <summary>
    /// Sets the attribute of type <paramref name="attributeSpecification"/> for this media item aspect.
    /// </summary>
    /// <remarks>
    /// A call to this method automatically resets the "ignore" flag for the given attribute.
    /// </remarks>
    /// <param name="attributeSpecification">Attribute type to set.</param>
    /// <param name="value">Value to be set to the specified attribute.</param>
    /// <exception cref="ArgumentException">Thrown if an attribute type is specified which is not defined on this MIA's metadata or
    /// if the provided <paramref name="value"/>'s type doesn't match the defined type in the
    /// <paramref name="attributeSpecification"/>.</exception>
    public void SetAttribute(MediaItemAspectMetadata.AttributeSpecification attributeSpecification, object value)
    {
      if (value != null)
        CheckAttributeSpecification(attributeSpecification, value.GetType());
      CheckSingleAttribute(attributeSpecification);
      _aspectData[attributeSpecification] = value;
    }

    /// <summary>
    /// Sets the collection attribute of type <paramref name="attributeSpecification"/> for this
    /// media item aspect.
    /// </summary>
    /// <remarks>
    /// A call to this method automatically resets the "ignore" flag for the given attribute.
    /// </remarks>
    /// <param name="attributeSpecification">Attribute type to set.</param>
    /// <param name="value">Enumeration of values to be set to the specified attribute.</param>
    /// <typeparam name="T">Value type of the attribute enumeration to set. This type has to be the same
    /// type as specified by <see cref="MediaItemAspectMetadata.AttributeSpecification.AttributeType"/>.</typeparam>
    /// <exception cref="ArgumentException">Thrown if an attribute type is specified which is not defined on this
    /// MIA's metadata or if the provided element type <typeparamref name="T"/> doesn't match the defined type in the
    /// <paramref name="attributeSpecification"/>.</exception>
    public void SetCollectionAttribute<T>(MediaItemAspectMetadata.AttributeSpecification attributeSpecification, IEnumerable<T> value)
    {
      CheckAttributeSpecification(attributeSpecification, typeof(T));
      CheckCollectionAttribute(attributeSpecification);
      _aspectData[attributeSpecification] = value;
    }

    /// <summary>
    /// Sets the collection attribute of type <paramref name="attributeSpecification"/> for this
    /// media item aspect.
    /// </summary>
    /// <remarks>
    /// A call to this method automatically resets the "ignore" flag for the given attribute.
    /// </remarks>
    /// <param name="attributeSpecification">Attribute type to set.</param>
    /// <param name="value">Enumeration of values to be set to the specified attribute.</param>
    /// <exception cref="ArgumentException">Thrown if an attribute type is specified which is not defined on this
    /// MIA's metadata or if the most specific type of all elements in the <paramref name="value"/> enumeration
    /// doesn't match the defined type in the <paramref name="attributeSpecification"/>.</exception>
    public void SetCollectionAttribute(MediaItemAspectMetadata.AttributeSpecification attributeSpecification, IEnumerable value)
    {
      CheckCollectionAttribute(attributeSpecification);
      _aspectData[attributeSpecification] = value;
    }

    public void Serialize(XmlWriter writer)
    {
      writer.WriteStartElement("MediaItemAspect");
      writer.WriteAttributeString("AspectTypeId", _metadata.AspectId.ToString());
      foreach (MediaItemAspectMetadata.AttributeSpecification spec in _aspectData.Keys)
      {
        if (IsIgnore(spec))
          continue;
        writer.WriteStartElement("Attribute");
        writer.WriteAttributeString("Name", spec.AttributeName);
        if (spec.IsCollectionAttribute)
        {
          IEnumerable values = GetCollectionAttribute(spec);
          foreach (object obj in values)
            SerializeValue(writer, obj, spec.AttributeType);
        }
        else
          SerializeValue(writer, GetAttribute(spec), spec.AttributeType);
        writer.WriteEndElement(); // Attribute
      }
      writer.WriteEndElement(); // MediaItemAspect
    }

    public static MediaItemAspect Deserialize(XmlReader reader)
    {
      reader.ReadStartElement("MediaItemAspect");
      if (!reader.MoveToAttribute("AspectTypeId"))
        throw new ArgumentException("Media item aspect cannot be deserialized: 'AspectTypeId' attribute missing");
      Guid aspectTypeId = new Guid(reader.ReadContentAsString());
      reader.MoveToElement();
      IMediaItemAspectTypeRegistration miatr = ServiceScope.Get<IMediaItemAspectTypeRegistration>();
      MediaItemAspectMetadata miaType;
      if (!miatr.LocallyKnownMediaItemAspectTypes.TryGetValue(aspectTypeId, out miaType))
        throw new ArgumentException(string.Format("Media item aspect cannot be deserialized: Unknown media item aspect type '{0}'",
            aspectTypeId));
      MediaItemAspect result = new MediaItemAspect(miaType);
      while (reader.NodeType != XmlNodeType.EndElement)
      {
        reader.ReadStartElement("Attribute");
        if (!reader.MoveToAttribute("Name"))
          throw new ArgumentException("Media item aspect attribute cannot be deserialized: 'Name' attribute missing");
        String attributeName = reader.ReadContentAsString();
        reader.MoveToElement();
        MediaItemAspectMetadata.AttributeSpecification attributeSpec;
        if (!miaType.AttributeSpecifications.TryGetValue(attributeName, out attributeSpec))
          throw new ArgumentException(string.Format(
              "Media item aspect attribute cannot be deserialized: Unknown attribute specification '{0}'", attributeName));
        if (attributeSpec.IsCollectionAttribute)
        {
          ArrayList values = new ArrayList();
          while (reader.NodeType != XmlNodeType.EndElement)
            values.Add(DeserializeValue(reader, attributeSpec.AttributeType));
          result.SetCollectionAttribute(attributeSpec, values);
        }
        else
          result.SetAttribute(attributeSpec, DeserializeValue(reader, attributeSpec.AttributeType));
      }
      reader.ReadEndElement(); // MediaItemAspect
      return result;
    }

    public static void SerializeValue(XmlWriter writer, object obj, Type type)
    {
      writer.WriteStartElement("Value");
      if (MediaItemAspectMetadata.SUPPORTED_BASIC_TYPES.Contains(type))
        writer.WriteValue(obj);
      writer.WriteEndElement();
    }

    public static object DeserializeValue(XmlReader reader, Type type)
    {
      return reader.ReadElementContentAs(type, null);
    }

    protected void CheckCollectionAttribute(MediaItemAspectMetadata.AttributeSpecification attributeSpecification)
    {
      if (!attributeSpecification.IsCollectionAttribute)
        throw new ArgumentException(string.Format(
            "Media item aspect '{0}': Attribute '{1}' is not of a collection type, but a collection attribute is requrested",
            _metadata.Name, attributeSpecification.AttributeName));
    }

    protected void CheckSingleAttribute(MediaItemAspectMetadata.AttributeSpecification attributeSpecification)
    {
      if (attributeSpecification.IsCollectionAttribute)
        throw new ArgumentException(string.Format(
            "Media item aspect '{0}': Attribute '{1}' is of collection type, but a single attribute is requested",
            _metadata.Name, attributeSpecification.AttributeName));
    }

    protected void CheckAttributeSpecification(MediaItemAspectMetadata.AttributeSpecification attributeSpecification, Type type)
    {
      if (!_aspectData.ContainsKey(attributeSpecification))
        throw new ArgumentException(string.Format("Attribute '{0}' is not defined in media item aspect '{1}'",
            attributeSpecification.AttributeName, _metadata.Name));
      if (!type.IsAssignableFrom(attributeSpecification.AttributeType))
        throw new ArgumentException(string.Format(
            "Illegal media item aspect attribute access in MIA '{0}': Attribute '{1}' is of type {2}, but type {3} is requested",
            _metadata.Name, attributeSpecification.AttributeName, attributeSpecification.AttributeType.Name, type.Name));
    }

    /// <summary>
    /// Initializes all of this media item aspect's attributes to their default values.
    /// </summary>
    protected void Initialize()
    {
      _aspectData.Clear();
      foreach (MediaItemAspectMetadata.AttributeSpecification attributeSpecification in _metadata.AttributeSpecifications.Values)
        _aspectData[attributeSpecification] = IGNORE;
    }
  }
}

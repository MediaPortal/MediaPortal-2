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

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Represents a bundle of media item metadata belonging together. Every media item has meta information
  /// from different aspects assigned.
  /// The total of all media item metadata is classified into media item aspects.
  /// </summary>
  public class MediaItemAspect
  {
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

    public object this[MediaItemAspectMetadata.AttributeSpecification attributeSpecification]
    {
      get
      {
        object result;
        if (_aspectData.TryGetValue(attributeSpecification, out result))
          return result;
        return null;
      }
    }

    /// <summary>
    /// Returns the attribute of type <paramref name="attributeSpecification"/> for this media item aspect.
    /// </summary>
    /// <param name="attributeSpecification">Attribute type to return.</param>
    /// <returns>Attribute of this media item aspect which belongs to the specified <paramref name="attributeSpecification"/>.</returns>
    /// <typeparam name="T">Expected type of the attribute to return. This type has to be the same type as
    /// specified by <see cref="MediaItemAspectMetadata.AttributeSpecification.AttributeType"/>.</typeparam>
    /// <exception cref="ArgumentException">Thrown if an attribute type is requested which is not defined on this MIA's metadata or
    /// if the requested type <typeparamref name="T"/> doesn't match the defined type in the
    /// <paramref name="attributeSpecification"/>.</exception>
    public T GetAttribute<T>(MediaItemAspectMetadata.AttributeSpecification attributeSpecification)
    {
      CheckAttributeSpecification(attributeSpecification, typeof(T));
      CheckSingleAttribute(attributeSpecification);
      return (T) _aspectData[attributeSpecification];
    }

    /// <summary>
    /// Returns the attribute of type <paramref name="attributeSpecification"/> for this media item aspect.
    /// </summary>
    /// <param name="attributeSpecification">Attribute type to return.</param>
    /// <returns>Attribute of this media item aspect which belongs to the specified <paramref name="attributeSpecification"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if an attribute type is requested which is not defined on this MIA's metadata.</exception>
    public object GetAttribute(MediaItemAspectMetadata.AttributeSpecification attributeSpecification)
    {
      CheckSingleAttribute(attributeSpecification);
      return _aspectData[attributeSpecification];
    }

    /// <summary>
    /// Returns the collection attribute of type <paramref name="attributeSpecification"/> for this
    /// media item aspect.
    /// </summary>
    /// <param name="attributeSpecification">Attribute type to return.</param>
    /// <returns>Enumeration of attribute values of this media item aspect which belongs to the specified
    /// <paramref name="attributeSpecification"/>.</returns>
    /// <typeparam name="T">Expected element type of the collection to return. This type has to be the same type as
    /// specified by <see cref="MediaItemAspectMetadata.AttributeSpecification.AttributeType"/>.</typeparam>
    /// <exception cref="ArgumentException">Thrown if an attribute type is requested which is not defined on this
    /// MIA's metadata or if the requested type <typeparamref name="T"/> doesn't match the defined type in the
    /// <paramref name="attributeSpecification"/>.</exception>
    public IEnumerable<T> GetCollectionAttribute<T>(MediaItemAspectMetadata.AttributeSpecification attributeSpecification)
    {
      CheckAttributeSpecification(attributeSpecification, typeof(T));
      CheckCollectionAttribute(attributeSpecification);
      return (IEnumerable<T>) _aspectData[attributeSpecification];
    }

    /// <summary>
    /// Returns the collection attribute of type <paramref name="attributeSpecification"/> for this
    /// media item aspect.
    /// </summary>
    /// <param name="attributeSpecification">Attribute type to return.</param>
    /// <returns>Enumeration of attribute values of this media item aspect which belongs to the specified
    /// <paramref name="attributeSpecification"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if an attribute type is requested which is not defined on this
    /// MIA's metadata.</exception>
    public IEnumerable GetCollectionAttribute(MediaItemAspectMetadata.AttributeSpecification attributeSpecification)
    {
      CheckCollectionAttribute(attributeSpecification);
      return (IEnumerable) _aspectData[attributeSpecification];
    }

    /// <summary>
    /// Sets the attribute of type <paramref name="attributeSpecification"/> for this media item aspect.
    /// </summary>
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

    /// <summary>
    /// Returns the metadata descriptor for this media item aspect.
    /// </summary>
    public MediaItemAspectMetadata Metadata
    {
      get { return _metadata; }
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
      foreach (MediaItemAspectMetadata.AttributeSpecification attributeSpecification in _metadata.AttributeSpecifications)
        _aspectData[attributeSpecification] = null;
    }
  }
}

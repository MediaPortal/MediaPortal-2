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

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Holds the metadata descriptor for a <see cref="MediaItemAspect"/>.
  /// </summary>
  public class MediaItemAspectMetadata
  {
    /// <summary>
    /// Stores the metadata for one attribute in a media item aspect.
    /// </summary>
    public class AttributeSpecification
    {
      #region Protected fields

      protected string _attributeName;
      protected Type _attributeType;
      protected bool _cardinalityN;

      #endregion

      internal AttributeSpecification(string name, Type type, bool cardinalityN)
      {
        _attributeName = name;
        _attributeType = type;
        _cardinalityN = cardinalityN;
      }

      /// <summary>
      /// Name of the defined attribute. The name is unique among all attributes of the same
      /// media item aspect.
      /// </summary>
      public string AttributeName
      {
        get { return _attributeName; }
      }

      /// <summary>
      /// Runtime value type for the attribute instances.
      /// </summary>
      public Type AttributeType
      {
        get { return _attributeType; }
      }

      /// <summary>
      /// Returns the information if the attribute's instance value is a collection
      /// of <see cref="AttributeType"/> (<c>CardinalityN == true</c>) or a single
      /// instance of <see cref="AttributeType"/> (<c>CardinalityN == false</c>).
      /// In case (<c>CardinalityN == false</c>), the attribute's instance value is exactly
      /// of class <see cref="AttributeType"/> (or any subtype), in case
      /// (<c>CardinalityN == true</c>), the attribute's instance value is of class
      /// <c>ICollection&lt;T&gt;</c>, where <c>T</c> is exactly the
      /// <see cref="AttributeType"/>.
      /// </summary>
      public bool CardinalityN
      {
        get { return _cardinalityN; }
      }
    }

    #region Protected fields

    protected string _aspectName;
    protected Guid _aspectId;
    protected bool _isSystemAspect;
    protected ICollection<AttributeSpecification> _attributeSpecifications;

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
    public MediaItemAspectMetadata(Guid aspectId, string aspectName,
        IEnumerable<AttributeSpecification> attributeSpecifications)
    {
      _aspectId = aspectId;
      _aspectName = aspectName;
      _attributeSpecifications = new List<AttributeSpecification>(attributeSpecifications).AsReadOnly();
    }

    /// <summary>
    /// Returns the globally unique ID of this aspect.
    /// </summary>
    public Guid AspectId
    {
      get { return _aspectId; }
    }

    /// <summary>
    /// Name of this aspect. Can be shown in the gui, for example. The returned string may be
    /// a localized string label (i.e. "[section.name]").
    /// </summary>
    public string Name
    {
      get { return _aspectName; }
    }

    /// <summary>
    /// Gets or sets the information if this aspect is a system aspect. System aspects must not be deleted.
    /// This property can only be set internal.
    /// </summary>
    public bool IsSystemAspect
    {
      get { return _isSystemAspect; }
      internal set { _isSystemAspect = value; }
    }

    /// <summary>
    /// Returns a read-only collection of available attributes of this media item aspect.
    /// </summary>
    public ICollection<AttributeSpecification> AttributeSpecifications
    {
      get { return _attributeSpecifications; }
    }

    /// <summary>
    /// Creates a specification for a new attribute which can be used in a new
    /// <see cref="MediaItemAspectMetadata"/> instance.
    /// </summary>
    public static AttributeSpecification CreateAttributeSpecification(string attributeName,
        Type attributeType, bool cardinalityN)
    {
      // TODO: check attributeType if it is a supported database type
      return new AttributeSpecification(attributeName, attributeType, cardinalityN);
    }
  }
}

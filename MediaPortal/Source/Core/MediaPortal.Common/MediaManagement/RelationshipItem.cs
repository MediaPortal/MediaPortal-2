#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// Encapsulates the role, linked role and aspects of a relationship extracted by a <see cref="IRelationshipRoleExtractor"/>.
  /// </summary>
  public class RelationshipItem : IXmlSerializable
  {
    #region Protected fields

    protected Guid _role;
    protected Guid _linkedRole;
    protected readonly IDictionary<Guid, IList<MediaItemAspect>> _aspects;

    #endregion

    /// <summary>
    /// Creates a new <see cref="RelationshipItem"/>.
    /// </summary>
    /// <param name="role">The role of the media item that this relationship belongs to.</param>
    /// <param name="linkedRole">The role of this relationship.</param>
    /// <param name="aspects">The extracted aspects of the relationship item.</param>
    public RelationshipItem(Guid role, Guid linkedRole, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      _role = role;
      _linkedRole = linkedRole;
      _aspects = new Dictionary<Guid, IList<MediaItemAspect>>(aspects);
    }

    /// <summary>
    /// The role of the aspects this relationship was extracted from.
    /// </summary>
    public Guid Role
    {
      get { return _role; }
    }

    /// <summary>
    /// The role of the aspects of this relationship.
    /// </summary>
    public Guid LinkedRole
    {
      get { return _linkedRole; }
    }

    /// <summary>
    /// The extracted aspects of the relationship item.
    /// </summary>
    public IDictionary<Guid, IList<MediaItemAspect>> Aspects
    {
      get { return _aspects; }
    }

    XmlSchema IXmlSerializable.GetSchema()
    {
      return null;
    }

    void IXmlSerializable.ReadXml(XmlReader reader)
    {
      try
      {
        // First read attributes, then check for empty start element
        if (!reader.MoveToAttribute("Role"))
          throw new ArgumentException("Role attribute not present");
        _role = new Guid(reader.Value);
        if (!reader.MoveToAttribute("LinkedRole"))
          throw new ArgumentException("LinkedRole attribute not present");
        _linkedRole = new Guid(reader.Value);
        if (reader.IsEmptyElement)
          return;
      }
      finally
      {
        reader.ReadStartElement();
      }
      while (reader.NodeType != XmlNodeType.EndElement)
      {
        if (reader.Name == "Aspect")
        {
          MediaItemAspect mia = MediaItemAspect.Deserialize(reader);
          if (mia is SingleMediaItemAspect)
          {
            MediaItemAspect.SetAspect(_aspects, (SingleMediaItemAspect)mia);
          }
          else if (mia is MultipleMediaItemAspect)
          {
            MediaItemAspect.AddOrUpdateAspect(_aspects, (MultipleMediaItemAspect)mia);
          }
        }
        else
        {
          reader.Read();
        }
      }
      reader.ReadEndElement(); // RI
    }

    void IXmlSerializable.WriteXml(XmlWriter writer)
    {
      writer.WriteAttributeString("Role", _role.ToString("D"));
      writer.WriteAttributeString("LinkedRole", _linkedRole.ToString("D"));
      foreach (IList<MediaItemAspect> list in _aspects.Values)
        foreach (MediaItemAspect mia in list)
          mia.Serialize(writer);
    }

    public void Serialize(XmlWriter writer)
    {
      writer.WriteStartElement("RI"); // MediaItem
      ((IXmlSerializable)this).WriteXml(writer);
      writer.WriteEndElement(); // MediaItem
    }

    public static RelationshipItem Deserialize(XmlReader reader)
    {
      RelationshipItem result = new RelationshipItem();
      ((IXmlSerializable)result).ReadXml(reader);
      return result;
    }

    #region Additional members for the XML serialization

    internal RelationshipItem()
    {
      _aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
    }

    #endregion
  }
}

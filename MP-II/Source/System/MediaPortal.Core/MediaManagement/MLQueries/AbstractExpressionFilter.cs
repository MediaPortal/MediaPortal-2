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

using System.Xml.Serialization;

namespace MediaPortal.Core.MediaManagement.MLQueries
{
  /// <summary>
  /// Abstract super class for all filters providing an filter expression and an escape char, like LIKE and SIMILAR TO.
  /// </summary>
  public abstract class AbstractExpressionFilter : IAttributeFilter
  {
    protected MediaItemAspectMetadata.AttributeSpecification _attributeType;
    protected string _expression;
    protected char _escapeChar;

    protected AbstractExpressionFilter(MediaItemAspectMetadata.AttributeSpecification attributeType,
        string expression, char escapeChar)
    {
      _attributeType = attributeType;
      _expression = expression;
      _escapeChar = escapeChar;
    }

    [XmlIgnore]
    public MediaItemAspectMetadata.AttributeSpecification AttributeType
    {
      get { return _attributeType; }
    }

    [XmlIgnore]
    public string Expression
    {
      get { return _expression; }
    }

    [XmlIgnore]
    public char EscapeChar
    {
      get { return _escapeChar; }
    }

    #region Additional members for the XML serialization

    internal AbstractExpressionFilter() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("AttributeType", IsNullable = false)]
    public MediaItemAspectMetadata.AttributeSpecification XML_AttributeType
    {
      get { return _attributeType; }
      set { _attributeType = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("Expression")]
    public string XML_Expression
    {
      get { return _expression; }
      set { _expression = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("EscapeChar")]
    public char XML_EscapeChar
    {
      get { return _escapeChar; }
      set { _escapeChar = value; }
    }

    #endregion
  }
}

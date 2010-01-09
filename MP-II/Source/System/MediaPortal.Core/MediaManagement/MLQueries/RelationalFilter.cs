#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
  /// Relational operator to be used in the <see cref="RelationalFilter"/> class.
  /// </summary>
  public enum RelationalOperator
  {
    /// <summary>
    /// Operator "Equal".
    /// </summary>
    EQ,

    /// <summary>
    /// Operator "Unequal".
    /// </summary>
    NEQ,

    /// <summary>
    /// Operator "Lower than".
    /// </summary>
    LT,

    /// <summary>
    /// Operator "Lower than or equal".
    /// </summary>
    LE,

    /// <summary>
    /// Operator "Greater than".
    /// </summary>
    GT,

    /// <summary>
    /// Operator "Greater than or equal".
    /// </summary>
    GE
  }

  /// <summary>
  /// Specifies an expression which filters media items by an attribute of one of their media item aspects.
  /// </summary>
  public class RelationalFilter : AbstractAttributeFilter
  {
    protected RelationalOperator _operator;
    protected object _filterValue;

    public RelationalFilter(MediaItemAspectMetadata.AttributeSpecification attributeType,
        RelationalOperator op, object filterValue) : base(attributeType)
    {
      _operator = op;
      _filterValue = filterValue;
    }

    [XmlIgnore]
    public RelationalOperator Operator
    {
      get { return _operator; }
    }

    [XmlIgnore]
    public object FilterValue
    {
      get { return _filterValue; }
    }

    #region Additional members for the XML serialization

    internal RelationalFilter() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("Operator", IsNullable = false)]
    public RelationalOperator XML_Operator
    {
      get { return _operator; }
      set { _operator = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("FilterValue", IsNullable = false)]
    public object XML_FilterValue
    {
      get { return _filterValue; }
      set { _filterValue = value; }
    }

    #endregion
  }
}

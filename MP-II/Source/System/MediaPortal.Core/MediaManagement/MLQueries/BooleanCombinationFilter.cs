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
using System.Xml.Serialization;

namespace MediaPortal.Core.MediaManagement.MLQueries
{
  public enum BooleanOperator
  {
    And,
    Or
  }

  /// <summary>
  /// Specifies an expression which combines multiple attribute filters with a boolean operator.
  /// </summary>
  public class BooleanCombinationFilter : IFilter
  {
    protected BooleanOperator _operator;
    protected IFilter[] _operands;

    public BooleanCombinationFilter(BooleanOperator op, IFilter[] operands)
    {
      _operator = op;
      _operands = operands;
    }

    [XmlIgnore]
    public BooleanOperator Operator
    {
      get { return _operator; }
    }

    [XmlIgnore]
    public IFilter[] Operands
    {
      get { return _operands; }
    }

    #region Additional members for the XML serialization

    internal BooleanCombinationFilter() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("Operator", IsNullable = false)]
    public BooleanOperator XML_Operator
    {
      get { return _operator; }
      set { _operator = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("Operands", IsNullable = false)]
    [XmlArrayItem("Between", typeof(BetweenFilter))]
    [XmlArrayItem("BooleanCombination", typeof(BooleanCombinationFilter))]
    [XmlArrayItem("In", typeof(InFilter))]
    [XmlArrayItem("Like", typeof(LikeFilter))]
    [XmlArrayItem("SimilarTo", typeof(SimilarToFilter))]
    [XmlArrayItem("Not", typeof(NotFilter))]
    [XmlArrayItem("Relational", typeof(RelationalFilter))]
    public object[] XML_Operands
    {
      get { return _operands; }
      set
      {
        _operands = new IFilter[value.Length];
        Array.Copy(value, _operands, value.Length);
      }
    }

    #endregion
  }
}

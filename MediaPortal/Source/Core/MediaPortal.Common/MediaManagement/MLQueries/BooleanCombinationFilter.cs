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
using System.Linq;
using System.Xml.Serialization;
using MediaPortal.Utilities;

namespace MediaPortal.Common.MediaManagement.MLQueries
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
    protected List<IFilter> _operands;

    public BooleanCombinationFilter(BooleanOperator op, IEnumerable<IFilter> operands)
    {
      _operator = op;
      _operands = new List<IFilter>(operands);
      if (_operands.Count == 0)
        throw new ArgumentException("The filter operands enumeration must not be empty");
    }

    [XmlIgnore]
    public BooleanOperator Operator
    {
      get { return _operator; }
      set { _operator = value; }
    }

    [XmlIgnore]
    public ICollection<IFilter> Operands
    {
      get { return _operands; }
      set { _operands = new List<IFilter>(value); }
    }

    public static IFilter CombineFilters(BooleanOperator op, IEnumerable<IFilter> filters)
    {
      if (filters == null)
        return null;
      IFilter[] filtersArray = filters.Where(fi => fi != null).ToArray();
      return filtersArray.Length == 0 ? null :
          (filtersArray.Length == 1 ? filtersArray[0] : new BooleanCombinationFilter(op, filters.ToArray()));
    }

    public static IFilter CombineFilters(BooleanOperator op, params IFilter[] filters)
    {
      return CombineFilters(op, (IEnumerable<IFilter>) filters);
    }

    public override string ToString()
    {
      return StringUtils.Join(" " + _operator + " ", _operands);
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
    [XmlArrayItem("Not", typeof(NotFilter))]
    [XmlArrayItem("Relational", typeof(RelationalFilter))]
    [XmlArrayItem("Empty", typeof(EmptyFilter))]
    [XmlArrayItem("RelationalUserData", typeof(RelationalUserDataFilter))]
    [XmlArrayItem("EmptyUserData", typeof(EmptyUserDataFilter))]
    [XmlArrayItem("False", typeof(FalseFilter))]
    [XmlArrayItem("MediaItemIds", typeof(MediaItemIdFilter))]
    [XmlArrayItem("Relationship", typeof(RelationshipFilter))]
    [XmlArrayItem("FilterRelationship", typeof(FilteredRelationshipFilter))]
    // Necessary to have an object ARRAY here, else the serialization algorithm cannot cope with polymorph values
    public object[] XML_Operands
    {
      get { return _operands.ToArray(); }
      set
      {
        _operands = new List<IFilter>(value.Length);
        foreach (IFilter filter in value)
          _operands.Add(filter);
      }
    }

    #endregion
  }
}

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
using System.Xml.Serialization;

namespace MediaPortal.Common.MediaManagement.MLQueries
{
  /// <summary>
  /// Specifies an expression which filters media items by a user data key value.
  /// </summary>
  public class RelationalUserDataFilter : AbstractUserDataFilter
  {
    protected RelationalOperator _operator;
    protected string _filterValue;
    protected bool _includeEmpty;

    public RelationalUserDataFilter(Guid userProfile, string userDataKey, RelationalOperator op, string filterValue, bool includeEmpty = false) : 
      base(userProfile, userDataKey)
    {
      _operator = op;
      _filterValue = filterValue;
      _includeEmpty = includeEmpty;
    }

    [XmlIgnore]
    public RelationalOperator Operator
    {
      get { return _operator; }
      set { _operator = value; }
    }

    [XmlIgnore]
    public string FilterValue
    {
      get { return _filterValue; }
      set { _filterValue = value; }
    }

    [XmlIgnore]
    public bool IncludeEmpty
    {
      get { return _includeEmpty; }
      set { _includeEmpty = value; }
    }

    public override string ToString()
    {
      return _userDataKey + " " + _operator + " " + _filterValue + (_includeEmpty ? "(and Empty)" : "");
    }

    #region Additional members for the XML serialization

    internal RelationalUserDataFilter() { }

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
    public string XML_FilterValue
    {
      get { return _filterValue; }
      set { _filterValue = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("IncludeEmpty", IsNullable = false)]
    public bool XML_IncludeEmpty
    {
      get { return _includeEmpty; }
      set { _includeEmpty = value; }
    }

    #endregion
  }
}

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

using System.Collections.Generic;

namespace MediaPortal.Core.MediaManager.Views
{
  public interface IQuery
  {
    /// <summary>
    /// Gets or sets the sub queries.
    /// </summary>
    /// <value>The sub queries.</value>
    List<IExpression> SubQueries { get; set; }

    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    /// <value>The key.</value>
    string Key { get; set; }

    /// <summary>
    /// Gets or sets the operator.
    /// </summary>
    /// <value>The operator.</value>
    Operator Operator { get; set; }


    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    /// <value>The value.</value>
    object Value { get; set; }

    int Limit { get;set;}

    SortOrder Sort { get;set;}
    List<string> SortFields { get;set;}

    /// <summary>
    /// Gets the field names used in this query.
    /// </summary>
    /// <value>The field names.</value>
    List<string> FieldNames { get; }

    List<string> FromStatement { get; }

    string WhereStatement { get; }
  }
}

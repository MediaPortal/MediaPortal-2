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

namespace MediaPortal.Database
{
  public interface IDbAttribute : ICloneable
  {
    /// <summary>
    /// Gets the attribute name.
    /// </summary>
    /// <value>The name.</value>
    string Name { get; }

    /// <summary>
    /// Gets the attribute type.
    /// </summary>
    /// <value>The type.</value>
    Type Type { get; }

    /// <summary>
    /// Gets a value indicating whether this attribute is changed.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is changed; otherwise, <c>false</c>.
    /// </value>
    bool IsChanged { get; set; }

    /// <summary>
    /// Gets/sets the attribute value.
    /// </summary>
    /// <value>The attribute value.</value>
    object Value { get; set; }

    /// <summary>
    /// if IsList is true, then this property can be used to get/set the values
    /// </summary>
    /// <value>The values.</value>
    IList<string> Values { get; set; }

    /// <summary>
    /// Gets the size of the field.
    /// </summary>
    /// <value>The size.</value>
    int Size { get; }

    /// <summary>
    /// Gets a value indicating whether this attribute contains a list of items.
    /// </summary>
    /// <value><c>true</c> if this attribute contains a list of items; otherwise, <c>false</c>.</value>
    bool IsList { get; }
  }
}

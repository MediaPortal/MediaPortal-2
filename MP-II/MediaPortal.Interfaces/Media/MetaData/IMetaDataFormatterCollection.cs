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

namespace MediaPortal.Media.MetaData
{
  /// <summary>
  /// interface for the meta formatter collection service
  /// This service is simply a class which holds a collection of all metadata formatters available within MP-II
  /// </summary>
  public interface IMetaDataFormatterCollection
  {
    /// <summary>
    /// Gets the list of all formatters registered
    /// </summary>
    /// <value>The formatters.</value>
    List<IMetaDataFormatter> Formatters { get;}

    /// <summary>
    /// Gets the formatter with the specified name.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>the formatter for the name</returns>
    IMetaDataFormatter Get(string name);

    /// <summary>
    /// Adds a new formatter
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="formatter">The formatter.</param>
    void Add(string name, IMetaDataFormatter formatter);

    /// <summary>
    /// Determines whether the collection contains a formatter with the name specified
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>
    /// 	<c>true</c> if the collection contains a formatter with the name specified; otherwise, <c>false</c>.
    /// </returns>
    bool Contains(string name);

  }
}

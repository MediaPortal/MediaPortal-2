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
using MediaPortal.Media.MediaManagement.Views;

namespace MediaPortal.Media.MediaManagement
{
  /// <summary>
  /// The MediaDatabase provides access to a database of all registered media files in the current
  /// MP-II system. It provides an interface to the locally or remotely located MediaLibrary.
  /// </summary>
  public interface IMediaDatabase
  {
    /// <summary>
    /// Evaluates the specified query on this media database and returns the qualifying media items.
    /// </summary>
    /// <param name="query">The query to evaluate on this media database.</param>
    /// <returns>List of qualifying media items.</returns>
    IList<IAbstractMediaItem> Evaluate(IQuery query);

    // TODO: Methods to access special media item data, to provide import data, ...
  }
}

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
using MediaPortal.Media.MediaManager.Views;

namespace MediaPortal.Media.MediaManager
{
  /// <summary>
  /// interface for a provider
  /// </summary>
  public interface IProvider
  {
    /// <summary>
    /// get the root containers for this provider
    /// </summary>
    List<IRootContainer> RootContainers { get; }

    /// <summary>
    /// get the title for this provider
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the view.
    /// </summary>
    /// <param name="query">The query for the view.</param>
    /// <returns>list of containers&items for the query</returns>
    List<IAbstractMediaItem> GetView(IView query, IRootContainer root, IRootContainer parent);

  }
}

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

namespace MediaPortal.Media.MediaManagement.Views
{
  /// <summary>
  /// Views are used to define predefined or user-configured excerpts of some media
  /// items out of the collectivity of all available media. A view specifies a set of media items
  /// by evaluating a <see cref="Query"/> on the media library. The view (logically) contains those
  /// media items which fulfill the query criteria.
  /// </summary>
  public interface IView 
  {
    /// <summary>
    /// Returns the display name for this view.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets or sets the query to evaluate the media items which are qualified for this view.
    /// </summary>
    IQuery Query { get; set; }
  }
}

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

namespace MediaPortal.SkinEngine.Controls
{
  /// <summary>
  /// This interface provides methods to get information about the current
  /// scrolling state.
  /// </summary>
  /// <remarks>
  /// In contrast to the WPF <c>IScrollViewer</c> interface, where direct scrolling is supported,
  /// MediaPortal only supports indirect scrolling, which is done as a result of focus movements.
  /// Those focus movements are triggered by calls to the interface methods of
  /// <see cref="IScrollViewerFocusSupport"/>, while this interface only provides information
  /// about the current scrolling state.
  /// <see cref="IScrollViewerFocusSupport"/> contains the extended focus movement methods, which are
  /// only indirectly related to scrolling, while <see cref="IScrollInfo"/> contains methods directly
  /// related to the scrolling. Typically, both interfaces are implemented by scrollable controls.
  /// </remarks>
  public interface IScrollInfo
  {
    // TODO: Methods for style to get scrollbar size and position
  }
}

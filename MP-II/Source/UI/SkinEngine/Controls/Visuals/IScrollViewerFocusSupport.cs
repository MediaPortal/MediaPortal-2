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

namespace MediaPortal.SkinEngine.Controls
{
  /// <summary>
  /// This interface supports focus movements inside a scroll viewer.
  /// </summary>
  /// <remarks>
  /// It replaces the typical <c>IScrollViewer</c> interface which is used in WPF,
  /// where a mouse is available to support a manual scrolling. In MediaPortal, scrolling is not supported
  /// in that way it works in those libraries. Here, scrolling takes place as a result of focus
  /// movements. Implementors of this interface provide the functionality to scroll not only
  /// with the normal arrow keys, but also by focus movement of a complete "page" and by moving the focus
  /// to the "home" position or to the "end".
  /// This interface is related to the <see cref="IScrollInfo"/> interface.
  /// <see cref="IScrollViewerFocusSupport"/> contains the extended focus movement methods, which are
  /// only indirectly related to scrolling, while <see cref="IScrollInfo"/> contains methods directly
  /// related to the scrolling. Typically, both interfaces are implemented by scrollable controls.
  /// </remarks>
  public interface IScrollViewerFocusSupport
  {
    /// <summary>
    /// Moves the focus down to the next control.
    /// </summary>
    /// <returns><c>true</c>, if the focus could be moved, else <c>false</c>.</returns>
    bool FocusDown();

    /// <summary>
    /// Moves the focus left to the next control.
    /// </summary>
    /// <returns><c>true</c>, if the focus could be moved, else <c>false</c>.</returns>
    bool FocusLeft();

    /// <summary>
    /// Moves the focus right to the next control.
    /// </summary>
    /// <returns><c>true</c>, if the focus could be moved, else <c>false</c>.</returns>
    bool FocusRight();

    /// <summary>
    /// Moves the focus up to the next control.
    /// </summary>
    /// <returns><c>true</c>, if the focus could be moved, else <c>false</c>.</returns>
    bool FocusUp();

    /// <summary>
    /// Moves the focus down by one page.
    /// </summary>
    /// <returns><c>true</c>, if the focus could be moved, else <c>false</c>.</returns>
    bool FocusPageDown();

    /// <summary>
    /// Moves the focus up by one page.
    /// </summary>
    /// <returns><c>true</c>, if the focus could be moved, else <c>false</c>.</returns>
    bool FocusPageUp();

    /// <summary>
    /// Moves the focus left by one page.
    /// </summary>
    /// <returns><c>true</c>, if the focus could be moved, else <c>false</c>.</returns>
    bool FocusPageLeft();

    /// <summary>
    /// Moves the focus right by one page.
    /// </summary>
    /// <returns><c>true</c>, if the focus could be moved, else <c>false</c>.</returns>
    bool FocusPageRight();

    /// <summary>
    /// Moves the focus to the home position.
    /// </summary>
    /// <returns><c>true</c>, if the focus could be moved, else <c>false</c>.</returns>
    bool FocusHome();

    /// <summary>
    /// Moves the focus to the end position.
    /// </summary>
    /// <returns><c>true</c>, if the focus could be moved, else <c>false</c>.</returns>
    bool FocusEnd();
  }
}

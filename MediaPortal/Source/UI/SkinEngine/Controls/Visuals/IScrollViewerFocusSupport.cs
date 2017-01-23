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

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
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
  /// This interface is related to the <see cref="IScrollViewerFocusSupport"/> interface.
  /// <see cref="IScrollInfo"/> contains the extended focus movement methods, which are
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

    // TODO: Those two methods don't really fit into this interface (don't have anything to do with focus...).
    // We should move them somewhere else.

    /// <summary>
    /// Scrolls down the specified number of items. When the number of items is bigger than what can be scrolled, scrolling will perform the maximum number possible.
    /// </summary>
    /// <returns><c>true</c>, if scrolling could be performed, else <c>false</c>.</returns>
    bool ScrollDown(int numLines);

    /// <summary>
    /// Scrolls up the specified number of items. When the number of items is bigger than what can be scrolled, it will scroll the maximum number possible.
    /// </summary>
    /// <param name="numLines">Number of detents the mousewheel was moved.</param>
    /// <returns><c>true</c>, if scrolling could be performed, else <c>false</c>.</returns>
    bool ScrollUp(int numLines);

    /// <summary>
    /// Begins a relative scrolling progress. This method is usually invoked when a TouchDown event happens.
    /// </summary>
    /// <returns></returns>
    bool BeginScroll();

    /// <summary>
    /// Scrolls by the given number of pixels. This method is usually invoked when a TouchMove event happens.
    /// </summary>
    /// <param name="deltaX">Relative difference in x direction.</param>
    /// <param name="deltaY">Relative difference in y direction.</param>
    /// <returns></returns>
    bool Scroll(float deltaX, float deltaY);

    /// <summary>
    /// Ends a relative scrolling progress. This method is usually invoked when a TouchUp event happens.
    /// </summary>
    /// <returns></returns>
    bool EndScroll();
  }
}
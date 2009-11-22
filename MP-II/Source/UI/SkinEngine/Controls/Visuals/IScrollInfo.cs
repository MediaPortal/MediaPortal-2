#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  /// <summary>
  /// Will be used for the <see cref="IScrollInfo.Scrolled"/> event.
  /// </summary>
  /// <param name="sender">The <see cref="IScrollInfo"/> instance which scrolled.</param>
  public delegate void ScrolledDlgt(object sender);

  /// <summary>
  /// This interface provides methods to get information about the current
  /// scrolling state.
  /// </summary>
  /// <remarks>
  /// In contrast to the WPF <c>IScrollViewer</c> interface, where direct scrolling is supported,
  /// MediaPortal only supports indirect scrolling, which is done as a result of focus movements.
  /// Those focus movements are triggered by calls to the interface methods of
  /// <see cref="IScrollViewerFocusSupport"/>. In contrast, this interface provides information
  /// about the current scrolling state.
  /// <see cref="IScrollViewerFocusSupport"/> contains the extended focus movement methods, which are
  /// only indirectly related to scrolling, while <see cref="IScrollInfo"/> contains methods directly
  /// related to the scrolling. Typically, both interfaces are implemented by scrollable controls.
  /// </remarks>
  public interface IScrollInfo
  {
    /// <summary>
    /// Will be called when 
    /// </summary>
    event ScrolledDlgt Scrolled;

    /// <summary>
    /// Gets or switches the ability to scroll. If this property is set, this object
    /// is contained in a scroll container. This may affect the desired width and height
    /// this object will declare: If <see cref="CanScroll"/> is set to <c>true</c>,
    /// this object may declare a lesser size as desired size than if <see cref="CanScroll"/> is
    /// set to false.
    /// </summary>
    bool CanScroll { get; set; }

    /// <summary>
    /// Returns the total width of all items to be displayed. This is normally equal or bigger
    /// than the viewport width.
    /// </summary>
    float TotalWidth { get; }

    /// <summary>
    /// Returns the total height of all items to be displayed. This is normally equal or bigger
    /// than the viewport height.
    /// </summary>
    float TotalHeight { get; }

    /// <summary>
    /// Returns the width of the viewport. If this value is <c>0</c>, the horizontal scrollbar
    /// knob is not shown.
    /// </summary>
    float ViewPortWidth { get; }

    /// <summary>
    /// Returns the starting position of the horizontal viewport.
    /// </summary>
    float ViewPortStartX { get; }

    /// <summary>
    /// Returns the height of the viewport. If this value is <c>0</c>, the vertical scrollbar
    /// knob is not shown.
    /// </summary>
    float ViewPortHeight { get; }

    /// <summary>
    /// Returns the starting position of the vertical viewport.
    /// </summary>
    float ViewPortStartY { get; }

    /// <summary>
    /// Returns the information if the viewport is at the top of the available area.
    /// </summary>
    bool IsViewPortAtTop { get; }

    /// <summary>
    /// Returns the information if the viewport is at the bottom of the available area.
    /// </summary>
    bool IsViewPortAtBottom { get; }

    /// <summary>
    /// Returns the information if the viewport is at the left side of the available area.
    /// </summary>
    bool IsViewPortAtLeft { get; }

    /// <summary>
    /// Returns the information if the viewport is at the right side of the available area.
    /// </summary>
    bool IsViewPortAtRight { get; }
  }
}
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

using System.Drawing;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.Control.InputManager;
using MediaPortal.SkinEngine.ScreenManagement;

namespace MediaPortal.SkinEngine.InputManagement
{
  public class FocusManager
  {
    static FrameworkElement _focusedElement = null;
    static Screen _currentScreen = null;

    /// <summary>
    /// Returns the currently focused element.
    /// </summary>
    public static FrameworkElement FocusedElement
    {
      get { return _focusedElement; }
    }

    /// <summary>
    /// Informs the focus manager that the specified <paramref name="focusedElement"/> gained the
    /// focus. This will reset the focus on the former focused element.
    /// This will be called from the <see cref="FrameworkElement"/> class.
    /// </summary>
    /// <param name="focusedElement">The element which gained focus.</param>
    public static void FrameworkElementGotFocus(FrameworkElement focusedElement)
    {
      if (_focusedElement != focusedElement)
      {
        if (_focusedElement != null)
          if (_focusedElement.HasFocus)
            _focusedElement.HasFocus = false;
        _focusedElement = focusedElement;
      }
    }

    /// <summary>
    /// Informs the focus manager that the specified <paramref name="focusedElement"/> lost its
    /// focus. This will be called from the <see cref="FrameworkElement"/> class.
    /// </summary>
    /// <param name="focusedElement">The element which had focus before.</param>
    public static void FrameworkElementLostFocus(FrameworkElement focusedElement)
    {
      if (_focusedElement == focusedElement)
        _focusedElement = null;
    }

    public static void AttachInput(Screen screen)
    {
      _focusedElement = null;
      _currentScreen = screen;
    }

    public static void DetachInput(Screen screen)
    {
      _focusedElement = null;
      _currentScreen = null;
    }

    public static void OnKeyPressed(ref Key key)
    {
      if (key == Key.None)
        return;
      FrameworkElement cntl;
      cntl = PredictFocus(FocusedElement == null ? new RectangleF?() : FocusedElement.ActualBounds, key);
      if (cntl != null)
      {
        cntl.HasFocus = true;
        if (cntl.HasFocus)
          key = Key.None;
      }
    }

    public static FrameworkElement FindFirstFocusableElement(FrameworkElement searchRoot)
    {
      return searchRoot.PredictFocus(null, MoveFocusDirection.Down);
    }

    /// <summary>
    /// Predicts which FrameworkElement should get the focus when the specified <paramref name="key"/>
    /// was pressed.
    /// </summary>
    /// <param name="currentFocusRect">The borders of the currently focused control.</param>
    /// <param name="key">The key to evaluate.</param>
    /// <returns>Framework element whcih gets focus when the specified <paramref name="key"/> was
    /// pressed, or <c>null</c>, if no focus change should take place.</returns>
    public static FrameworkElement PredictFocus(RectangleF? currentFocusRect, Key key)
    {
      if (_currentScreen == null || _currentScreen.RootElement == null)
        return null;
      if (key == Key.Up)
        return _currentScreen.RootElement.PredictFocus(currentFocusRect, MoveFocusDirection.Up);
      else if (key == Key.Down)
        return _currentScreen.RootElement.PredictFocus(currentFocusRect, MoveFocusDirection.Down);
      else if (key == Key.Left)
        return _currentScreen.RootElement.PredictFocus(currentFocusRect, MoveFocusDirection.Left);
      else if (key == Key.Right)
        return _currentScreen.RootElement.PredictFocus(currentFocusRect, MoveFocusDirection.Right);
      return null;
    }
  }
}

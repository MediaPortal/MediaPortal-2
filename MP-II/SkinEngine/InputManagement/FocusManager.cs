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
using MediaPortal.Core;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.Control.InputManager;

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
      IInputManager inputManager = ServiceScope.Get<IInputManager>();
      inputManager.KeyPressed += OnKeyPressed;
    }

    public static void DetachInput(Screen screen)
    {
      _focusedElement = null;
      _currentScreen = null;
      IInputManager inputManager = ServiceScope.Get<IInputManager>();
      inputManager.KeyPressed -= OnKeyPressed;
    }

    public static void OnKeyPressed(ref Key key)
    {
      if (key == Key.None)
        return;
      FrameworkElement cntl;
      if (FocusedElement != null)
        cntl = PredictFocus(FocusedElement, key);
      else
        cntl = PredictFocus(key);
      if (cntl != null)
      {
        cntl.HasFocus = true;
        if (cntl.HasFocus)
          key = Key.None;
      }
    }

    public static FrameworkElement FindFirstFocusableElement(FrameworkElement root)
    {
      return PredictFocusDown(null);
    }

    public static FrameworkElement PredictFocus(Key key)
    {
      if (_currentScreen == null || _currentScreen.Visual == null)
        return null;
      return FindFirstFocusableElement((FrameworkElement) _currentScreen.Visual);
    }

    /// <summary>
    /// Predicts which FrameworkElement should get the focus when the specified <paramref name="key"/>
    /// was pressed.
    /// </summary>
    /// <param name="focusedFrameworkElement">The currently focused FrameworkElement.</param>
    /// <param name="key">The key to evaluate.</param>
    /// <returns>Framework element whcih gets focus when the specified <paramref name="key"/> was
    /// pressed.</returns>
    public static FrameworkElement PredictFocus(FrameworkElement focusedFrameworkElement, Key key)
    {
      if (key == Key.Up)
      {
        return PredictFocusUp(focusedFrameworkElement);
      }
      if (key == Key.Down)
      {
        return PredictFocusDown(focusedFrameworkElement);
      }
      if (key == Key.Left)
      {
        return PredictFocusLeft(focusedFrameworkElement);
      }
      if (key == Key.Right)
      {
        return PredictFocusRight(focusedFrameworkElement);
      }
      return null;
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is positioned above the specified
    /// <paramref name="focusedFrameworkElement"/>.
    /// </summary>
    /// <param name="focusedFrameworkElement">The currently focused FrameworkElement.</param>
    /// <returns>Framework element which will get the focus.</returns>
    private static FrameworkElement PredictFocusUp(FrameworkElement focusedFrameworkElement)
    {
      if (_currentScreen == null)
        return null;
      return _currentScreen.RootElement.PredictFocusUp(focusedFrameworkElement);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is positioned below the specified
    /// <paramref name="focusedFrameworkElement"/>.
    /// </summary>
    /// <param name="focusedFrameworkElement">The currently focused FrameworkElement.</param>
    /// <returns>Framework element which will get the focus.</returns>
    private static FrameworkElement PredictFocusDown(FrameworkElement focusedFrameworkElement)
    {
      if (_currentScreen == null)
        return null;
      return _currentScreen.RootElement.PredictFocusDown(focusedFrameworkElement);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is positioned left of the specified
    /// <paramref name="focusedFrameworkElement"/>.
    /// </summary>
    /// <param name="focusedFrameworkElement">The currently focused FrameworkElement.</param>
    /// <returns>Framework element which will get the focus.</returns>
    private static FrameworkElement PredictFocusLeft(FrameworkElement focusedFrameworkElement)
    {
      if (_currentScreen == null)
        return null;
      return _currentScreen.RootElement.PredictFocusLeft(focusedFrameworkElement);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is positioned right of the specified
    /// <paramref name="focusedFrameworkElement"/>.
    /// </summary>
    /// <param name="focusedFrameworkElement">The currently focused FrameworkElement.</param>
    /// <returns>Framework element which will get the focus.</returns>
    private static FrameworkElement PredictFocusRight(FrameworkElement focusedFrameworkElement)
    {
      if (_currentScreen == null)
        return null;
      return _currentScreen.RootElement.PredictFocusRight(focusedFrameworkElement);
    }
  }
}
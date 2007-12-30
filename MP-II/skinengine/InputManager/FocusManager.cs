#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using System.Collections.Generic;
using System.Text;

using SkinEngine.Controls.Visuals;
using MediaPortal.Core;
using MediaPortal.Core.InputManager;
using MediaPortal.Core.WindowManager;

namespace SkinEngine
{
  public class FocusManager
  {
    /// <summary>
    /// Predicts which FrameworkElement should get the focus
    /// </summary>
    /// <param name="focusedFrameworkElement">The current focused FrameworkElement.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public static FrameworkElement PredictFocus(FrameworkElement focusedFrameworkElement, ref Key key)
    {
      if (key == Key.Up)
      {
        return PredictFocusUp(focusedFrameworkElement, ref key, true);
      }
      if (key == Key.Down)
      {
        return PredictFocusDown(focusedFrameworkElement, ref key, true);
      }
      if (key == Key.Left)
      {
        return PredictFocusLeft(focusedFrameworkElement, ref key, true);
      }
      if (key == Key.Right)
      {
        return PredictFocusRight(focusedFrameworkElement, ref key, true);
      }

      return null;
    }

    /// <summary>
    /// returns the distance between 2 FrameworkElements
    /// </summary>
    /// <param name="c1">The c1.</param>
    /// <param name="c2">The c2.</param>
    /// <returns></returns>
    private static float Distance(FrameworkElement c1, FrameworkElement c2)
    {
      float y = Math.Abs(c1.ActualPosition.Y - c2.ActualPosition.Y);
      float x = Math.Abs(c1.ActualPosition.X - c2.ActualPosition.X);
      float distance = (float)Math.Sqrt(y * y + x * x);
      return distance;
    }

    /// <summary>
    /// returns the horizontal distance between 2 FrameworkElements
    /// </summary>
    /// <param name="c1">The c1.</param>
    /// <param name="c2">The c2.</param>
    /// <returns></returns>
    private static float DistanceX(FrameworkElement c1, FrameworkElement c2)
    {
      float distance = Math.Abs(c1.ActualPosition.X - c2.ActualPosition.X);
      return distance;
    }

    /// <summary>
    /// returns the vertical distance between 2 FrameworkElements
    /// </summary>
    /// <param name="c1">The c1.</param>
    /// <param name="c2">The c2.</param>
    /// <returns></returns>
    private static float DistanceY(FrameworkElement c1, FrameworkElement c2)
    {
      float distance = Math.Abs(c1.ActualPosition.Y - c2.ActualPosition.Y);
      return distance;
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position above this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The focused FrameworkElement.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    private static FrameworkElement PredictFocusUp(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      Window window = (Window)ServiceScope.Get<IWindowManager>().CurrentWindow;
      return window.RootElement.PredictFocusUp(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position below this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The focused FrameworkElement.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    private static FrameworkElement PredictFocusDown(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      Window window = (Window)ServiceScope.Get<IWindowManager>().CurrentWindow;
      return window.RootElement.PredictFocusDown(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is positioned left of this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The focused FrameworkElement.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    private static FrameworkElement PredictFocusLeft(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      Window window = (Window)ServiceScope.Get<IWindowManager>().CurrentWindow;
      return window.RootElement.PredictFocusLeft(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is positioned right of this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The focused FrameworkElement.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    private static FrameworkElement PredictFocusRight(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      Window window = (Window)ServiceScope.Get<IWindowManager>().CurrentWindow;
      return window.RootElement.PredictFocusRight(focusedFrameworkElement, ref key, strict);
    }
  }
}

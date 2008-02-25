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
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Core.InputManager;

using SkinEngine;
using SlimDX;
using SlimDX.Direct3D9;
using SkinEngine.Controls.Panels;

namespace SkinEngine.Controls.Visuals
{
  public class ScrollViewer : ContentControl
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ScrollViewer"/> class.
    /// </summary>
    public ScrollViewer()
    {
      Init();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScrollViewer"/> class.
    /// </summary>
    /// <param name="s">The s.</param>
    public ScrollViewer(ScrollViewer s)
      : base(s)
    {
      Init();
    }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    /// <returns></returns>
    public override object Clone()
    {
      return new ScrollViewer(this);
    }

    void Init()
    {
    }


    /// <summary>
    /// Handles keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public override void OnKeyPressed(ref Key key)
    {
      if (Content == null) return;
      UIElement element = (UIElement)Content;
      FrameworkElement focusedElement = element.FindFocusedItem() as FrameworkElement;
      if (focusedElement == null) return;

      if (key == MediaPortal.Core.InputManager.Key.PageDown)
      {
        if (OnPageDown(focusedElement.ActualPosition.X, focusedElement.ActualPosition.Y))
        {
          key = MediaPortal.Core.InputManager.Key.None;
          return;
        }
      }

      if (key == MediaPortal.Core.InputManager.Key.PageUp)
      {
        if (OnPageUp(focusedElement.ActualPosition.X, focusedElement.ActualPosition.Y))
        {
          key = MediaPortal.Core.InputManager.Key.None;
          return;
        }
      }

      if (key == MediaPortal.Core.InputManager.Key.Down)
      {
        if (OnDown(focusedElement.ActualPosition.X, focusedElement.ActualPosition.Y))
        {
          key = MediaPortal.Core.InputManager.Key.None;
          return;
        }
      }

      if (key == MediaPortal.Core.InputManager.Key.Up)
      {
        if (OnUp(focusedElement.ActualPosition.X, focusedElement.ActualPosition.Y))
        {
          key = MediaPortal.Core.InputManager.Key.None;
          return;
        }
      }

      if (key == MediaPortal.Core.InputManager.Key.Left)
      {
        if (OnLeft(focusedElement.ActualPosition.X, focusedElement.ActualPosition.Y))
        {
          key = MediaPortal.Core.InputManager.Key.None;
          return;
        }
      }

      if (key == MediaPortal.Core.InputManager.Key.Right)
      {
        if (OnRight(focusedElement.ActualPosition.X, focusedElement.ActualPosition.Y))
        {
          key = MediaPortal.Core.InputManager.Key.None;
          return;
        }
      }
      if (key == MediaPortal.Core.InputManager.Key.Home)
      {
        OnHome(focusedElement.ActualPosition.X, focusedElement.ActualPosition.Y);
        key = MediaPortal.Core.InputManager.Key.None;
        return;
      }
      if (key == MediaPortal.Core.InputManager.Key.End)
      {
        OnEnd(focusedElement.ActualPosition.X, focusedElement.ActualPosition.Y);
        key = MediaPortal.Core.InputManager.Key.None;
        return;
      }
      if (Char.IsLetterOrDigit(key.RawCode))
      {
        ScrollToItemWhichStartsWith(key.RawCode);
        key = MediaPortal.Core.InputManager.Key.None;
        return;
      }
      Content.OnKeyPressed(ref key);
    }

    void ScrollToItemWhichStartsWith(char key)
    {
      IScrollInfo info = GetScrollInfo();
      if (info == null) return;
      info.ScrollToItemWhichStartsWith(key);
    }
    void OnHome(float x, float y)
    {
      IScrollInfo info = GetScrollInfo();
      if (info == null) return;
      Panel element = (Panel)info;
      info.Home(new PointF(0, 0));
    }
    void OnEnd(float x, float y)
    {
      IScrollInfo info = GetScrollInfo();
      if (info == null) return;
      Panel element = (Panel)info;
      info.End(new PointF(0, 0));
    }

    bool OnPageDown(float x, float y)
    {
      IScrollInfo info = GetScrollInfo();
      if (info == null) return false;
      if (info.PageDown(new PointF(x, y)))
      {
        return true;
      }
      return false;
    }

    bool OnPageUp(float x, float y)
    {
      IScrollInfo info = GetScrollInfo();
      if (info == null) return false;
      if (info.PageUp(new PointF(x, y)))
      {
        return true;
      }
      return false;
    }

    bool OnLeft(float x, float y)
    {
      IScrollInfo info = GetScrollInfo();
      if (info == null) return false;
      FrameworkElement element = (FrameworkElement)info;
      if (x - info.LineWidth < element.ActualPosition.X)
      {
        if (info.LineLeft(new PointF(x, y)))
        {
          return true;
        }
      }
      return false;
    }

    bool OnRight(float x, float y)
    {
      IScrollInfo info = GetScrollInfo();
      if (info == null) return false;
      FrameworkElement element = (FrameworkElement)info;
      if (x + (info.LineWidth * 2) >= element.ActualPosition.X + element.ActualWidth)
      {
        if (info.LineDown(new PointF(x, y)))
        {
          return true;
        }
      }
      return false;
    }

    bool OnDown(float x, float y)
    {
      IScrollInfo info = GetScrollInfo();
      if (info == null) return false;
      FrameworkElement element = (FrameworkElement)info;
      if (y + (info.LineHeight) >= element.ActualPosition.Y + element.ActualHeight)
      {
        if (info.LineDown(new PointF(x, y)))
        {
          return true;
        }
      }
      return false;
    }

    bool OnUp(float x, float y)
    {
      IScrollInfo info = GetScrollInfo();
      if (info == null) return false;
      FrameworkElement element = (FrameworkElement)info;
      if (y <= element.ActualPosition.Y + info.LineHeight - 1)
      {
        if (info.LineUp(new PointF(x, y)))
        {
          return true;
        }
      }
      return false;
    }


    IScrollInfo GetScrollInfo()
    {
      IScrollInfo info = Content as IScrollInfo;
      if (info != null) return info;
      UIElement element = Content.FindItemsHost();
      info = element as IScrollInfo;
      return info;
    }
  }
}

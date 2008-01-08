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
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Core.InputManager;

using SkinEngine;


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
    /// Does the render.
    /// </summary>
    public override void DoRender()
    {
      GraphicsDevice.Device.RenderState.ScissorTestEnable = true;
      float x = (int)ActualPosition.X;
      float y = (int)ActualPosition.Y;
      float w = (int)ActualWidth;
      float h = (int)ActualHeight;
      x *= (((float)GraphicsDevice.Width) / ((float)SkinContext.Width));
      w *= (((float)GraphicsDevice.Width) / ((float)SkinContext.Width));

      y *= (((float)GraphicsDevice.Height) / ((float)SkinContext.Height));
      h *= (((float)GraphicsDevice.Height) / ((float)SkinContext.Height));
      if (x + w > (float)GraphicsDevice.Width)
      {
        w = (float)GraphicsDevice.Width - x;
      }
      if (y + h > (float)GraphicsDevice.Height)
      {
        h = (float)GraphicsDevice.Height - y;
      }
      GraphicsDevice.Device.ScissorRectangle = new System.Drawing.Rectangle((int)x, (int)y, (int)w, (int)h);
      if (Content != null)
      {
        Content.DoRender();
      }
      GraphicsDevice.Device.RenderState.ScissorTestEnable = false;
    }

    /// <summary>
    /// Measures the specified available size.
    /// </summary>
    /// <param name="availableSize">Size of the available.</param>
    public override void Measure(System.Drawing.Size availableSize)
    {
      _desiredSize = new System.Drawing.Size((int)Width, (int)Height);
      if (Width <= 0)
        _desiredSize.Width = (int)availableSize.Width - (int)(Margin.X + Margin.W);
      if (Height <= 0)
        _desiredSize.Height = (int)availableSize.Height - (int)(Margin.Y + Margin.Z);

      if (Content != null)
      {
        Content.Measure(_desiredSize);
        if (_desiredSize.Width < (availableSize.Width - (Margin.X + Margin.W)))
          _desiredSize.Width = Content.DesiredSize.Width;
        if (_desiredSize.Height < (availableSize.Height - (Margin.Y + Margin.Z)))
          _desiredSize.Height = Content.DesiredSize.Height;
      }
      if (Width > 0)
        _desiredSize.Width = (int)Width;
      if (Height > 0)
        _desiredSize.Height = (int)Height;

      if (LayoutTransform != null)
      {
        Microsoft.DirectX.Matrix mNew;
        LayoutTransform.GetTransform(out mNew);
        mNew.M41 = 0;
        mNew.M42 = 0;
        float w = _desiredSize.Width;
        float h = _desiredSize.Height;
        float w1 = w * mNew.M11 + h * mNew.M21;
        float h1 = w * mNew.M12 + h * mNew.M22;
        _transformedSize = new System.Drawing.Size((int)w1, (int)h1);

        _transformedSize.Width += (int)(Margin.X + Margin.W);
        _transformedSize.Height += (int)(Margin.Y + Margin.Z);
      }
      else
      {
        _desiredSize.Width += (int)(Margin.X + Margin.W);
        _desiredSize.Height += (int)(Margin.Y + Margin.Z);
        _transformedSize = _desiredSize;
      }


      _availableSize = new System.Drawing.Size(availableSize.Width, availableSize.Height);
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
      Content.OnKeyPressed(ref key);
    }

    void OnHome(float x, float y)
    {
      IScrollInfo info = Content as IScrollInfo;
      if (info == null) return;
      info.Home();
      OnMouseMove((float)(Content.ActualPosition.X), (float)(Content.ActualPosition.Y));
    }
    void OnEnd(float x, float y)
    {
      IScrollInfo info = Content as IScrollInfo;
      if (info == null) return;
      info.End();
      OnMouseMove((float)(Content.ActualPosition.X), (float)(Content.ActualPosition.Y + Content.ActualHeight - info.LineHeight));
    }

    bool OnPageDown(float x, float y)
    {
      IScrollInfo info = Content as IScrollInfo;
      if (info == null) return false;
      if (info.PageDown())
      {
        OnMouseMove(x, y);
        return true;
      }
      return false;
    }

    bool OnPageUp(float x, float y)
    {
      IScrollInfo info = Content as IScrollInfo;
      if (info == null) return false;
      if (info.PageUp())
      {
        OnMouseMove(x, y);
        return true;
      }
      return false;
    }

    bool OnLeft(float x, float y)
    {
      IScrollInfo info = Content as IScrollInfo;
      if (info == null) return false;
      if (x - info.LineWidth < Content.ActualPosition.X)
      {
        if (info.LineLeft())
        {
          OnMouseMove(x, y);
          return true;
        }
      }
      return false;
    }

    bool OnRight(float x, float y)
    {
      IScrollInfo info = Content as IScrollInfo;
      if (info == null) return false;
      if (x + (info.LineWidth * 2) >= Content.ActualPosition.X + Content.ActualWidth)
      {
        if (info.LineDown())
        {
          OnMouseMove(x, y);
          return true;
        }
      }
      return false;
    }

    bool OnDown(float x, float y)
    {
      IScrollInfo info = Content as IScrollInfo;
      if (info == null) return false;
      if (y + (info.LineHeight * 2) >= Content.ActualPosition.Y + Content.ActualHeight)
      {
        if (info.LineDown())
        {
          OnMouseMove(x, y);
          return true;
        }
      }
      return false;
    }

    bool OnUp(float x, float y)
    {
      IScrollInfo info = Content as IScrollInfo;
      if (info == null) return false;
      if (y <= Content.ActualPosition.Y)
      {
        if (info.LineUp())
        {
          OnMouseMove(x, y);
          return true;
        }
      }
      return false;
    }


  }
}

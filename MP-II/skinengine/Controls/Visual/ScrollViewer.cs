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
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Core.InputManager;

using SkinEngine;
using SlimDX;
using SlimDX.Direct3D9;

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
      //GraphicsDevice.Device.SetRenderState(RenderState.ScissorTestEnable, true);
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
      GraphicsDevice.Device.ScissorRect = new System.Drawing.Rectangle((int)x, (int)y, (int)w, (int)h);
      if (Content != null)
      {
        Content.DoRender();
      }
      GraphicsDevice.Device.SetRenderState(RenderState.ScissorTestEnable, false);
    }

    /// <summary>
    /// Measures the specified available size.
    /// </summary>
    /// <param name="availableSize">Size of the available.</param>
    public override void Measure(System.Drawing.SizeF availableSize)
    {
      float marginWidth = (float)((Margin.X + Margin.W) * SkinContext.Zoom.Width);
      float marginHeight = (float)((Margin.Y + Margin.Z) * SkinContext.Zoom.Height);
      _desiredSize = new System.Drawing.SizeF((float)Width * SkinContext.Zoom.Width, (float)Height * SkinContext.Zoom.Height);
      if (Width <= 0)
        _desiredSize.Width = (float)(availableSize.Width - marginWidth);
      if (Height <= 0)
        _desiredSize.Height = (float)(availableSize.Height - marginHeight);

      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      if (Content != null)
      {
        Content.Measure(_desiredSize);
        if (_desiredSize.Width < (availableSize.Width - (marginWidth)))
          _desiredSize.Width = Content.DesiredSize.Width;
        if (_desiredSize.Height < (availableSize.Height - (marginHeight)))
          _desiredSize.Height = Content.DesiredSize.Height;
      }
      if (Width > 0)
        _desiredSize.Width = (float)Width * SkinContext.Zoom.Width;
      if (Height > 0)
        _desiredSize.Height = (float)Height * SkinContext.Zoom.Height;

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);
      _desiredSize.Width += marginWidth;
      _desiredSize.Height += marginHeight;
      _originalSize = _desiredSize;



      _availableSize = new System.Drawing.SizeF(availableSize.Width, availableSize.Height);
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

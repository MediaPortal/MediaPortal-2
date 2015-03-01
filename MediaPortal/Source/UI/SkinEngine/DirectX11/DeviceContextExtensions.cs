#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using System;
using MediaPortal.UI.SkinEngine.Controls;
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace MediaPortal.UI.SkinEngine.DirectX11
{
  public static class DeviceContextExtensions
  {
    public static void FillGeometry(this DeviceContext context, SharpDX.Direct2D1.Geometry geometry, Brush brush, RenderContext renderContext)
    {
      DrawAdjustedToRenderContext(brush, renderContext, () => GraphicsDevice11.Instance.Context2D1.FillGeometry(geometry, brush));
    }

    public static void FillGeometry(this DeviceContext context, SharpDX.Direct2D1.Geometry geometry, Brush brush, Brush opacityBrush, RenderContext renderContext)
    {
      if (opacityBrush == null)
        FillGeometry(context, geometry, brush, renderContext);
      else
        DrawAdjustedToRenderContext(brush, renderContext, () => GraphicsDevice11.Instance.Context2D1.FillGeometry(geometry, brush, opacityBrush));
    }

    public static void FillGeometry(this DeviceContext context, SharpDX.Direct2D1.Geometry geometry, Controls.Brushes.Brush brush, RenderContext renderContext)
    {
      IRenderBrush renderBrush = brush as IRenderBrush;
      if (renderBrush != null)
      {
        if (!renderBrush.RenderContent(renderContext))
          return;
      }
      FillGeometry(context, geometry, brush.Brush2D, renderContext);
    }

    public static void FillGeometry(this DeviceContext context, SharpDX.Direct2D1.Geometry geometry, Brush brush, Controls.Brushes.Brush opacityBrush, RenderContext renderContext)
    {
      if (opacityBrush == null || !opacityBrush.TryAllocate())
        FillGeometry(context, geometry, brush, renderContext);
      else
      {
        var opacityBrush2D = opacityBrush.Brush2D;
        if (opacityBrush2D is SolidColorBrush)
        {
          // SolidColorBrushes won't work? So only use the Alpha value
          brush.Opacity *= ((SolidColorBrush)opacityBrush2D).Color.Alpha;
          GraphicsDevice11.Instance.Context2D1.FillGeometry(geometry, brush);
        }
        else
        {
          // Other kinds of OpacityMasks are handled as Layers
          GraphicsDevice11.Instance.Context2D1.FillGeometry(geometry, brush);
        }
      }
    }

    public static void DrawGeometry(this DeviceContext context, SharpDX.Direct2D1.Geometry geometry, Brush brush, float strokeWidth, RenderContext renderContext)
    {
      DrawAdjustedToRenderContext(brush, renderContext, () => GraphicsDevice11.Instance.Context2D1.DrawGeometry(geometry, brush, strokeWidth));
    }

    public static void DrawGeometry(this DeviceContext context, SharpDX.Direct2D1.Geometry geometry, Brush brush, float strokeWidth, StrokeStyle strokeStyle, RenderContext renderContext)
    {
      DrawAdjustedToRenderContext(brush, renderContext, () => GraphicsDevice11.Instance.Context2D1.DrawGeometry(geometry, brush, strokeWidth, strokeStyle));
    }

    public static void DrawTextLayout(this DeviceContext context, Vector2 origin, TextLayout textLayout, Brush brush, RenderContext renderContext)
    {
      DrawAdjustedToRenderContext(brush, renderContext, () => GraphicsDevice11.Instance.Context2D1.DrawTextLayout(origin, textLayout, brush, DrawTextOptions.NoSnap | DrawTextOptions.Clip));
    }

    public static void DrawBitmap(this DeviceContext context, Bitmap bitmap, RectangleF destinationRectangle, RectangleF textureClip, float opacity, RenderContext renderContext)
    {
      AdjustClipRect(bitmap, ref textureClip);
      DrawAdjustedToRenderContext(null, renderContext,
        () => GraphicsDevice11.Instance.Context2D1.DrawBitmap(bitmap, destinationRectangle, opacity, BitmapInterpolationMode.Linear, textureClip));
    }

    public static void DrawImage(this DeviceContext context, Image image, RenderContext renderContext)
    {
      DrawAdjustedToRenderContext(null, renderContext, () => GraphicsDevice11.Instance.Context2D1.DrawImage(image));
    }

    public static void DrawImage(this DeviceContext context, Effect effect, Vector2 targetOffset, RenderContext renderContext)
    {
      DrawAdjustedToRenderContext(null, renderContext, () => GraphicsDevice11.Instance.Context2D1.DrawImage(effect, targetOffset, GraphicsDevice11.Instance.ImageInterpolationMode));
    }

    /// <summary>
    /// Translates relative rect to real size.
    /// </summary>
    /// <param name="bitmap">Bitmap</param>
    /// <param name="textureClip">Relative rect</param>
    private static void AdjustClipRect(Bitmap bitmap, ref RectangleF textureClip)
    {
      int w = bitmap.PixelSize.Width;
      int h = bitmap.PixelSize.Height;
      textureClip.Width *= w;
      textureClip.Height *= h;
      textureClip.Left *= textureClip.Width;
      textureClip.Top *= textureClip.Height;
    }

    public static void DrawAdjustedToRenderContext(Brush brush, RenderContext renderContext, Action renderCall)
    {
      float oldOpacity = 1f;
      if (brush != null)
      {
        oldOpacity = brush.Opacity;
        brush.Opacity *= (float)renderContext.Opacity;
      }
      var oldTransform = GraphicsDevice11.Instance.Context2D1.Transform;


      // Note: no Brush transformation here. The Brush has to be initialized to match control boundaries

      GraphicsDevice11.Instance.Context2D1.Transform = renderContext.Transform;

      renderCall();

      // Only for debugging: if there were errors they are only visible in EndDraw / Flush. This call is bad for performance.
      // GraphicsDevice11.Instance.Context2D1.Flush();

      GraphicsDevice11.Instance.Context2D1.Transform = oldTransform;
      if (brush != null)
        brush.Opacity = oldOpacity;
    }

    /// <summary>
    /// Renders the <paramref name="brush"/>'s content if required. This is needed for brushes with dynamic content, like the <see cref="Controls.Brushes.VideoBrush"/>.
    /// </summary>
    /// <param name="brush"></param>
    /// <param name="localRenderContext"></param>
    /// <returns></returns>
    public static bool RenderBrush(this Controls.Brushes.Brush brush, RenderContext localRenderContext)
    {
      if (brush == null)
        return false;

      IRenderBrush renderBrush = brush as IRenderBrush;
      if (renderBrush == null)
        return true;

      return renderBrush.RenderContent(localRenderContext);
    }
  }
}

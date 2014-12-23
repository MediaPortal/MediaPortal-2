using System;
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

    public static void FillGeometry(this DeviceContext context, SharpDX.Direct2D1.Geometry geometry, Brush brush, Controls.Brushes.Brush opacityBrush, RenderContext renderContext)
    {
      if (opacityBrush == null || !opacityBrush.TryAllocate())
        FillGeometry(context, geometry, brush, renderContext);
      else
        DrawAdjustedToRenderContext(brush, renderContext, () =>
        {
          var opacityBrush2D = opacityBrush.Brush2D;
          if (opacityBrush2D is SolidColorBrush)
          {
            // SolidColorBrushes won't work? So only use the Alpha value
            brush.Opacity *= ((SolidColorBrush)opacityBrush2D).Color.Alpha;
            GraphicsDevice11.Instance.Context2D1.FillGeometry(geometry, brush);
          }
          else if (opacityBrush2D is LinearGradientBrush || opacityBrush2D is RadialGradientBrush || opacityBrush2D is BitmapBrush)
          {
            GraphicsDevice11.Instance.Context2D1.FillGeometry(geometry, brush);
          }
          else
            GraphicsDevice11.Instance.Context2D1.FillGeometry(geometry, brush, opacityBrush.Brush2D);
          // Only for debugging: if there were errors they are only visible in EndDraw / Flush
          //GraphicsDevice11.Instance.Context2D1.Flush();
        });
    }

    public static void DrawGeometry(this DeviceContext context, SharpDX.Direct2D1.Geometry geometry, Brush brush, float strokeWidth, RenderContext renderContext)
    {
      DrawAdjustedToRenderContext(brush, renderContext, () => GraphicsDevice11.Instance.Context2D1.DrawGeometry(geometry, brush, strokeWidth));
    }

    public static void DrawTextLayout(this DeviceContext context, Vector2 origin, TextLayout textLayout, Brush brush, RenderContext renderContext)
    {
      DrawAdjustedToRenderContext(brush, renderContext, () => GraphicsDevice11.Instance.Context2D1.DrawTextLayout(origin, textLayout, brush));
    }

    public static void DrawBitmap(this DeviceContext context, Bitmap bitmap, RectangleF destinationRectangle, float opacity, BitmapInterpolationMode interpolationMode, RenderContext renderContext)
    {
      DrawAdjustedToRenderContext(null, renderContext, () => GraphicsDevice11.Instance.Context2D1.DrawBitmap(bitmap, destinationRectangle, opacity, interpolationMode));
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

      GraphicsDevice11.Instance.Context2D1.Transform = oldTransform;
      if (brush != null)
      {
        brush.Opacity = oldOpacity;
      }
      ;
    }
  }
}

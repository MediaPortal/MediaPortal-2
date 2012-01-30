using System.Drawing;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Rendering;
using SlimDX;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  public abstract class Effect : DependencyObject
  {
    protected RectangleF _vertsBounds;
    protected EffectAsset _effect;
    protected readonly RectangleF CROP_FULLSIZE = new RectangleF(0, 0, 1, 1);

    public bool BeginRender(Texture texture, RenderContext renderContext)
    {
      if (_vertsBounds.IsEmpty)
        return false;
      return BeginRenderEffectOverride(texture, renderContext);
    }

    protected abstract bool BeginRenderEffectOverride(Texture texture, RenderContext renderContext);

    public abstract void EndRender();

    protected bool UpdateBounds(ref PositionColoredTextured[] verts)
    {
      if (verts == null)
      {
        _vertsBounds = RectangleF.Empty;
        return false;
      }
      float minx = float.MaxValue;
      float miny = float.MaxValue;
      float maxx = 0;
      float maxy = 0;
      foreach (PositionColoredTextured vert in verts)
      {
        if (vert.X < minx) minx = vert.X;
        if (vert.Y < miny) miny = vert.Y;

        if (vert.X > maxx) maxx = vert.X;
        if (vert.Y > maxy) maxy = vert.Y;
      }
      _vertsBounds = new RectangleF(minx, miny, maxx - minx, maxy - miny);
      return true;
    }

    public virtual void SetupEffect(FrameworkElement parent, ref PositionColoredTextured[] verts, float zOrder, bool adaptVertsToBrushTexture)
    {
      if (!UpdateBounds(ref verts))
        return;
      float w = _vertsBounds.Width;
      float h = _vertsBounds.Height;
      float xoff = _vertsBounds.X;
      float yoff = _vertsBounds.Y;
      if (adaptVertsToBrushTexture)
        for (int i = 0; i < verts.Length; i++)
        {
          PositionColoredTextured vert = verts[i];
          float x = vert.X;
          float u = x - xoff;
          u /= w;

          float y = vert.Y;
          float v = y - yoff;
          v /= h;

          if (u < 0) u = 0;
          if (u > 1) u = 1;
          if (v < 0) v = 0;
          if (v > 1) v = 1;
          unchecked
          {
            Color4 color = ColorConverter.FromColor(Color.White);
            vert.Color = color.ToArgb();
          }
          vert.Tu1 = u;
          vert.Tv1 = v;
          vert.Z = zOrder;
          verts[i] = vert;
        }
    }

  }
}

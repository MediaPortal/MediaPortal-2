#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX;
using SharpDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  /// <summary>
  /// Provides a base class for all bitmap effects.
  /// </summary>
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
            vert.Color = color.ToBgra();
          }
          vert.Tu1 = u;
          vert.Tv1 = v;
          vert.Z = zOrder;
          verts[i] = vert;
        }
    }

  }
}

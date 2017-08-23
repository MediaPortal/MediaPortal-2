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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.UI.SkinEngine.ContentManagement;
using SharpDX;
using SharpDX.Direct3D9;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  /// <summary>
  /// Builds and holds a texture which is used for all gradient brushes. The created texture contains a gradient
  /// specified by a gradient stop collection.
  /// </summary>
  public class GradientBrushTexture : ITextureAsset, IDisposable
  {
    class GradientStopData
    {
      private readonly Color _color;
      private readonly double _offset;

      private GradientStopData(Color color, double offset)
      {
        _color = color;
        _offset = offset;
      }

      public static GradientStopData FromGradientStop(GradientStop origin)
      {
        return new GradientStopData(origin.Color, origin.Offset);
      }

      public Color Color
      {
        get { return _color; }
      }

      public double Offset
      {
        get { return _offset; }
      }
    }

    RenderTextureAsset _texture;
    readonly IList<GradientStopData> _stops;
    readonly string _name;
    static int _assetId = 0;

    private const int GRADIENT_TEXTURE_WIDTH = 256;
    private const int GRADIENT_TEXTURE_HEIGHT = 2;

    public GradientBrushTexture(GradientStopCollection stops)
    {
      _assetId++;
      _stops = stops.OrderedGradientStopList.Select(GradientStopData.FromGradientStop).ToList();
      _name = String.Format("GradientBrushTexture#{0}", _assetId);
      Allocate();
    }

    public void Dispose()
    {
      _texture = null;
    }

    public void Allocate()
    {
      if (_texture == null)
        _texture = ContentManager.Instance.GetRenderTexture(_name);
      if (_texture.IsAllocated)
        return;
      _texture.AllocateDynamic(GRADIENT_TEXTURE_WIDTH, GRADIENT_TEXTURE_HEIGHT);
      CreateGradient();
    }

    public bool IsSame(GradientStopCollection stops)
    {
      IList<GradientStop> compareStops = stops.OrderedGradientStopList;
      if (compareStops.Count != _stops.Count)
        return false;
      for (int i = 0; i < _stops.Count; i++)
      {
        if (_stops[i].Offset != compareStops[i].Offset)
          return false;
        if (!_stops[i].Color.Equals(compareStops[i].Color))
          return false;
      }
      return true;
    }

    public Texture Texture
    {
      get
      {
        if (!IsAllocated)
          Allocate();
        return _texture.Texture;
      }
    }

    void CreateGradient()
    {
      byte[] data = new byte[4 * GRADIENT_TEXTURE_WIDTH * GRADIENT_TEXTURE_HEIGHT];

      for (int i = 0; i < _stops.Count - 1; i++)
        CreatePartialGradient(data, _stops[i], _stops[i + 1]);

      DataStream dataStream;
      _texture.Surface0.LockRectangle(LockFlags.None, out dataStream);
      using (dataStream)
      {
        dataStream.Write(data, 0, 4 * GRADIENT_TEXTURE_WIDTH * GRADIENT_TEXTURE_HEIGHT);
        _texture.Surface0.UnlockRectangle();
      }
    }

    public bool IsAllocated
    {
      get { return _texture != null && _texture.IsAllocated; }
    }

    public void Free(bool force)
    {
      _texture = null;
    }

    private static void CreatePartialGradient(byte[] data, GradientStopData stopBegin, GradientStopData stopEnd)
    {
      const float width = GRADIENT_TEXTURE_WIDTH;
      const int offY = GRADIENT_TEXTURE_WIDTH * 4;

      Color4 colorStart = ColorConverter.FromColor(stopBegin.Color);
      Color4 colorEnd = ColorConverter.FromColor(stopEnd.Color);
      int offsetStart = (int) (stopBegin.Offset * width);
      int offsetEnd = (int) (stopEnd.Offset * width);

      int clampedStart = Math.Max(0, offsetStart);
      int clampedEnd = Math.Min(GRADIENT_TEXTURE_WIDTH, offsetEnd);

      float distance = offsetEnd - offsetStart;
      for (int x = clampedStart; x < clampedEnd; ++x)
      {
        float step = (x - offsetStart) / distance;
        float r = step * (colorEnd.Red - colorStart.Red);
        r += colorStart.Red;

        float g = step * (colorEnd.Green - colorStart.Green);
        g += colorStart.Green;

        float b = step * (colorEnd.Blue - colorStart.Blue);
        b += colorStart.Blue;

        float a = step * (colorEnd.Alpha - colorStart.Alpha);
        a += colorStart.Alpha;

        a *= 255;
        r *= 255;
        g *= 255;
        b *= 255;

        int offx = x * 4;
        data[offx] = (byte) b;
        data[offx + 1] = (byte) g;
        data[offx + 2] = (byte) r;
        data[offx + 3] = (byte) a;

        data[offY + offx] = (byte) b;
        data[offY + offx + 1] = (byte) g;
        data[offY + offx + 2] = (byte) r;
        data[offY + offx + 3] = (byte) a;
      }
    }
  }
}

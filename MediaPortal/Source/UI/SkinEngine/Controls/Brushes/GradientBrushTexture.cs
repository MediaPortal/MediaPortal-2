#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.UI.SkinEngine.ContentManagement;
using SlimDX;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  /// <summary>
  /// Builds and holds a texture which is used for all gradient brushes. The created texture contains a gradient
  /// specified by a gradient stop collection.
  /// </summary>
  public class GradientBrushTexture : ITextureAsset, IDisposable
  {
    RenderTextureAsset _texture;
    readonly GradientStopCollection _stops;
    readonly string _name;
    static int _assetId = 0;

    public GradientBrushTexture(GradientStopCollection stops)
    {
      _assetId++;
      _stops = stops;
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
        _texture = ServiceRegistration.Get<ContentManager>().GetRenderTexture(_name);
      if (!_texture.IsAllocated) 
      {
        _texture.AllocateDynamic(256, 2);
        CreateGradient();
      }
    }

    public bool IsSame(GradientStopCollection stops)
    {
      if (stops.Count != _stops.Count)
        return false;
      IList<GradientStop> thisStops = _stops.OrderedGradientStopList;
      IList<GradientStop> thatStops = stops.OrderedGradientStopList;
      for (int i = 0; i < thisStops.Count; i++)
      {
        if (thisStops[i].Offset != thatStops[i].Offset)
          return false;
        if (!thisStops[i].Color.Equals(thatStops[i].Color))
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
      float width = 256.0f;
      byte[] data = new byte[4 * 512];
      int offY = 256 * 4;
      IList<GradientStop> orderedStops = _stops.OrderedGradientStopList;
      for (int i = 0; i < orderedStops.Count - 1; i++)
      {
        GradientStop stopBegin = orderedStops[i];
        GradientStop stopEnd = orderedStops[i + 1];
        Color4 colorStart = ColorConverter.FromColor(stopBegin.Color);
        Color4 colorEnd = ColorConverter.FromColor(stopEnd.Color);
        int offsetStart = (int)( stopBegin.Offset * width);
        int offsetEnd = (int) (stopEnd.Offset * width);

        float distance = offsetEnd - offsetStart;
        for (int x = offsetStart; x < offsetEnd; ++x)
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
      DataRectangle rect = _texture.Surface0.LockRectangle(LockFlags.None);
      rect.Data.Write(data, 0, 4 * 512);
      _texture.Surface0.UnlockRectangle();
      rect.Data.Dispose();
    }

    public bool IsAllocated
    {
      get { return _texture != null && _texture.IsAllocated; }
    }

    public void Free(bool force)
    {
      _texture = null;
    }
  }
}

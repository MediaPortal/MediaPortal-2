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
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Core.Properties;
using SkinEngine.DirectX;
using SkinEngine.Controls.Visuals;

using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;

namespace SkinEngine.Controls.Brushes
{

  public enum TileMode
  {
    //no tiling
    None,
    //content is tiled
    Tile,
    //content is tiled and flipped around x-axis
    FlipX,
    //content is tiled and flipped around y-axis
    FlipY
  };


  /// <summary>
  /// 
  /// </summary>
  public class TileBrush : Brush
  {
    Property _alignmentXProperty;
    Property _alignmentYProperty;
    Property _stretchProperty;
    Property _viewPortProperty;
    Property _tileModeProperty;

    public TileBrush()
    {
      Init();
    }

    public TileBrush(TileBrush b)
      : base(b)
    {
      Init();
      AlignmentX = b.AlignmentX;
      AlignmentY = b.AlignmentY;
      Stretch = b.Stretch;
      Tile = b.Tile;
      ViewPort = b.ViewPort;
    }

    void Init()
    {
      _alignmentXProperty = new Property(AlignmentX.Center);
      _alignmentYProperty = new Property(AlignmentY.Center);
      _stretchProperty = new Property(Stretch.Fill);
      _tileModeProperty = new Property(TileMode.None);
      _viewPortProperty = new Property(new Vector4(0, 0, 1, 1));

      _alignmentXProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _alignmentYProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _stretchProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _tileModeProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _viewPortProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }

    /// <summary>
    /// Gets or sets the alignment X property.
    /// </summary>
    /// <value>The alignment X property.</value>
    public Property AlignmentXProperty
    {
      get
      {
        return _alignmentXProperty;
      }
      set
      {
        _alignmentXProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the alignment X.
    /// </summary>
    /// <value>The alignment X.</value>
    public AlignmentX AlignmentX
    {
      get
      {
        return (AlignmentX)_alignmentXProperty.GetValue();
      }
      set
      {
        _alignmentXProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the alignment Y property.
    /// </summary>
    /// <value>The alignment Y property.</value>
    public Property AlignmentYProperty
    {
      get
      {
        return _alignmentYProperty;
      }
      set
      {
        _alignmentYProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the alignment Y.
    /// </summary>
    /// <value>The alignment Y.</value>
    public AlignmentY AlignmentY
    {
      get
      {
        return (AlignmentY)_alignmentYProperty.GetValue();
      }
      set
      {
        _alignmentYProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the stretch property.
    /// </summary>
    /// <value>The stretch property.</value>
    public Property StretchProperty
    {
      get
      {
        return _stretchProperty;
      }
      set
      {
        _stretchProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the stretch.
    /// </summary>
    /// <value>The stretch.</value>
    public Stretch Stretch
    {
      get
      {
        return (Stretch)_stretchProperty.GetValue();
      }
      set
      {
        _stretchProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the view port property.
    /// </summary>
    /// <value>The view port property.</value>
    public Property ViewPortProperty
    {
      get
      {
        return _viewPortProperty;
      }
      set
      {
        _viewPortProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the view port.
    /// </summary>
    /// <value>The view port.</value>
    public Vector4 ViewPort
    {
      get
      {
        return (Vector4)_viewPortProperty.GetValue();
      }
      set
      {
        _viewPortProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the tile property.
    /// </summary>
    /// <value>The tile property.</value>
    public Property TileProperty
    {
      get
      {
        return _tileModeProperty;
      }
      set
      {
        _tileModeProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the tile.
    /// </summary>
    /// <value>The tile.</value>
    public TileMode Tile
    {
      get
      {
        return (TileMode)_tileModeProperty.GetValue();
      }
      set
      {
        _tileModeProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Setups the brush.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="verts"></param>
    public override void SetupBrush(FrameworkElement element, ref PositionColored2Textured[] verts)
    {
      // todo here:
      ///   - stretchmode
      ///   - tilemode  : none,tile,flipx,flipy
      ///   - alignmentx/alignmenty
      ///   - viewport  : dimensions of a single tile
      ///   
      switch (Stretch)
      {
        case Stretch.None:
          //center, original size
          break;

        case Stretch.Uniform:
          //center, keep aspect ratio and show borders
          break;

        case Stretch.UniformToFill:
          //keep aspect ratio, zoom in to avoid borders
          break;

        case Stretch.Fill:
          //stretch to fill
          break;
      }


      for (int i = 0; i < verts.Length; ++i)
      {
        float u, v;
        float x1, y1;
        y1 = (float)(verts[i].Y - element.ActualPosition.Y);
        v = y1 / (float)(element.ActualHeight * ViewPort.W);
        v += ViewPort.Y;

        x1 = (float)(verts[i].X - element.ActualPosition.X);
        u = x1 / (float)(element.ActualWidth * ViewPort.Z);
        u += ViewPort.X;
        Scale(ref u, ref v);
        unchecked
        {
          ColorValue color = ColorConverter.FromColor(System.Drawing.Color.White);
          color.Alpha *= (float)Opacity;
          verts[i].Color = color.ToArgb();
        }
        verts[i].Tu1 = u;
        verts[i].Tv1 = v;
        verts[i].Tu2 = 0;
        verts[i].Tv2 = 0;
      }
    }

    /// <summary>
    /// Scales the specified u.
    /// </summary>
    /// <param name="u">The u.</param>
    /// <param name="v">The v.</param>
    protected virtual void Scale(ref float u, ref float v)
    {
    }

    /// <summary>
    /// Gets the brush dimensions.
    /// </summary>
    /// <value>The brush dimensions.</value>
    protected virtual Vector2 BrushDimensions
    {
      get
      {
        return new Vector2(1, 1);
      }
    }
  }
}

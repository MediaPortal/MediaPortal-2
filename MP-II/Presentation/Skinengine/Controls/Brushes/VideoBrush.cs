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
using System.Drawing;
using MediaPortal.Core;
using MediaPortal.Presentation.Properties;

using Presentation.SkinEngine.Controls.Transforms;
using Presentation.SkinEngine.Controls.Visuals;
using Presentation.SkinEngine.Effects;
using Presentation.SkinEngine;
using Presentation.SkinEngine.DirectX;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using MediaPortal.Presentation.Players;
using Rectangle = System.Drawing.Rectangle;

namespace Presentation.SkinEngine.Controls.Brushes
{
  public class VideoBrush : Brush
  {
    Property _streamProperty;
    EffectAsset _effect;
    Size _videoSize;
    Size _videoAspectRatio;
    string _previousGeometry;
    PositionColored2Textured[] _verts;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoBrush"/> class.
    /// </summary>
    public VideoBrush()
    {
      Init();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoBrush"/> class.
    /// </summary>
    /// <param name="videoBrush">The video brush.</param>
    public VideoBrush(VideoBrush videoBrush)
      : base(videoBrush)
    {
      Init();
      Stream = videoBrush.Stream;
    }

    /// <summary>
    /// Inits this instance.
    /// </summary>
    void Init()
    {
      _streamProperty = new Property((int)0);
      _effect = ContentManager.GetEffect("normal");
    }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    /// <returns></returns>
    public override object Clone()
    {
      return new VideoBrush(this);
    }

    /// <summary>
    /// Gets or sets the stream property.
    /// </summary>
    /// <value>The stream property.</value>
    public Property StreamProperty
    {
      get
      {
        return _streamProperty;
      }
      set
      {
        _streamProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the video stream number
    /// </summary>
    /// <value>The video stream number.</value>
    public int Stream
    {
      get
      {
        return (int)_streamProperty.GetValue();
      }
      set
      {
        _streamProperty.SetValue(value);
      }
    }
    /// <summary>
    /// Setups the brush.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="verts"></param>
    public override void SetupBrush(FrameworkElement element, ref PositionColored2Textured[] verts)
    {
      UpdateBounds(element, ref verts);
      base.SetupBrush(element, ref verts);
      _verts = verts;
      _videoSize = new Size(0, 0);
      _videoAspectRatio = new Size(0, 0);
    }


    void UpdateVertexBuffer(IPlayer player, VertexBuffer vertexBuffer)
    {
      Size size = player.VideoSize;
      Size aspectRatio = player.VideoAspectRatio;
      if (size == _videoSize && aspectRatio == _videoAspectRatio)
      {
        if (SkinContext.Geometry.Current.Name == _previousGeometry)
          return;
      }

      _videoSize = size;
      _videoAspectRatio = aspectRatio;
      _previousGeometry = SkinContext.Geometry.Current.Name;
      Rectangle sourceRect;
      Rectangle destinationRect;
      SkinContext.Geometry.ImageWidth = (int)_videoSize.Width;
      SkinContext.Geometry.ImageHeight = (int)_videoSize.Height;
      SkinContext.Geometry.ScreenWidth = (int)_bounds.Width;
      SkinContext.Geometry.ScreenHeight = (int)_bounds.Height;
      SkinContext.Geometry.GetWindow(aspectRatio.Width, aspectRatio.Height,
                                     out sourceRect,
                                     out destinationRect,
                                     SkinContext.CropSettings);
      player.MovieRectangle = destinationRect;
      string shaderName = SkinContext.Geometry.Current.Shader;
      if (shaderName != "")
      {
        _effect = ContentManager.GetEffect(shaderName);
      }
      else
      {
        _effect = ContentManager.GetEffect("normal");
      }

      float minU = ((float)(sourceRect.X)) / ((float)_videoSize.Width);
      float minV = ((float)(sourceRect.Y)) / ((float)_videoSize.Height);
      float maxU = ((float)(sourceRect.Width)) / ((float)_videoSize.Width);
      float maxV = ((float)(sourceRect.Height)) / ((float)_videoSize.Height);

      float minX = ((float)(destinationRect.X)) / ((float)_bounds.Width);
      float minY = ((float)(destinationRect.Y)) / ((float)_bounds.Height);

      float maxX = ((float)(destinationRect.Width)) / ((float)_bounds.Width);
      float maxY = ((float)(destinationRect.Height)) / ((float)_bounds.Height);

      float diffU = maxU - minU;
      float diffV = maxV - minV;
      PositionColored2Textured[] verts = new PositionColored2Textured[_verts.Length];
      for (int i = 0; i < _verts.Length; ++i)
      {
        float x = ((_verts[i].X - _minPosition.X) / (_bounds.Width)) * maxX + minX;
        float y = ((_verts[i].Y - _minPosition.Y) / (_bounds.Height)) * maxY + minY;
        verts[i].X = (x * _bounds.Width) + _minPosition.X;
        verts[i].Y = (y * _bounds.Height) + _minPosition.Y;

        float u = _verts[i].Tu1 * diffU + minU;
        float v = _verts[i].Tv1 * diffV + minV;
        verts[i].Tu1 = u;
        verts[i].Tv1 = v;
        verts[i].Color = _verts[i].Color;
        verts[i].Z = SkinContext.Z;
      }
      PositionColored2Textured.Set(vertexBuffer, ref verts);
    }

    /// <summary>
    /// Begins the render.
    /// </summary>
    /// <param name="vertexBuffer">The vertex buffer.</param>
    /// <param name="primitiveCount"></param>
    /// <param name="primitiveType"></param>
    /// <returns></returns>
    public override bool BeginRender(VertexBuffer vertexBuffer, int primitiveCount, PrimitiveType primitiveType)
    {

      PlayerCollection players = ServiceScope.Get<PlayerCollection>();
      if (players.Count <= Stream) return false;

      if (Transform != null)
      {
        ExtendedMatrix mTrans;
        Transform.GetTransform(out mTrans);
        SkinContext.AddTransform(mTrans);
      }

      IPlayer player = players[Stream];
      UpdateVertexBuffer(player, vertexBuffer);
      player.BeginRender(_effect);
      return true;
    }

    /// <summary>
    /// Ends the render.
    /// </summary>
    public override void EndRender()
    {
      PlayerCollection players = ServiceScope.Get<PlayerCollection>();
      players = ServiceScope.Get<PlayerCollection>();
      if (players.Count <= Stream) return;

      IPlayer player = players[Stream];
      player.EndRender(_effect);
      if (Transform != null)
      {
        SkinContext.RemoveTransform();
      }
    }
  }
}

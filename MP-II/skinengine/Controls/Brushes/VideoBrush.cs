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

using SkinEngine.Controls.Transforms;
using SkinEngine.Controls.Visuals;
using SkinEngine.Effects;
using SkinEngine;
using SkinEngine.DirectX;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using MediaPortal.Core.Players;

namespace SkinEngine.Controls.Brushes
{
  public class VideoBrush : Brush
  {
    Property _streamProperty;
    EffectAsset _effect;
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
      _effect.StartRender(player.Texture as Texture);
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
      _effect.EndRender();
      if (Transform != null)
      {
        SkinContext.RemoveTransform();
      }
    }
  }
}

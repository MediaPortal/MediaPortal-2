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
using System.Drawing;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Effects;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.Rendering;
using SlimDX.Direct3D9;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public class VideoBrush : Brush
  {
    #region Private fields

    AbstractProperty _streamProperty;
    EffectAsset _effect;
    Size _videoSize;
    Size _videoAspectRatio;
    float _pixelAspectRatio;
    IGeometry _currentGeometry;
    PositionColored2Textured[] _verts;
    ISlimDXVideoPlayer _renderPlayer = null;

    #endregion

    #region Ctor

    public VideoBrush()
    {
      Init();
    }

    void Init()
    {
      _streamProperty = new SProperty(typeof(int), 0);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      VideoBrush b = (VideoBrush) source;
      Stream = b.Stream;
    }

    #endregion

    #region Public properties

    public AbstractProperty StreamProperty
    {
      get { return _streamProperty; }
    }

    /// <summary>
    /// Gets or sets the video stream number.
    /// </summary>
    public int Stream
    {
      get { return (int) _streamProperty.GetValue(); }
      set { _streamProperty.SetValue(value); }
    }

    #endregion

    public override void SetupBrush(FrameworkElement parent, ref PositionColored2Textured[] verts, float zOrder, bool adaptVertsToBrushTexture)
    {
      base.SetupBrush(parent, ref verts, zOrder, adaptVertsToBrushTexture);
      _effect = ContentManager.GetEffect("normal");
      _verts = verts;
      _videoSize = new Size(0, 0);
      _videoAspectRatio = new Size(0, 0);
      if (ServiceRegistration.Get<IPlayerManager>(false) == null)
        ServiceRegistration.Get<ILogger>().Debug("VideoBrush.SetupBrush: Player manager not found");
    }

    void UpdateVertexBuffer(IVideoPlayer player, VertexBuffer vertexBuffer, float zOrder)
    {
      Size size = player.VideoSize;
      Size aspectRatio = player.VideoAspectRatio;

      // Correct pixelAspectRatio for anamorphic video
      float pixelRatio = aspectRatio.IsEmpty ? 1.0f : 
        (aspectRatio.Height * size.Width) / (float) (aspectRatio.Width * size.Height);

      IGeometry geometry = player.GeometryOverride;
      IGeometryManager geometryManager = ServiceRegistration.Get<IGeometryManager>();
      if (geometry == null)
        geometry = geometryManager.DefaultVideoGeometry;
      if (size == _videoSize && aspectRatio == _videoAspectRatio && geometry == _currentGeometry && _pixelAspectRatio == pixelRatio)
          return;

      _videoSize = size;
      _videoAspectRatio = aspectRatio;
      _pixelAspectRatio = pixelRatio;
      _currentGeometry = geometry;
      Rectangle sourceRect;
      Rectangle destinationRect;
      GeometryData gd = new GeometryData(
          new Size(_videoSize.Width, _videoSize.Height), new Size((int) _vertsBounds.Width, (int) _vertsBounds.Height), _pixelAspectRatio);

      geometryManager.Transform(_currentGeometry, gd, out sourceRect, out destinationRect);
      string shaderName = geometry.Shader;
      _effect = string.IsNullOrEmpty(shaderName) ? ContentManager.GetEffect("normal") :
          ContentManager.GetEffect(shaderName);

      float minU = sourceRect.X / (float) _videoSize.Width;
      float minV = sourceRect.Y / (float) _videoSize.Height;
      float diffU = sourceRect.Width / (float) _videoSize.Width;
      float diffV = sourceRect.Height / (float) _videoSize.Height;

      PositionColored2Textured[] verts = new PositionColored2Textured[_verts.Length];
      for (int i = 0; i < _verts.Length; ++i)
      {
        verts[i].X = ((_verts[i].X - _vertsBounds.Left) / _vertsBounds.Width) * destinationRect.Width + destinationRect.Left + _vertsBounds.Left;
        verts[i].Y = ((_verts[i].Y - _vertsBounds.Top) / _vertsBounds.Height) * destinationRect.Height + destinationRect.Top + _vertsBounds.Top;

        verts[i].Tu1 = _verts[i].Tu1 * diffU + minU;
        verts[i].Tv1 = _verts[i].Tv1 * diffV + minV;
        verts[i].Color = _verts[i].Color;
        verts[i].Z = zOrder;
      }
      PositionColored2Textured.Set(vertexBuffer, verts);
    }

    public override bool BeginRenderBrush(PrimitiveContext primitiveContext, RenderContext renderContext)
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>(false);
      if (playerManager == null)
      {
        _renderPlayer = null;
        return false;
      }
      // The Stream property could change between the calls of BeginRenderBrush and EndRender,
      // so we memorize the rendering player for the EndRender method
      _renderPlayer = playerManager[Stream] as ISlimDXVideoPlayer;
      if (_renderPlayer == null) return false;

      UpdateVertexBuffer(_renderPlayer, primitiveContext.VertexBuffer, renderContext.ZOrder);
      _renderPlayer.BeginRender(_effect, renderContext.Transform);
      return true;
    }

    public override void BeginRenderOpacityBrush(Texture tex, RenderContext renderContext)
    {
      throw new NotImplementedException("VideoBrush doesn't support to be rendered as opacity brush");
    }

    public override void EndRender()
    {
      if (_renderPlayer == null) return;
      _renderPlayer.EndRender(_effect);
      _renderPlayer = null;
    }
  }
}

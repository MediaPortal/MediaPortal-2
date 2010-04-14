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

using System.Drawing;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Effects;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Players;
using SlimDX.Direct3D9;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.SkinManagement;

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

    public override void SetupBrush(RectangleF bounds, ExtendedMatrix layoutTransform, float zOrder, ref PositionColored2Textured[] verts)
    {
      UpdateBounds(bounds, layoutTransform, ref verts);
      base.SetupBrush(bounds, layoutTransform, zOrder, ref verts);
      _effect = ContentManager.GetEffect("normal");
      _verts = verts;
      _videoSize = new Size(0, 0);
      _videoAspectRatio = new Size(0, 0);
      if (ServiceScope.Get<IPlayerManager>(false) == null)
        ServiceScope.Get<ILogger>().Debug("VideoBrush.SetupBrush: Player manager not found");
    }

    void UpdateVertexBuffer(IVideoPlayer player, VertexBuffer vertexBuffer)
    {
      Size size = player.VideoSize;
      Size aspectRatio = player.VideoAspectRatio;

      // Correct pixelAspectRatio for anamorphic video
      float pixelRatio = aspectRatio.IsEmpty ? 1.0f : 
        (aspectRatio.Height * size.Width) / (float) (aspectRatio.Width * size.Height);

      IGeometry geometry = player.GeometryOverride;
      IGeometryManager geometryManager = ServiceScope.Get<IGeometryManager>();
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
          new Size(_videoSize.Width, _videoSize.Height), new Size((int)_bounds.Width, (int)_bounds.Height), _pixelAspectRatio);

      geometryManager.Transform(_currentGeometry, gd, out sourceRect, out destinationRect);
      string shaderName = geometry.Shader;
      _effect = string.IsNullOrEmpty(shaderName) ? ContentManager.GetEffect("normal") :
          ContentManager.GetEffect(shaderName);

      float minU = sourceRect.X / (float) _videoSize.Width;
      float minV = sourceRect.Y / (float) _videoSize.Height;
      float maxU = sourceRect.Width / (float) _videoSize.Width;
      float maxV = sourceRect.Height / (float) _videoSize.Height;

      float minX = destinationRect.X / _bounds.Width;
      float minY = destinationRect.Y / _bounds.Height;

      float maxX = destinationRect.Width / _bounds.Width;
      float maxY = destinationRect.Height / _bounds.Height;

      float diffU = maxU - minU;
      float diffV = maxV - minV;
      PositionColored2Textured[] verts = new PositionColored2Textured[_verts.Length];
      for (int i = 0; i < _verts.Length; ++i)
      {
        float x = ((_verts[i].X - _minPosition.X) / _bounds.Width) * maxX + minX;
        float y = ((_verts[i].Y - _minPosition.Y) / _bounds.Height) * maxY + minY;
        verts[i].X = (x * _bounds.Width) + _minPosition.X;
        verts[i].Y = (y * _bounds.Height) + _minPosition.Y;

        float u = _verts[i].Tu1 * diffU + minU;
        float v = _verts[i].Tv1 * diffV + minV;
        verts[i].Tu1 = u;
        verts[i].Tv1 = v;
        verts[i].Color = _verts[i].Color;
        verts[i].Z = SkinContext.GetZorder();
      }
      PositionColored2Textured.Set(vertexBuffer, ref verts);
    }

    public override bool BeginRender(VertexBuffer vertexBuffer, int primitiveCount, PrimitiveType primitiveType)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>(false);
      if (playerManager == null)
      {
        _renderPlayer = null;
        return false;
      }
      // The Stream property could change between the calls of BeginRender and EndRender,
      // so we memorize the rendering player for the EndRender method
      _renderPlayer = playerManager[Stream] as ISlimDXVideoPlayer;
      if (_renderPlayer == null) return false;

      if (Transform != null)
      {
        ExtendedMatrix mTrans;
        Transform.GetTransform(out mTrans);
        SkinContext.AddTransform(mTrans);
      }

      UpdateVertexBuffer(_renderPlayer, vertexBuffer);
      _renderPlayer.BeginRender(_effect);
      return true;
    }

    public override void EndRender()
    {
      if (_renderPlayer == null) return;
      _renderPlayer.EndRender(_effect);
      _renderPlayer = null;
      if (Transform != null)
        SkinContext.RemoveTransform();
    }
  }
}

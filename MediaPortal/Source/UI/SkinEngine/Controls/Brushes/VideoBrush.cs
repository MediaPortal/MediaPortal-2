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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
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
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SlimDX.Direct3D9;
using SlimDX;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public class VideoBrush : Brush
  {
    #region Consts

    protected const string EFFECT_DEFAULT_VIDEO = "videodefault";

    protected const string PARAM_TRANSFORM = "g_transform";
    protected const string PARAM_OPACITY = "g_opacity";

    protected const string PARAM_TEXTURE = "g_texture";
    protected const string PARAM_ALPHATEX = "g_alphatex";
    protected const string PARAM_RELATIVE_TRANSFORM = "g_relativetransform";
    protected const string PARAM_BRUSH_TRANSFORM = "g_brushtransform";

    #endregion

    #region Protected fields

    protected AbstractProperty _streamProperty;
    protected AbstractProperty _geometryProperty;
    protected AbstractProperty _borderColorProperty;
    protected EffectAsset _effect;
    protected IGeometry _currentGeometry;
    protected IGeometry _lastGeometry;
    protected Size _lastVideoSize;
    protected Size _lastAspectRatio;
    protected ISlimDXVideoPlayer _renderPlayer;
    protected Matrix _relativeTransformCache;
    protected Vector4 _brushTransform;
    protected bool _refresh;
    #endregion

    #region Ctor

    public VideoBrush()
    {
      Init();
    }

    void Init()
    {
      _streamProperty = new SProperty(typeof(int), 0);
      _geometryProperty = new SProperty(typeof(string), "");
      _borderColorProperty = new SProperty(typeof(Color), Color.Black);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      VideoBrush b = (VideoBrush) source;
      Stream = b.Stream;
      Geometry = b.Geometry;
      BorderColor = b.BorderColor;
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

    public AbstractProperty GeometryProperty
    {
      get { return _geometryProperty; }
    }

    /// <summary>
    /// Allows the skin to override the video gemoetry asked for by the player.
    /// </summary>
    public string Geometry
    {
      get { return (string) _geometryProperty.GetValue(); }
      set { 
          _geometryProperty.SetValue(value);
          IGeometryManager geometryManager = ServiceRegistration.Get<IGeometryManager>();
          IGeometry geometry;
          string name = value;
          if (String.IsNullOrEmpty(name))
            _currentGeometry = null;
          else if (geometryManager.AvailableGeometries.TryGetValue(name, out geometry))
            _currentGeometry = geometry;
          else {
            ServiceRegistration.Get<ILogger>().Debug(@"VideoBrush: Geometry '{0}' does not exist", name);
            _currentGeometry = null;
          }
      }
    }

    public AbstractProperty BorderColorProperty
    {
      get { return _borderColorProperty; }
    }

    /// <summary>
    /// Gets or sets the color to be used for drawing bars/borders around the video
    /// </summary>
    public Color BorderColor
    {
      get { return (Color) _borderColorProperty.GetValue(); }
      set { _borderColorProperty.SetValue(value); }
    }

    #endregion

    #region Protected members

    protected IGeometry ChooseVideoGeometry(IVideoPlayer player)
    {
      if (_currentGeometry != null)
        return _currentGeometry;
      if (player.GeometryOverride != null)
        return player.GeometryOverride;

      return ServiceRegistration.Get<IGeometryManager>().DefaultVideoGeometry;
    }

    protected void RefreshEffectParameters(IVideoPlayer player)
    {
      Size aspectRatio = player.VideoAspectRatio;
      Size playerSize = player.VideoSize;
      IGeometry geometry = ChooseVideoGeometry(player);

      // Do we need a refresh?
      if (!_refresh && _lastVideoSize == playerSize && _lastAspectRatio == player.VideoAspectRatio && geometry == _lastGeometry)
        return;

      SizeF targetSize = _vertsBounds.Size;
      SizeF videoSize = playerSize;

      // Get Effect
      string shaderName = geometry.Shader;
      _effect = ServiceRegistration.Get<ContentManager>().GetEffect(String.IsNullOrEmpty(shaderName) ? EFFECT_DEFAULT_VIDEO : shaderName);
      
      // Correct aspect ratio for anamorphic video
      if (!aspectRatio.IsEmpty)
      {
        float pixelRatio = aspectRatio.Width / (float) aspectRatio.Height;
        videoSize.Width = videoSize.Height * pixelRatio; 
      }
      // Adjust target size to match final Skin scaling
      targetSize.Width *= (GraphicsDevice.Width / (float) SkinContext.SkinResources.SkinWidth);
      targetSize.Height *= (GraphicsDevice.Height / (float) SkinContext.SkinResources.SkinHeight);
      // Adjust video size to fit desired geometry
      videoSize = geometry.Transform(videoSize, targetSize);
      
      // Convert brush dimensions to viewport space
      SizeF maxuv = player.SurfaceMaxUV;
      Vector4 brushRect = new Vector4(0.0f, 0.0f, videoSize.Width, videoSize.Height);
      brushRect.Z /= targetSize.Width * maxuv.Width;
      brushRect.W /= targetSize.Height * maxuv.Height;

      // Center texture
      brushRect.X += (1.0f - brushRect.Z) / 2.0f;
      brushRect.Y += (1.0f - brushRect.W) / 2.0f;

      // Determine correct 2D transoform for mapping the texture to the correct place
      float repeatx = 1.0f / brushRect.Z;
      float repeaty = 1.0f / brushRect.W;
      if (repeatx < 0.001f || repeaty < 0.001f)
      {
        _refresh = true;
        return;
      }

      _brushTransform = new Vector4(brushRect.X * repeatx, brushRect.Y * repeaty, repeatx, repeaty);

      // Cache inverse relative transform
      _relativeTransformCache = (RelativeTransform == null) ? Matrix.Identity : Matrix.Invert(RelativeTransform.GetTransform());

      // Store state
      _lastVideoSize = playerSize;
      _lastAspectRatio = aspectRatio;
      _lastGeometry = geometry;

      _refresh = false;
    }

    protected override void OnPropertyChanged(AbstractProperty prop, object oldValue)
    {
      _refresh = true;
      base.OnPropertyChanged(prop, oldValue);
    }

    #endregion

    #region Public members

    public override void SetupBrush(FrameworkElement parent, ref PositionColored2Textured[] verts, float zOrder, bool adaptVertsToBrushTexture)
    {
      base.SetupBrush(parent, ref verts, zOrder, adaptVertsToBrushTexture);
      if (ServiceRegistration.Get<IPlayerManager>(false) == null)
        ServiceRegistration.Get<ILogger>().Debug("VideoBrush.SetupBrush: Player manager not found");
    }

    public override bool BeginRenderBrush(PrimitiveBuffer primitiveContext, RenderContext renderContext)
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

      RefreshEffectParameters(_renderPlayer);

      if (_effect == null)
        return false;

      _effect.Parameters[PARAM_RELATIVE_TRANSFORM] = _relativeTransformCache;
      _effect.Parameters[PARAM_TRANSFORM] = GetCachedFinalBrushTransform();
      _effect.Parameters[PARAM_OPACITY] = (float) (Opacity * renderContext.Opacity);
      _effect.Parameters[PARAM_BRUSH_TRANSFORM] = _brushTransform;

      GraphicsDevice.Device.SetSamplerState(0, SamplerState.BorderColor, BorderColor.ToArgb());

      _renderPlayer.BeginRender(_effect, renderContext.Transform);

      return true;
    }

    public override void BeginRenderOpacityBrush(Texture tex, RenderContext renderContext)
    {
      throw new NotImplementedException("VideoBrush doesn't support use rendered as an opacity brush");
    }

    public override void EndRender()
    {
      if (_renderPlayer == null) return;
      _renderPlayer.EndRender(_effect);
      _renderPlayer = null;
    }

    #endregion
  }
}

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
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.Rendering;
using SlimDX.Direct3D9;
using SlimDX;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public class VideoBrush : Brush
  {
    #region Consts

    protected const string EFFECT_BASE_VIDEO = "video_base";

    protected const string PARAM_TRANSFORM = "g_transform";
    protected const string PARAM_RELATIVE_TRANSFORM = "g_relativetransform";

    #endregion

    #region Protected fields

    protected AbstractProperty _streamProperty;
    protected AbstractProperty _geometryProperty;
    protected AbstractProperty _borderColorProperty;

    protected IGeometry _currentGeometry;
    protected Matrix _relativeTransformCache;
    protected ImageContext _imageContext;
    protected SizeF _scaledVideoSize;

    protected IGeometry _lastGeometry;
    protected string _lastEffect;
    protected CropSettings _lastCropSettings;
    protected Size _lastVideoSize;
    protected Size _lastAspectRatio;
    protected int _lastDeviceWidth;
    protected int _lastDeviceHeight;
    protected Vector4 _lastFrameData;
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

      _imageContext = new ImageContext();
      _imageContext.OnRefresh = OnImagecontextRefresh;
      _imageContext.ExtraParameters = new System.Collections.Generic.Dictionary<string, object>();
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

    /// <summary>
    /// Gets or sets the player stream number.
    /// </summary>
    public int Stream
    {
      get { return (int) _streamProperty.GetValue(); }
      set { _streamProperty.SetValue(value); }
    }

    public AbstractProperty StreamProperty
    {
      get { return _streamProperty; }
    }

    /// <summary>
    /// Allows the skin to override the video gemoetry asked for by the player.
    /// </summary>
    public string Geometry
    {
      get { return (string) _geometryProperty.GetValue(); }
      set
      { 
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

    public AbstractProperty GeometryProperty
    {
      get { return _geometryProperty; }
    }

    /// <summary>
    /// Gets or sets the color to be used for drawing bars/borders around the video
    /// </summary>
    public Color BorderColor
    {
      get { return (Color) _borderColorProperty.GetValue(); }
      set { _borderColorProperty.SetValue(value); }
    }

    public AbstractProperty BorderColorProperty
    {
      get { return _borderColorProperty; }
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
      ISlimDXVideoPlayer sdvPlayer = player as ISlimDXVideoPlayer;
      if (sdvPlayer == null)
        return;
      Size aspectRatio = sdvPlayer.VideoAspectRatio;
      Size playerSize = sdvPlayer.VideoSize;
      IGeometry geometry = ChooseVideoGeometry(player);
      CropSettings cropSettings = player.CropSettings;
      string effectName = player.EffectOverride;

      // Do we need a refresh?
      if (_lastVideoSize == playerSize &&
          _lastAspectRatio == player.VideoAspectRatio &&
          _lastGeometry == geometry &&
          _lastEffect == effectName &&
          _lastCropSettings == cropSettings &&
          _lastDeviceWidth == GraphicsDevice.Width &&
          _lastDeviceHeight == GraphicsDevice.Height)
        return;

      SizeF targetSize = _vertsBounds.Size;

      _scaledVideoSize = cropSettings == null ? playerSize : cropSettings.CropRect(playerSize).Size;
      
      // Correct aspect ratio for anamorphic video
      if (!aspectRatio.IsEmpty && geometry.RequiresCorrectAspectRatio)
      {
        float pixelRatio = aspectRatio.Width / (float) aspectRatio.Height;
        _scaledVideoSize.Width = _scaledVideoSize.Height * pixelRatio; 
      }
      // Adjust target size to match final Skin scaling
      targetSize = ImageContext.AdjustForSkinAR(targetSize);
      
      // Adjust video size to fit desired geometry
      _scaledVideoSize = geometry.Transform(_scaledVideoSize, targetSize);

      // Cache inverse RelativeTransform
      _relativeTransformCache = (RelativeTransform == null) ? Matrix.Identity : Matrix.Invert(RelativeTransform.GetTransform());

      // Prepare our ImageContext
      _imageContext.FrameSize = targetSize;
      _imageContext.ShaderBase = EFFECT_BASE_VIDEO;
      _imageContext.ShaderTransform = geometry.Shader;
      _imageContext.ShaderEffect = player.EffectOverride;

      // Store state
      _lastFrameData = new Vector4(playerSize.Width, playerSize.Height, 0.0f, 0.0f);
      _lastVideoSize = playerSize;
      _lastAspectRatio = aspectRatio;
      _lastGeometry = geometry;
      _lastCropSettings = cropSettings;
      _lastEffect = effectName;
      _lastDeviceWidth = GraphicsDevice.Width;
      _lastDeviceHeight = GraphicsDevice.Height;
    }

    protected void OnImagecontextRefresh()
    {
      _imageContext.ExtraParameters[PARAM_RELATIVE_TRANSFORM] = _relativeTransformCache;
      _imageContext.ExtraParameters[PARAM_TRANSFORM] = GetCachedFinalBrushTransform();
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
        return false;

      ISlimDXVideoPlayer player = playerManager[Stream] as ISlimDXVideoPlayer;
      if (player == null) 
          return false;

      RefreshEffectParameters(player);

      // NOTE: It appears that render textures are always allocated to the exact size (not nearest power-of-2), which makes this SurfaceMaxUV stuff
      // unnecessary. It is unclear whether this is how it is done by every graphics driver though, so I'll leave it in for the 
      // time being.
      SizeF maxuv = player.SurfaceMaxUV;
      return _imageContext.StartRender(renderContext, _scaledVideoSize, player.Texture, maxuv.Width, maxuv.Height, BorderColor.ToArgb(), _lastFrameData);
    }

    public override void BeginRenderOpacityBrush(Texture tex, RenderContext renderContext)
    {
      throw new NotImplementedException("VideoBrush doesn't support being rendered as an opacity brush");
    }

    public override void EndRender()
    {
      _imageContext.EndRender();
    }

    #endregion
  }
}

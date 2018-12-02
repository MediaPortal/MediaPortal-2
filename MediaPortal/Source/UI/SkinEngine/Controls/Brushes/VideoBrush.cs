#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.DirectX11;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX.Direct2D1;
using SharpDX.Direct3D9;
using SharpDX;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Utilities.DeepCopy;
using SharpDX.Mathematics.Interop;
using Transform = MediaPortal.UI.SkinEngine.Controls.Transforms.Transform;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  /// <summary>
  /// Brush which paints the video image of a player of type <see cref="ISharpDXVideoPlayer"/> provided by the <see cref="IPlayerManager"/>.
  /// </summary>
  public class VideoBrush : Brush, IRenderBrush
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
    protected Matrix _inverseRelativeTransformCache;
    protected ImageContext _imageContext;
    protected Size2F _scaledVideoSize;
    protected RectangleF _videoTextureClip;

    protected IGeometry _lastGeometry;
    protected string _lastEffect;
    protected Rectangle _lastCropVideoRect;
    protected Size2 _lastVideoSize;
    protected Size2F _lastAspectRatio;
    protected int _lastDeviceWidth;
    protected int _lastDeviceHeight;
    protected Vector4 _lastFrameData;
    protected RectangleF _lastVertsBounds;
    protected Texture _texture = null;
    protected volatile bool _refresh = true;
    private Bitmap1 _videoBitmap;

    #endregion

    #region Ctor

    public VideoBrush()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _streamProperty = new SProperty(typeof(int), 0);
      _geometryProperty = new SProperty(typeof(string), null);
      _borderColorProperty = new SProperty(typeof(Color), Color.Black);

      _imageContext = new ImageContext { OnRefresh = OnImagecontextRefresh };
    }

    void Attach()
    {
      _geometryProperty.Attach(OnGeometryChange);
    }

    void Detach()
    {
      _geometryProperty.Detach(OnGeometryChange);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      VideoBrush b = (VideoBrush)source;
      Stream = b.Stream;
      Geometry = b.Geometry;
      BorderColor = b.BorderColor;
      _refresh = true;
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      TryDispose(ref _texture);
    }

    #endregion

    #region Protected & private members

    void OnGeometryChange(AbstractProperty prop, object oldVal)
    {
      string geometryName = Geometry;
      if (String.IsNullOrEmpty(geometryName))
      {
        _currentGeometry = null;
        return;
      }
      IGeometryManager geometryManager = ServiceRegistration.Get<IGeometryManager>();
      IGeometry geometry;
      if (geometryManager.AvailableGeometries.TryGetValue(geometryName, out geometry))
        _currentGeometry = geometry;
      else
      {
        ServiceRegistration.Get<ILogger>().Debug("VideoBrush: Geometry '{0}' does not exist", geometryName);
        _currentGeometry = null;
      }
    }

    protected override void OnRelativeTransformChanged(IObservable trans)
    {
      _refresh = true;
      base.OnRelativeTransformChanged(trans);
    }

    protected IGeometry ChooseVideoGeometry(IVideoPlayer player)
    {
      if (_currentGeometry != null)
        return _currentGeometry;
      if (player.GeometryOverride != null)
        return player.GeometryOverride;

      return ServiceRegistration.Get<IGeometryManager>().DefaultVideoGeometry;
    }

    protected bool RefreshEffectParameters(IVideoPlayer player)
    {
      ISharpDXVideoPlayer sdvPlayer = player as ISharpDXVideoPlayer;
      if (sdvPlayer == null)
        return false;
      Size2F aspectRatio = sdvPlayer.VideoAspectRatio.ToSize2F();
      Size2 playerSize = sdvPlayer.VideoSize.ToSize2();
      Rectangle cropVideoRect = sdvPlayer.CropVideoRect;
      IGeometry geometry = ChooseVideoGeometry(player);
      string effectName = player.EffectOverride;
      int deviceWidth = GraphicsDevice.Width; // To avoid threading issues if the device size changes
      int deviceHeight = GraphicsDevice.Height;
      RectangleF vertsBounds = _vertsBounds;

      // Do we need a refresh?
      if (!_refresh &&
          _lastVideoSize == playerSize &&
          _lastAspectRatio == aspectRatio &&
          _lastCropVideoRect == cropVideoRect &&
          _lastGeometry == geometry &&
          _lastEffect == effectName &&
          _lastDeviceWidth == deviceWidth &&
          _lastDeviceHeight == deviceHeight &&
          _lastVertsBounds == vertsBounds)
        return true;

      Size2F targetSize = vertsBounds.Size;

      lock (sdvPlayer.SurfaceLock)
      {
        var surface = sdvPlayer.Surface;
        if (surface == null)
        {
          _refresh = true;
          return false;
        }

        _videoTextureClip = new RectangleF(cropVideoRect.X / (float)surface.Width, cropVideoRect.Y / (float)surface.Height,
            cropVideoRect.Width / (float)surface.Width, cropVideoRect.Height / (float)surface.Height);
      }
      _scaledVideoSize = cropVideoRect.ToSize2F();

      // Correct aspect ratio for anamorphic video
      if (!aspectRatio.IsEmpty() && geometry.RequiresCorrectAspectRatio)
      {
        float pixelRatio = aspectRatio.Width / aspectRatio.Height;
        _scaledVideoSize.Width = _scaledVideoSize.Height * pixelRatio;
      }
      // Adjust target size to match final Skin scaling
      targetSize = ImageContext.AdjustForSkinAR(targetSize);

      // Adjust video size to fit desired geometry
      _scaledVideoSize = geometry.Transform(_scaledVideoSize.ToDrawingSizeF(), targetSize.ToDrawingSizeF()).ToSize2F();

      // Cache inverse RelativeTransform
      Transform relativeTransform = RelativeTransform;
      _inverseRelativeTransformCache = (relativeTransform == null) ? Matrix.Identity : Matrix.Invert(relativeTransform.GetTransform());

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
      _lastCropVideoRect = cropVideoRect;
      _lastEffect = effectName;
      _lastDeviceWidth = deviceWidth;
      _lastDeviceHeight = deviceHeight;

      _refresh = false;
      return true;
    }

    protected void OnImagecontextRefresh()
    {
      _imageContext.RelativeTransform = _inverseRelativeTransformCache;
      _imageContext.Transform = GetCachedFinalBrushTransform();
    }

    #endregion

    #region Public properties

    public AbstractProperty StreamProperty
    {
      get { return _streamProperty; }
    }

    /// <summary>
    /// Gets or sets the number of the player stream to be shown.
    /// </summary>
    public int Stream
    {
      get { return (int)_streamProperty.GetValue(); }
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
      get { return (string)_geometryProperty.GetValue(); }
      set { _geometryProperty.SetValue(value); }
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
      get { return (Color)_borderColorProperty.GetValue(); }
      set { _borderColorProperty.SetValue(value); }
    }

    #endregion

    #region Public members

    public override void SetupBrush(FrameworkElement parent, ref RawRectangleF boundary, float zOrder, bool adaptVertsToBrushTexture)
    {
      base.SetupBrush(parent, ref boundary, zOrder, adaptVertsToBrushTexture);
      if (ServiceRegistration.Get<IPlayerManager>(false) == null)
        ServiceRegistration.Get<ILogger>().Debug("VideoBrush.SetupBrush: Player manager not found");
    }

    //protected bool BeginRenderBrushOverride(PrimitiveBuffer primitiveContext, RenderContext renderContext)
    public bool RenderContent(RenderContext renderContext)
    {
      ISharpDXVideoPlayer player;
      if (!GetPlayer(out player))
        return false;

      if (!RefreshEffectParameters(player))
        return false;


      lock (player.SurfaceLock)
      {
        // Force a refresh of effekt parameters when real brush is set first
        if (!(_brush2D is BitmapBrush1))
          _refresh = true;

        var playerSurface = player.Surface;
        if (playerSurface != null && _videoBitmap != playerSurface.Bitmap)
        {
          _videoBitmap = playerSurface.Bitmap;
        }
      }

      if (_videoBitmap == null)
        return false;

      // Handling of multipass (3D) rendering, transformed rect contains the clipped area of the source image (i.e. left side in Side-By-Side mode).
      RectangleF tranformedRect;
      GraphicsDevice11.Instance.RenderPipeline.GetVideoClip(_videoTextureClip, out tranformedRect);
      return _imageContext.StartRender(renderContext, _scaledVideoSize, _videoBitmap, tranformedRect, BorderColor, _lastFrameData);
    }

    protected virtual bool GetPlayer(out ISharpDXVideoPlayer player)
    {
      player = null;
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>(false);
      if (playerContextManager == null)
        return false;

      player = playerContextManager[Stream] as ISharpDXVideoPlayer;
      return player != null;
    }

    public void EndRender()
    {
      //_imageContext.EndRender();
    }

    #endregion
  }
}

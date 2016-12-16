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
using System.Diagnostics;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.UI.SkinEngine.Controls.Transforms;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX.Direct3D9;
using SharpDX;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Utilities.DeepCopy;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  /// <summary>
  /// Brush which paints the video image of a player of type <see cref="ISharpDXVideoPlayer"/> provided by the <see cref="IPlayerManager"/>.
  /// </summary>
  public class VideoBrush : Brush
  {
    [DebuggerDisplay("BrushContext '{ContextName}'")]
    public class BrushContext : IDisposable
    {
      protected Matrix _inverseRelativeTransformCache;
      protected ImageContext _imageContext;
      protected SizeF _scaledVideoSize;
      protected RectangleF _videoTextureClip;

      protected IGeometry _currentGeometry;
      protected IGeometry _lastGeometry;
      protected string _lastEffect;
      protected Rectangle _lastCropVideoRect;
      protected Size _lastVideoSize;
      protected SizeF _lastAspectRatio;
      protected int _lastDeviceWidth;
      protected int _lastDeviceHeight;
      protected Vector4 _lastFrameData;
      protected RectangleF _lastVertsBounds;
      protected Texture _texture = null;
      protected volatile bool _refresh = true;
      protected readonly VideoBrush _parentBrush;
      protected bool _renderStarted = false;
      protected PrimitiveBuffer _primitiveContext;

      public BrushContext(VideoBrush parentBrush)
      {
        _parentBrush = parentBrush;
        _imageContext = new ImageContext
        {
          OnRefresh = OnImagecontextRefresh,
          ExtraParameters = new Dictionary<string, object>()
        };
      }

      public Func<Texture> GetBrushTexture { get; set; }
      public Func<RectangleF> GetVertBounds { get; set; }
      public Func<Transform> GetRelativeTransform { get; set; }
      public Func<Matrix> GetCachedFinalBrushTransform { get; set; }
      public string ContextName { get; set; }

      public void Dispose()
      {
        TryDispose(ref _texture);
      }

      public void Refresh()
      {
        _refresh = true;
      }

      public void SetGeoemtry(IGeometry geometry)
      {
        _currentGeometry = geometry;
      }

      public bool RefreshEffectParameters(IVideoPlayer player, Texture texture)
      {
        ISharpDXVideoPlayer sdvPlayer = player as ISharpDXVideoPlayer;
        if (sdvPlayer == null || texture == null || texture.IsDisposed)
          return false;
        SizeF aspectRatio = sdvPlayer.VideoAspectRatio.ToSize2F();
        Size playerSize = sdvPlayer.VideoSize.ToSize2();
        Rectangle cropVideoRect = sdvPlayer.CropVideoRect;
        IGeometry geometry = ChooseVideoGeometry(player);
        string effectName = player.EffectOverride;
        int deviceWidth = GraphicsDevice.Width; // To avoid threading issues if the device size changes
        int deviceHeight = GraphicsDevice.Height;
        RectangleF vertsBounds = GetVertBounds();

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

        _lastVertsBounds = vertsBounds;
        SizeF targetSize = vertsBounds.Size;

        lock (sdvPlayer.SurfaceLock)
        {
          SurfaceDescription desc = texture.GetLevelDescription(0);
          _videoTextureClip = new RectangleF(cropVideoRect.X / desc.Width, cropVideoRect.Y / desc.Height,
              cropVideoRect.Width / desc.Width, cropVideoRect.Height / desc.Height);
        }
        _scaledVideoSize = cropVideoRect.Size.ToSize2F();

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
        Transform relativeTransform = GetRelativeTransform();
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

      protected IGeometry ChooseVideoGeometry(IVideoPlayer player)
      {
        if (_currentGeometry != null)
          return _currentGeometry;
        if (player.GeometryOverride != null)
          return player.GeometryOverride;

        return ServiceRegistration.Get<IGeometryManager>().DefaultVideoGeometry;
      }

      protected void OnImagecontextRefresh()
      {
        _imageContext.ExtraParameters[PARAM_RELATIVE_TRANSFORM] = _inverseRelativeTransformCache;
        _imageContext.ExtraParameters[PARAM_TRANSFORM] = GetCachedFinalBrushTransform();
      }

      public bool BeginRenderBrushOverride(PrimitiveBuffer primitiveContext, RenderContext renderContext)
      {
        _primitiveContext = primitiveContext;
        _renderStarted = false;
        if (!IsValid())
          return false;

        _renderStarted = true;
        // Handling of multipass (3D) rendering, transformed rect contains the clipped area of the source image (i.e. left side in Side-By-Side mode).
        RectangleF tranformedRect;
        GraphicsDevice.RenderPipeline.GetVideoClip(_videoTextureClip, out tranformedRect);
        return _imageContext.StartRender(renderContext, _scaledVideoSize, _texture, tranformedRect, _parentBrush.BorderColor, _lastFrameData);
      }

      public bool IsValid()
      {
        if (GetBrushTexture == null)
          return false;
        _texture = GetBrushTexture();
        return _texture != null;
      }

      public void Render(int stream)
      {
        if (_primitiveContext != null && _renderStarted)
          _primitiveContext.Render(stream);
      }

      public void EndRender()
      {
        if (_renderStarted)
          _imageContext.EndRender();
      }
    }

    #region Consts

    protected const string EFFECT_BASE_VIDEO = "video_base";

    protected const string PARAM_TRANSFORM = "g_transform";
    protected const string PARAM_RELATIVE_TRANSFORM = "g_relativetransform";

    #endregion

    #region Protected fields

    protected AbstractProperty _streamProperty;
    protected AbstractProperty _geometryProperty;
    protected AbstractProperty _borderColorProperty;

    protected List<BrushContext> _brushContexts = new List<BrushContext>();
    protected BrushContext _lastBeginContext;

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

      ISharpDXVideoPlayer player;

      // Primary video texture
      BrushContext videoContext = CreateAndAddBrushContext();
      videoContext.ContextName = "MainVideoTexture";
      videoContext.GetBrushTexture = () =>
      {
        if (!GetPlayer(out player))
          return null;

        lock (player.SurfaceLock)
        {
          var texture = player.Texture;
          if (!videoContext.RefreshEffectParameters(player, texture))
            return null;
          return texture;
        }
      };

      // Check for multiple texture planes
      if (!GetPlayer(out player))
        return;

      ISharpDXMultiTexturePlayer multiTexturePlayer = player as ISharpDXMultiTexturePlayer;
      if (multiTexturePlayer == null)
        return;

      for(int i = 0; i < multiTexturePlayer.TexturePlanes.Length; i++)
      {
        int plane = i;
        // All additional overlay textures
        var planeContext = CreateAndAddBrushContext();
        planeContext.ContextName = "OverlayTexture_" + plane;
        planeContext.GetBrushTexture = () =>
        {
          if (!GetPlayer(out player))
            return null;

          lock (multiTexturePlayer.SurfaceLock)
          {
            var texture = multiTexturePlayer.TexturePlanes[plane];
            if (!planeContext.RefreshEffectParameters(player, texture))
              return null;
            return texture;
          }
        };
      }
    }

    protected BrushContext CreateAndAddBrushContext()
    {
      BrushContext context = new BrushContext(this)
      {
        GetCachedFinalBrushTransform = GetCachedFinalBrushTransform,
        GetRelativeTransform = () => RelativeTransform,
        GetVertBounds = () => _vertsBounds
      };
      _brushContexts.Add(context);
      return context;
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
      foreach (var brushContext in _brushContexts)
        brushContext.Refresh();
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      foreach (var brushContext in _brushContexts)
      {
        brushContext.Dispose();
      }
      _brushContexts.Clear();
    }

    #endregion

    #region Protected & private members

    void OnGeometryChange(AbstractProperty prop, object oldVal)
    {
      string geometryName = Geometry;
      if (String.IsNullOrEmpty(geometryName))
      {
        SetGeometries(null);
        return;
      }
      IGeometryManager geometryManager = ServiceRegistration.Get<IGeometryManager>();
      IGeometry geometry;
      if (geometryManager.AvailableGeometries.TryGetValue(geometryName, out geometry))
        SetGeometries(geometry);
      else
      {
        ServiceRegistration.Get<ILogger>().Debug("VideoBrush: Geometry '{0}' does not exist", geometryName);
        SetGeometries(null);
      }
    }

    protected void SetGeometries(IGeometry geometry)
    {
      foreach (var brushContext in _brushContexts)
        brushContext.SetGeoemtry(geometry);
    }

    protected override void OnRelativeTransformChanged(IObservable trans)
    {
      foreach (var brushContext in _brushContexts)
        brushContext.Refresh();
      base.OnRelativeTransformChanged(trans);
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

    public override void SetupBrush(FrameworkElement parent, ref PositionColoredTextured[] verts, float zOrder, bool adaptVertsToBrushTexture)
    {
      base.SetupBrush(parent, ref verts, zOrder, adaptVertsToBrushTexture);
      if (ServiceRegistration.Get<IPlayerManager>(false) == null)
        ServiceRegistration.Get<ILogger>().Debug("VideoBrush.SetupBrush: Player manager not found");
    }

    protected override bool BeginRenderBrushOverride(PrimitiveBuffer primitiveContext, RenderContext renderContext)
    {
      // Clear last context
      _lastBeginContext = null;
      bool result = false;
      foreach (var brushContext in _brushContexts)
      {
        // We can only begin a new render pass if the previous ended
        if (_lastBeginContext != null && brushContext.IsValid())
        {
          _lastBeginContext.Render(0);
          _lastBeginContext.EndRender();
          _lastBeginContext = null;
        }

        var currentResult = brushContext.BeginRenderBrushOverride(primitiveContext, renderContext);
        result |= currentResult;
        if (currentResult)
          _lastBeginContext = brushContext;
      }
      return result;
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

    public override void EndRender()
    {
      if (_lastBeginContext != null)
        _lastBeginContext.EndRender();
    }

    #endregion
  }
}

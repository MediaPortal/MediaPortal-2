#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.DirectX11;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX;
using MediaPortal.Utilities.DeepCopy;
using SharpDX.Direct2D1;
using SharpDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public class VideoBrush : TileBrush
  {
    #region Protected fields

    protected AbstractProperty _streamProperty;
    protected AbstractProperty _geometryProperty;

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

    #endregion

    #region Ctor

    public VideoBrush()
    {
      Init();
      Attach();
      Stretch = Stretch.Uniform;
    }

    void Init()
    {
      _streamProperty = new SProperty(typeof(int), 0);
      _geometryProperty = new SProperty(typeof(string), null);
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
      _refresh = true;
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      TryDispose(ref _texture);
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

    #endregion

    #region Protected methods

    void OnGeometryChange(AbstractProperty prop, object oldVal)
    {
      string geometryName = Geometry;
      if (string.IsNullOrEmpty(geometryName))
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

    protected override Vector2 TextureMaxUV
    {
      get { return new Vector2(1.0f, 1.0f); }
    }

    protected override void OnPropertyChanged(AbstractProperty prop, object oldValue)
    {
      Free();
      base.OnPropertyChanged(prop, oldValue);
    }

    protected override Vector2 BrushDimensions
    {
      get { return _bitmapAsset2D == null ? base.BrushDimensions : new Vector2(_bitmapAsset2D.Width, _bitmapAsset2D.Height); }
    }

    #endregion

    #region Public methods

    public void Free()
    {
      _bitmapAsset2D = null;
    }

    public override void SetupBrush(FrameworkElement parent, ref RectangleF boundary, float zOrder, bool adaptVertsToBrushTexture)
    {
      Allocate();
      base.SetupBrush(parent, ref boundary, zOrder, adaptVertsToBrushTexture);

      // Dummy brush until real video frame is known and a bitmap brush is created
      SetBrush(new SharpDX.Direct2D1.SolidColorBrush(GraphicsDevice11.Instance.Context2D1, Color.Black));
    }

    public override bool RenderContent(RenderContext renderContext)
    {
      ISharpDXVideoPlayer player;
      if (!GetPlayer(out player))
        return false;

      lock (player.SurfaceLock)
      {
        // Force a refresh of effekt parameters when real brush is set first
        if (!(_brush2D is BitmapBrush))
          _refresh = true;

        var cropVideoRect = player.CropVideoRect;
        var playerSurface = player.Surface;
        if (playerSurface != null && _bitmapAsset2D != playerSurface)
        {
          _bitmapAsset2D = playerSurface;

          BitmapBrushProperties props = new BitmapBrushProperties
          {
            ExtendModeX = ExtendMode.Clamp,
            ExtendModeY = ExtendMode.Clamp,
          };

          SetBrush(new BitmapBrush(GraphicsDevice11.Instance.Context2D1, _bitmapAsset2D.Bitmap, props));
        }

        if (_bitmapAsset2D == null || !_bitmapAsset2D.IsAllocated)
          return false;

        var desc = _bitmapAsset2D.Bitmap.Size;
        _videoTextureClip = new RectangleF(cropVideoRect.X / desc.Width, cropVideoRect.Y / desc.Height, cropVideoRect.Width / desc.Width, cropVideoRect.Height / desc.Height);

        // Handling of multipass (3D) rendering, transformed rect contains the clipped area of the source image (i.e. left side in Side-By-Side mode).
        GraphicsDevice11.Instance.RenderPipeline.GetVideoClip(_videoTextureClip, out _textureClip);
        return base.RenderContent(renderContext);
      }
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
      _imageContext.EndRender();
    }

    #endregion
  }
}

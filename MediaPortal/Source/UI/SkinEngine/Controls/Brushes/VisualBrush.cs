#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.Utilities.DeepCopy;
using SlimDX;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public class VisualBrush : TileBrush
  {
    #region Protected fields

    protected AbstractProperty _visualProperty;
    protected AbstractProperty _autoLayoutContentProperty;
    protected RenderTextureAsset _visualTexture = null;
    protected RenderTargetAsset _visualSurface = null;
    protected Screen _screen = null;
    protected SizeF _visualSize = Size.Empty;
    protected FrameworkElement _preparedVisual = null;
    protected String _renderTextureKey;
    protected String _renderSurfaceKey;
    protected static int _visualBrushId = 0;

    #endregion

    #region Ctor

    public VisualBrush()
    {
      _visualBrushId++;
      _renderTextureKey = String.Format("VisualBrush RenderTexture #{0}", _visualBrushId);
      _renderSurfaceKey = String.Format("VisualBrush RenderSurface #{0}", _visualBrushId);

      Init();
      Attach();
    }

    public override void Dispose()
    {
      FrameworkElement visual = Visual;
      MPF.TryCleanupAndDispose(visual);
      base.Dispose();
    }

    void Init()
    {
      _visualProperty = new SProperty(typeof(FrameworkElement), null);
      _autoLayoutContentProperty = new SProperty(typeof(bool), true);
    }

    void Attach()
    {
      _visualProperty.Attach(OnVisualChanged);
    }

    void Detach()
    {
      _visualProperty.Detach(OnVisualChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      VisualBrush b = (VisualBrush) source;
      Visual = b.Visual; // Use the original Visual, copying isn't necessary
      AutoLayoutContent = b.AutoLayoutContent;
      Attach();
    }

    #endregion

    void OnVisualChanged(AbstractProperty prop, object oldVal)
    {
      PrepareVisual();
    }

    void UpdateRenderTarget(FrameworkElement fe)
    {
      fe.RenderToSurface(_visualSurface, new RenderContext(Matrix.Identity, Matrix.Identity, Opacity,
          new RectangleF(new PointF(0.0f, 0.0f), _vertsBounds.Size), 1.0f));

      // Unfortunately, brushes/brush effects are based on textures and cannot work with surfaces, so we need this additional copy step
      GraphicsDevice.Device.StretchRectangle(
          _visualSurface.Surface, new Rectangle(Point.Empty, _visualSurface.Size),
          _visualTexture.Surface0, new Rectangle(Point.Empty,  _visualTexture.Size),
          TextureFilter.None);
    }

    protected void PrepareVisual()
    {
      FrameworkElement visual = Visual;
      if (_preparedVisual != null && _preparedVisual != visual)
      {
        _preparedVisual.SetElementState(ElementState.Available);
        _preparedVisual.Deallocate();
        _preparedVisual = null;
      }
      if (_screen == null)
        return;
      if (visual == null)
        return;
      if (AutoLayoutContent)
      {
        // We must bypass normal layout or the visual will be layed out to screen/skin size
        visual.SetScreen(_screen);
        if (visual.ElementState == ElementState.Available)
          visual.SetElementState(ElementState.Running);
        // Here is _screen != null, which means we are allocated
        visual.Allocate();
        SizeF size = _vertsBounds.Size;
        visual.Measure(ref size);
        visual.Arrange(new RectangleF(new PointF(0, 0), _vertsBounds.Size));
      }
      _preparedVisual = visual;
    }

    #region Public properties

    public AbstractProperty VisualProperty
    {
      get { return _visualProperty; }
    }

    public FrameworkElement Visual
    {
      get { return (FrameworkElement) _visualProperty.GetValue(); }
      set { _visualProperty.SetValue(value); }
    }

    public AbstractProperty AutoLayoutContentProperty
    {
      get { return _autoLayoutContentProperty; }
    }

    public bool AutoLayoutContent
    {
      get { return (bool) _autoLayoutContentProperty.GetValue(); }
      set { _autoLayoutContentProperty.SetValue(value); }
    }

    public override Texture Texture
    {
      get { return _visualTexture.Texture; }
    }

    protected override Vector2 BrushDimensions
    {
      get { return (_visualTexture != null) ? new Vector2(_visualTexture.Width, _visualTexture.Height) : new Vector2(1.0f, 1.0f); }
    }

    protected override Vector2 TextureMaxUV
    {
      get { return (_visualTexture != null) ? new Vector2(_visualTexture.MaxU, _visualTexture.MaxV) : new Vector2(1.0f, 1.0f); }
    }

    #endregion

    public override void SetupBrush(FrameworkElement parent, ref PositionColoredTextured[] verts, float zOrder, bool adaptVertsToBrushTexture)
    {
      base.SetupBrush(parent, ref verts, zOrder, adaptVertsToBrushTexture);
      _visualTexture = ContentManager.Instance.GetRenderTexture(_renderTextureKey);
      _visualSurface = ContentManager.Instance.GetRenderTarget(_renderSurfaceKey);
      _screen = parent.Screen;
      PrepareVisual();
    }

    protected override bool BeginRenderBrushOverride(PrimitiveBuffer primitiveContext, RenderContext renderContext)
    {
      FrameworkElement fe = _preparedVisual;
      if (fe == null) return false;
      _visualTexture.AllocateRenderTarget((int) _vertsBounds.Width, (int) _vertsBounds.Height);
      _visualSurface.AllocateRenderTarget((int) _vertsBounds.Width, (int) _vertsBounds.Height);

      UpdateRenderTarget(fe);
      return base.BeginRenderBrushOverride(primitiveContext, renderContext);
    }

    protected override bool BeginRenderOpacityBrushOverride(Texture tex, RenderContext renderContext)
    {
      FrameworkElement fe = _preparedVisual;
      if (fe == null)
        return false;

      UpdateRenderTarget(fe);
      return base.BeginRenderOpacityBrushOverride(tex, renderContext);
    }

    public override void Allocate()
    {
      base.Allocate();
      FrameworkElement fe = _preparedVisual;
      if (fe != null)
        fe.Allocate();
    }

    public override void Deallocate()
    {
      base.Allocate();
      FrameworkElement fe = _preparedVisual;
      if (fe != null)
        fe.Deallocate();
    }
  }
}

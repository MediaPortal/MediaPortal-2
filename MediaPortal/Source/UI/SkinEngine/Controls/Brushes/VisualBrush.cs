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
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.Utilities.DeepCopy;
using SharpDX;
using SharpDX.Direct3D9;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public class VisualBrush : TileBrush
  {
    #region Protected fields

    protected AbstractProperty _visualProperty;
    protected AbstractProperty _autoLayoutContentProperty;
    protected RenderTextureAsset _visualTexture = null;
    protected Screen _screen = null;
    protected SizeF _visualSize = new SizeF();
    protected FrameworkElement _preparedVisual = null;
    protected String _renderTextureKey;
    protected static int _visualBrushId = 0;

    #endregion

    #region Ctor

    public VisualBrush()
    {
      _visualBrushId++;
      _renderTextureKey = String.Format("VisualBrush RenderTexture #{0}", _visualBrushId);
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
      Visual = copyManager.GetCopy(b.Visual); // Copy visual, as same could be used multiple times
      AutoLayoutContent = b.AutoLayoutContent;
      Attach();
    }

    #endregion

    void OnVisualChanged(AbstractProperty prop, object oldValue)
    {
      PrepareVisual();
    }

    void UpdateRenderTarget(FrameworkElement fe)
    {
      // We need to consider a special case for rendering Opacity masks: in this case the alpha blending is enabled on device already. If we render now without changes,
      // the Visual will not be visible in target texture. In this case we switch back to normal rendering mode and restore blending mode after the Visual is rendered.
      var wasBlendingEnabled = GraphicsDevice.IsAlphaChannelBlendingEnabled;
      if (wasBlendingEnabled)
      {
        // Opposite steps as done inside FrameworkElement.RenderOpacityBrush
        GraphicsDevice.DisableAlphaChannelBlending();
        GraphicsDevice.EnableAlphaTest();
      }
      RectangleF bounds = new RectangleF(0, 0, _vertsBounds.Size.Width, _vertsBounds.Size.Height);
      fe.RenderToTexture(_visualTexture, new RenderContext(Matrix.Identity, Opacity, bounds, 1.0f));
      if (wasBlendingEnabled)
      {
        // Redo steps as done inside FrameworkElement.RenderOpacityBrush
        GraphicsDevice.EnableAlphaChannelBlending();
        GraphicsDevice.DisableAlphaTest();
      }
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
        visual.Arrange(new RectangleF(0, 0, _vertsBounds.Size.Width, _vertsBounds.Size.Height));
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
      _screen = parent.Screen;
      PrepareVisual();
    }

    protected override bool BeginRenderBrushOverride(PrimitiveBuffer primitiveContext, RenderContext renderContext)
    {
      FrameworkElement fe = _preparedVisual;
      if (fe == null) return false;
      _visualTexture.AllocateRenderTarget((int) _vertsBounds.Width, (int) _vertsBounds.Height);

      UpdateRenderTarget(fe);

      return base.BeginRenderBrushOverride(primitiveContext, renderContext);
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

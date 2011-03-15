#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
    protected RenderTextureAsset _textureVisual = null;
    protected Screen _screen = null;
    protected SizeF _visualSize = Size.Empty;
    protected FrameworkElement _preparedVisual = null;
    protected String _renderTextureKey;
    protected static int _visualBrushId = 0;

    #endregion

    #region Ctor

    public VisualBrush()
    {
      ++_visualBrushId;
      _renderTextureKey = String.Format("VisualBrush RenderTexture #{0}", _visualBrushId);

      Init();
      Attach();
    }

    public override void Dispose()
    {
      FrameworkElement visual = Visual;
      if (visual != null)
        Registration.TryCleanupAndDispose(visual);
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

    #endregion

    public override void SetupBrush(FrameworkElement parent, ref PositionColoredTextured[] verts, float zOrder, bool adaptVertsToBrushTexture)
    {
      base.SetupBrush(parent, ref verts, zOrder, adaptVertsToBrushTexture);
      _textureVisual = ServiceRegistration.Get<ContentManager>().GetRenderTexture(_renderTextureKey);
      _screen = parent.Screen;
      PrepareVisual();
    }

    public override bool BeginRenderBrush(PrimitiveBuffer primitiveContext, RenderContext renderContext)
    {
      if (_preparedVisual == null) return false;
      _textureVisual.AllocateRenderTarget((int) _vertsBounds.Width, (int) _vertsBounds.Height);

      UpdateRenderTarget(renderContext);
      base.BeginRenderBrush(primitiveContext, renderContext);

      return true;
    }

    public override void BeginRenderOpacityBrush(Texture tex, RenderContext renderContext)
    {
      if (_preparedVisual == null) return;

      UpdateRenderTarget(renderContext);
      base.BeginRenderOpacityBrush(tex, renderContext);
    }

    void UpdateRenderTarget(RenderContext renderContext)
    {
      _preparedVisual.RenderToTexture(_textureVisual, new RenderContext(Matrix.Identity, Matrix.Identity, Opacity, new RectangleF(new PointF(0.0f, 0.0f), _vertsBounds.Size), 1.0f));
    }

    public override Texture Texture
    {
      get { return _textureVisual.Texture; }
    }

    protected override Vector2 BrushDimensions
    {
      get { return (_textureVisual != null) ? new Vector2(_textureVisual.Width, _textureVisual.Height) : new Vector2(1.0f, 1.0f); }
    }

    protected override Vector2 TextureMaxUV
    {
      get { return (_textureVisual != null) ? new Vector2(_textureVisual.MaxU, _textureVisual.MaxV) : new Vector2(1.0f, 1.0f); }
    }
  }
}

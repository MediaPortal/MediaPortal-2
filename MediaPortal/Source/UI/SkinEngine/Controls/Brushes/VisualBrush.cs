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
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Effects;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public class VisualBrush : TileBrush
  {
    #region Protected fields

    protected AbstractProperty _visualProperty;
    protected AbstractProperty _autoLayoutContentProperty;
    protected EffectAsset _effect;
    protected Texture _textureVisual;
    protected Screen _screen = null;
    protected FrameworkElement _preparedVisual = null;


    #endregion

    #region Ctor

    public VisualBrush()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _visualProperty = new SProperty(typeof(FrameworkElement), null);
      _autoLayoutContentProperty = new SProperty(typeof(bool), true);
      _effect = ContentManager.GetEffect("normal");
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
      if (_screen == null)
        return;
      FrameworkElement visual = Visual;
      if (visual == null)
        return;
      if (AutoLayoutContent)
      {
        visual.SetScreen(_screen);
        visual.UpdateLayout();
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

    public override void SetupBrush(FrameworkElement parent, ref PositionColored2Textured[] verts, float zOrder, bool adaptVertsToBrushTexture)
    {
      base.SetupBrush(parent, ref verts, zOrder, adaptVertsToBrushTexture);
      _textureVisual = new Texture(GraphicsDevice.Device, (int) _vertsBounds.Width, (int ) _vertsBounds.Height, 1,
          Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
      _screen = parent.Screen;
      if (_preparedVisual == null)
        PrepareVisual();
    }

    public override bool BeginRenderBrush(PrimitiveContext primitiveContext, RenderContext renderContext)
    {
      // TODO: Implement and use method in TileBrush
      FrameworkElement visual = _preparedVisual;
      if (visual == null) return false;

      Matrix finalTransform = renderContext.Transform.Clone();

      // TODO: Handle properties of TileBrush
      // TODO: Handle RelativeTransform, Transform

      RenderContext tempRenderContext = renderContext.Derive(_vertsBounds, null, null,
          new Vector2(0.5f, 0.5f), Opacity);

      visual.RenderToTexture(_textureVisual, tempRenderContext);

      // Now render our texture
      _effect.StartRender(_textureVisual, finalTransform);

      return true;
    }

    public override void BeginRenderOpacityBrush(Texture tex, RenderContext renderContext)
    {
      // TODO: Create method in TileBrush to render an image as opacity brush and use that method here
      throw new NotImplementedException();
    }

    public override void EndRender()
    {
      // TODO: Create method in TileBrush and call from here
    }

  }
}

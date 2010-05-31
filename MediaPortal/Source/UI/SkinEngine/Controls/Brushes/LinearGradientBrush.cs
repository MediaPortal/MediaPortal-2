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
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Effects;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Rendering;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  // TODO: Implement Freezable behaviour
  public class LinearGradientBrush : GradientBrush
  {
    #region Private fields

    EffectAsset _effect;

    AbstractProperty _startPointProperty;
    AbstractProperty _endPointProperty;
    EffectHandleAsset _handleTransform;
    EffectHandleAsset _handleOpacity;
    EffectHandleAsset _handleStartPoint;
    EffectHandleAsset _handleEndPoint;
    EffectHandleAsset _handleSolidColor;
    EffectHandleAsset _handleAlphaTexture;
    EffectHandleAsset _handleUpperVertsBounds;
    EffectHandleAsset _handleLowerVertsBounds;
    GradientBrushTexture _gradientBrushTexture;
    float[] g_startpoint;
    float[] g_endpoint;
    bool _refresh = false;

    #endregion

    #region Ctor

    public LinearGradientBrush()
    {
      Init();
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
    }

    void Init()
    {
      _startPointProperty = new SProperty(typeof(Vector2), new Vector2(0.0f, 0.0f));
      _endPointProperty = new SProperty(typeof(Vector2), new Vector2(1.0f, 1.0f));
    }

    void Attach()
    {
      _startPointProperty.Attach(OnPropertyChanged);
      _endPointProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _startPointProperty.Detach(OnPropertyChanged);
      _endPointProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      LinearGradientBrush b = (LinearGradientBrush) source;
      StartPoint = copyManager.GetCopy(b.StartPoint);
      EndPoint = copyManager.GetCopy(b.EndPoint);
      Attach();
    }

    #endregion

    protected override void OnPropertyChanged(AbstractProperty prop, object oldValue)
    {
      _refresh = true;
      FireChanged();
    }

    public AbstractProperty StartPointProperty
    {
      get { return _startPointProperty; }
    }

    public Vector2 StartPoint
    {
      get { return (Vector2) _startPointProperty.GetValue(); }
      set { _startPointProperty.SetValue(value); }
    }

    public AbstractProperty EndPointProperty
    {
      get { return _endPointProperty; }
    }

    public Vector2 EndPoint
    {
      get { return (Vector2) _endPointProperty.GetValue(); }
      set { _endPointProperty.SetValue(value); }
    }

    public override void SetupBrush(FrameworkElement parent, ref PositionColored2Textured[] verts, float zOrder, bool adaptVertsToBrushTexture)
    {
      base.SetupBrush(parent, ref verts, zOrder, adaptVertsToBrushTexture);

      if (_gradientBrushTexture == null)
        _gradientBrushTexture = BrushCache.Instance.GetGradientBrush(GradientStops);
      _refresh = true;
    }

    public override bool BeginRenderBrush(PrimitiveContext primitiveContext, RenderContext renderContext)
    {
      if (_gradientBrushTexture == null)
        return false;
      Matrix finalTransform = renderContext.Transform.Clone();
      if (_refresh)
      {
        _refresh = false;
        CheckSingleColor();
        if (_singleColor)
        {
          _effect = ContentManager.GetEffect("solidbrush");
          _handleSolidColor = _effect.GetParameterHandle("g_solidColor");
        }
        else
        {
          _effect = ContentManager.GetEffect("lineargradient");
          _handleTransform = _effect.GetParameterHandle("Transform");
          _handleOpacity = _effect.GetParameterHandle("g_opacity");
          _handleStartPoint = _effect.GetParameterHandle("g_StartPoint");
          _handleEndPoint = _effect.GetParameterHandle("g_EndPoint");
        }

        g_startpoint = new float[] {StartPoint.X, StartPoint.Y};
        g_endpoint = new float[] {EndPoint.X, EndPoint.Y};
        if (MappingMode == BrushMappingMode.Absolute)
        {
          g_startpoint[0] /= _vertsBounds.Width;
          g_startpoint[1] /= _vertsBounds.Height;

          g_endpoint[0] /= _vertsBounds.Width;
          g_endpoint[1] /= _vertsBounds.Height;
        }
        if (RelativeTransform != null)
        {
          Matrix m = RelativeTransform.GetTransform();
          m.Transform(ref g_startpoint[0], ref g_startpoint[1]);
          m.Transform(ref g_endpoint[0], ref g_endpoint[1]);
        }
      }

      if (_singleColor)
      {
        Color4 v = ColorConverter.FromColor(Color.FromArgb((int) (255*Opacity*renderContext.Opacity), GradientStops[0].Color));
        _handleSolidColor.SetParameter(v);
        _effect.StartRender(finalTransform);
      }
      else
      {
        _handleTransform.SetParameter(GetCachedFinalBrushTransform());
        _handleOpacity.SetParameter((float) (Opacity * renderContext.Opacity));
        _handleStartPoint.SetParameter(g_startpoint);
        _handleEndPoint.SetParameter(g_endpoint);
        _effect.StartRender(_gradientBrushTexture.Texture, finalTransform);
      }
      return true;
    }

    public override void BeginRenderOpacityBrush(Texture tex, RenderContext renderContext)
    {
      if (tex == null)
        return;
      Matrix finalTransform = renderContext.Transform.Clone();
      if (_refresh)
      {
        _refresh = false;
        CheckSingleColor();
        _effect = ContentManager.GetEffect("linearopacitygradient");
        _handleTransform = _effect.GetParameterHandle("Transform");
        _handleOpacity = _effect.GetParameterHandle("g_opacity");
        _handleStartPoint = _effect.GetParameterHandle("g_StartPoint");
        _handleEndPoint = _effect.GetParameterHandle("g_EndPoint");
        _handleAlphaTexture = _effect.GetParameterHandle("g_alphatex");
        _handleUpperVertsBounds = _effect.GetParameterHandle("g_UpperVertsBounds");
        _handleLowerVertsBounds = _effect.GetParameterHandle("g_LowerVertsBounds");

        g_startpoint = new float[] {StartPoint.X, StartPoint.Y};
        g_endpoint = new float[] {EndPoint.X, EndPoint.Y};
        if (MappingMode == BrushMappingMode.Absolute)
        {
          g_startpoint[0] /= _vertsBounds.Width;
          g_startpoint[1] /= _vertsBounds.Height;

          g_endpoint[0] /= _vertsBounds.Width;
          g_endpoint[1] /= _vertsBounds.Height;
        }

        if (RelativeTransform != null)
        {
          Matrix m = RelativeTransform.GetTransform();
          m.Transform(ref g_startpoint[0], ref g_startpoint[1]);
          m.Transform(ref g_endpoint[0], ref g_endpoint[1]);
        }
      }
      if (_singleColor)
      {
        _handleOpacity.SetParameter((float) (Opacity * renderContext.Opacity));
        _effect.StartRender(tex, finalTransform);
      }
      else
      {
        SurfaceDescription desc = tex.GetLevelDescription(0);
        float[] g_LowerVertsBounds = new float[] {_vertsBounds.Left / desc.Width, _vertsBounds.Top / desc.Height};
        float[] g_UpperVertsBounds = new float[] {_vertsBounds.Right / desc.Width, _vertsBounds.Bottom / desc.Height};

        _handleTransform.SetParameter(GetCachedFinalBrushTransform());
        _handleOpacity.SetParameter((float) (Opacity * renderContext.Opacity));
        _handleStartPoint.SetParameter(g_startpoint);
        _handleEndPoint.SetParameter(g_endpoint);
        _handleAlphaTexture.SetParameter(_gradientBrushTexture.Texture);
        _handleUpperVertsBounds.SetParameter(g_UpperVertsBounds);
        _handleLowerVertsBounds.SetParameter(g_LowerVertsBounds);

        _effect.StartRender(tex, finalTransform);
      }
    }

    public override void EndRender()
    {
      if (_effect != null)
        _effect.EndRender();
    }

    public override Texture Texture
    {
      get { return _gradientBrushTexture.Texture; }
    }
  }
}

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

using System.Collections.Generic;
using System.Diagnostics;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.Utilities.DeepCopy;
using SharpDX;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  /// <summary>
  /// <see cref="SimpleShaderEffect"/> provides a shader that allows setting the filename (<see cref="ShaderEffectName"/>) of a shader from XAML. 
  /// This way any parameterless .fx file can be used. Shaders that provide parameters should be handled with an own shader class 
  /// that exposes the parameters as properties.
  /// </summary>
  public class ShinyWave : ShaderEffect
  {
    #region Protected fields

    // Transparency
    protected AbstractProperty _transparencyProperty;
    protected float _transparency;

    // Backgroundcolor
    protected AbstractProperty _backgroundProperty;
    protected string _background;

    protected Dictionary<string, object> _effectParameters = new Dictionary<string, object>();

    #endregion

    private static Stopwatch fTime0_x;

    #region Ctor & maintainance

    public ShinyWave()
    {
      Init();
      Attach();
    }

    private void Init()
    {
      _shaderEffectName = "ShinyWave";
      _transparency = (float)1.0;
      _transparencyProperty = new SProperty(typeof(float), _transparency);

      _background = "#FF000000";
      _backgroundProperty = new SProperty(typeof(string), _background);

      fTime0_x = new Stopwatch();
      fTime0_x.Start();
    }

    private void Attach()
    {
      _transparencyProperty.Attach(OnPropertyChangedTransparency);
      _backgroundProperty.Attach(OnPropertyChangedBackground);
    }

    private void Detach()
    {
      _transparencyProperty.Detach(OnPropertyChangedTransparency);
      _backgroundProperty.Detach(OnPropertyChangedBackground);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ShinyWave el = (ShinyWave)source;
      Transparency = el.Transparency;
      Attach();
    }

    private void OnPropertyChangedTransparency(AbstractProperty property, object oldvalue)
    {
      _transparency = Transparency;
    }

    private void OnPropertyChangedBackground(AbstractProperty property, object oldvalue)
    {
      _background = Background;
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
      fTime0_x.Stop();
    }

    #endregion

    #region Properties

    public AbstractProperty TransaprencyProperty
    {
      get { return _transparencyProperty; }
    }

    public AbstractProperty BackgroundProperty
    {
      get { return _backgroundProperty; }
    }

    /// <summary>
    /// Gets or sets the name of the shader to use. A corresponding shader file must be present in the
    /// skin's shader directory (directory <c>shaders</c>). Only the file name without extension is required, folder name and
    /// <c>.fx</c> extension are added internally.
    /// </summary>
    public float Transparency
    {
      get { return (float)_transparencyProperty.GetValue(); }
      set { _transparencyProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the name of the shader to use. A corresponding shader file must be present in the
    /// skin's shader directory (directory <c>shaders</c>). Only the file name without extension is required, folder name and
    /// <c>.fx</c> extension are added internally.
    /// </summary>
    public string Background
    {
      get { return (string)_backgroundProperty.GetValue(); }
      set { _transparencyProperty.SetValue(value); }
    }

    #endregion

    protected override Dictionary<string, object> GetShaderParameters()
    {
      _effectParameters["time"] = (float)fTime0_x.Elapsed.Ticks / (float)100000000;
      if (fTime0_x.Elapsed.Seconds > 120)
      {
        fTime0_x.Reset();
        fTime0_x.Start();
      }
      // random hard coded values for mouse
      _effectParameters["mouse"] = (Vector2)new Vector2((float)0.001570, (float)0.0565619);
      //BUG: use right size
      _effectParameters["resolution"] = (Vector2)new Vector2( (float)GraphicsDevice.Width, (float)GraphicsDevice.Height);
      _effectParameters["matViewProjection"] = (Matrix)GraphicsDevice.TransformView;
      _effectParameters["transparency"] = Transparency;
      Color backgroundColor; //RGBA
      ColorConverter.ConvertColor(Background, out backgroundColor);
      _effectParameters["backgroundColor"] = backgroundColor.ToVector4();

      return _effectParameters;
    }
  }
}
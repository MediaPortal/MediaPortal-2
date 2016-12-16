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

using MediaPortal.Common.General;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  /// <summary>
  /// <see cref="SimpleShaderEffect"/> provides a shader that allows setting the filename (<see cref="ShaderEffectName"/>) of a shader from XAML. 
  /// This way any parameterless .fx file can be used. Shaders that provide parameters should be handled with an own shader class 
  /// that exposes the parameters as properties.
  /// </summary>
  public class SimpleShaderEffect : ShaderEffect
  {
    #region Protected fields

    protected AbstractProperty _shaderEffectNameProperty;
    
    #endregion

    #region Ctor & maintainance

    public SimpleShaderEffect()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _shaderEffectName = "normal";
      _shaderEffectNameProperty = new SProperty(typeof(string), _shaderEffectName);
    }

    void Attach()
    {
      _shaderEffectNameProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _shaderEffectNameProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      SimpleShaderEffect el = (SimpleShaderEffect) source;
      ShaderEffectName = el.ShaderEffectName;
      Attach();
    }

    private void OnPropertyChanged(AbstractProperty property, object oldvalue)
    {
      _shaderEffectName = ShaderEffectName;
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
    }
    
    #endregion

    #region Properties

    public AbstractProperty ShaderEffectNameProperty
    {
      get { return _shaderEffectNameProperty; }
    }

    /// <summary>
    /// Gets or sets the name of the shader to use. A corresponding shader file must be present in the
    /// skin's shader directory (directory <c>shaders</c>). Only the file name without extension is required, folder name and
    /// <c>.fx</c> extension are added internally.
    /// </summary>
    public string ShaderEffectName
    {
      get { return (string) _shaderEffectNameProperty.GetValue(); }
      set { _shaderEffectNameProperty.SetValue(value); }
    }

    #endregion
  }
}

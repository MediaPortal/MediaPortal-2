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
  public class GaussianBlur : ShaderEffect
  {
    #region Protected fields
    
    protected Dictionary<string, object> _effectParameters = new Dictionary<string, object>();

    #endregion

    #region Ctor & maintainance

    public GaussianBlur()
    {
      Init();
      Attach();
    }

    private void Init()
    {
      _shaderEffectName = "GaussianBlur";
    }

    private void Attach()
    {
      
    }

    private void Detach()
    {
      
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      GaussianBlur el = (GaussianBlur)source;
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
    }

    #endregion

    #region Properties

    

    #endregion

    protected override Dictionary<string, object> GetShaderParameters()
    {
      _effectParameters["matViewProjection"] = (Matrix)GraphicsDevice.TransformView;
      return _effectParameters;
    }
  }
}
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

using System.Collections.Generic;
using MediaPortal.Common.General;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  public class PixelateEffect : ImageEffect
  {
    #region Consts

    public const string EFFECT_PIXELATE = "pixelate";

    #endregion

    #region Protected fields

    protected AbstractProperty _horizonzalPixelCountProperty;
    protected AbstractProperty _verticalPixelCountProperty;

    protected Dictionary<string, object> _effectParameters = new Dictionary<string, object>();

    #endregion

    #region Ctor & maintainance

    public PixelateEffect()
    {
      _partialShaderEffect = EFFECT_PIXELATE;
      Init();
    }

    void Init()
    {
      _horizonzalPixelCountProperty = new SProperty(typeof(double), 80.0d);
      _verticalPixelCountProperty = new SProperty(typeof(double), 80.0d);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      PixelateEffect el = (PixelateEffect) source;
      HorizonzalPixelCount = el.HorizonzalPixelCount;
      VerticalPixelCount = el.VerticalPixelCount;
    }

    #endregion

    #region Properties

    public AbstractProperty HorizonzalPixelCountProperty
    {
      get { return _horizonzalPixelCountProperty; }
    }

    public double HorizonzalPixelCount
    {
      get { return (double) _horizonzalPixelCountProperty.GetValue(); }
      set { _horizonzalPixelCountProperty.SetValue(value); }
    }

    public AbstractProperty VerticalPixelCountProperty
    {
      get { return _verticalPixelCountProperty; }
    }

    public double VerticalPixelCount
    {
      get { return (double) _verticalPixelCountProperty.GetValue(); }
      set { _verticalPixelCountProperty.SetValue(value); }
    }

    #endregion

    protected override Dictionary<string, object> GetShaderParameters()
    {
      _effectParameters["g_horizontalPixelCounts"] = (float) HorizonzalPixelCount;
      _effectParameters["g_verticalPixelCounts"] = (float) VerticalPixelCount;
      return _effectParameters;
    }
  }
}

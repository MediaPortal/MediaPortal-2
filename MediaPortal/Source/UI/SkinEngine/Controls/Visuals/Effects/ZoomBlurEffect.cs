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
  public class ZoomBlurEffect : ImageEffect
  {
    #region Consts

    public const string EFFECT_BLUR = "zoom_blur";

    #endregion

    #region Protected fields

    protected AbstractProperty _centerXProperty;
    protected AbstractProperty _centerYProperty;
    protected AbstractProperty _blurAmountProperty;

    protected Dictionary<string, object> _effectParameters = new Dictionary<string, object>();

    #endregion

    #region Ctor & maintainance

    public ZoomBlurEffect()
    {
      _partialShaderEffect = EFFECT_BLUR;
      Init();
    }

    void Init()
    {
      _centerXProperty = new SProperty(typeof(double), 0.5);
      _centerYProperty = new SProperty(typeof(double), 0.5);
      _blurAmountProperty = new SProperty(typeof(double), 0.1);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ZoomBlurEffect el = (ZoomBlurEffect) source;
      CenterX = el.CenterX;
      CenterY = el.CenterY;
      BlurAmount = el.BlurAmount;
    }

    #endregion

    #region Properties

    public AbstractProperty CenterXProperty
    {
      get { return _centerXProperty; }
    }

    public double CenterX
    {
      get { return (double) _centerXProperty.GetValue(); }
      set { _centerXProperty.SetValue(value); }
    }

    public AbstractProperty CenterYProperty
    {
      get { return _centerYProperty; }
    }

    public double CenterY
    {
      get { return (double) _centerYProperty.GetValue(); }
      set { _centerYProperty.SetValue(value); }
    }

    public AbstractProperty BlurAmountProperty
    {
      get { return _blurAmountProperty; }
    }

    public double BlurAmount
    {
      get { return (double) _blurAmountProperty.GetValue(); }
      set { _blurAmountProperty.SetValue(value); }
    }

    #endregion

    protected override Dictionary<string, object> GetShaderParameters()
    {
      _effectParameters["g_centerX"] = (float) CenterX;
      _effectParameters["g_centerY"] = (float) CenterY;
      _effectParameters["g_blurAmount"] = (float) BlurAmount;
      return _effectParameters;
    }
  }
}

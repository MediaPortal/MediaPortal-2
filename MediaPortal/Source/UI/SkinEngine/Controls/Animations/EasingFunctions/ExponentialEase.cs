#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.UI.SkinEngine.Utils;
using MediaPortal.Utilities.DeepCopy;
using System;

namespace MediaPortal.UI.SkinEngine.Controls.Animations.EasingFunctions
{
  /// <summary>
  /// Easing function that gives an exponential curve.
  /// </summary>
  public class ExponentialEase : EasingFunctionBase
  {
    #region Protected fields

    protected AbstractProperty _exponentProperty;

    #endregion

    #region Ctor

    public ExponentialEase()
    {
      Init();
    }

    protected void Init()
    {
      _exponentProperty = new SProperty(typeof(double), 2.0);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ExponentialEase e = (ExponentialEase)source;
      Exponent = e.Exponent;
    }

    #endregion

    #region Public properties

    public AbstractProperty ExponentProperty
    {
      get { return _exponentProperty; }
    }

    /// <summary>
    /// Gets or sets the exponent.
    /// </summary>
    public double Exponent
    {
      get { return (double)_exponentProperty.GetValue(); }
      set { _exponentProperty.SetValue(value); }
    }

    #endregion

    #region EasingFunctionBase overrides

    protected override double EaseInCore(double normalizedTime)
    {
      double exponent = Exponent;
      return DoubleUtils.IsZero(exponent) ?
        normalizedTime : (Math.Exp(exponent * normalizedTime) - 1.0) / (Math.Exp(exponent) - 1.0);
    }

    #endregion
  }
}

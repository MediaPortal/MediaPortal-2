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
using MediaPortal.Utilities.DeepCopy;
using System;

namespace MediaPortal.UI.SkinEngine.Controls.Animations.EasingFunctions
{
  /// <summary>
  /// Easing function that gives a polynomial curve with an arbitrary power.
  /// </summary>
  public class PowerEase : EasingFunctionBase
  {
    #region Protected fields

    protected AbstractProperty _powerProperty;

    #endregion

    #region Ctor

    public PowerEase()
    {
      Init();
    }

    protected void Init()
    {
      _powerProperty = new SProperty(typeof(double), 2.0);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      PowerEase e = (PowerEase)source;
      Power = e.Power;
    }

    #endregion

    #region Public properties

    public AbstractProperty PowerProperty
    {
      get { return _powerProperty; }
    }

    /// <summary>
    /// Gets or sets the power of the polynomial equation.
    /// </summary>
    public double Power
    {
      get { return (double)_powerProperty.GetValue(); }
      set { _powerProperty.SetValue(value); }
    }

    #endregion

    #region EasingFunctionBase overrides

    protected override double EaseInCore(double normalizedTime)
    {
      double power = Math.Max(0.0, Power);
      return Math.Pow(normalizedTime, power);
    }

    #endregion
  }
}

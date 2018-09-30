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
  /// Easing function that provides an elasticy curve towards the destination.
  /// </summary>
  public class ElasticEase : EasingFunctionBase
  {
    #region Protected fields

    protected AbstractProperty _oscillationsProperty;
    protected AbstractProperty _springinessProperty;

    #endregion

    #region Ctor

    public ElasticEase()
    {
      Init();
    }

    protected void Init()
    {
      _oscillationsProperty = new SProperty(typeof(int), 3);
      _springinessProperty = new SProperty(typeof(double), 3.0);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ElasticEase e = (ElasticEase)source;
      Oscillations = e.Oscillations;
      Springiness = e.Springiness;
    }

    #endregion

    #region Public properties

    public AbstractProperty OscillationsProperty
    {
      get { return _oscillationsProperty; }
    }

    /// <summary>
    /// Gets or sets the number of oscillations.
    /// </summary>
    public int Oscillations
    {
      get { return (int)_oscillationsProperty.GetValue(); }
      set { _oscillationsProperty.SetValue(value); }
    }

    public AbstractProperty SpringinessProperty
    {
      get { return _springinessProperty; }
    }

    /// <summary>
    /// Gets or sets the springiness.
    /// </summary>
    public double Springiness
    {
      get { return (double)_springinessProperty.GetValue(); }
      set { _springinessProperty.SetValue(value); }
    }

    #endregion

    #region EasingFunctionBase overrides

    protected override double EaseInCore(double normalizedTime)
    {
      double oscillations = Math.Max(0.0, Oscillations);
      double springiness = Math.Max(0.0, Springiness);
      double exponent = DoubleUtils.IsZero(springiness) ?
        normalizedTime : (Math.Exp(springiness * normalizedTime) - 1.0) / (Math.Exp(springiness) - 1.0);
      return exponent * (Math.Sin((Math.PI * 2.0 * oscillations + Math.PI * 0.5) * normalizedTime));
    }

    #endregion
  }
}

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
  /// Easing function that backs up before going to the destination.
  /// </summary>
  public class BackEase : EasingFunctionBase
  {
    #region Protected fields

    protected AbstractProperty _amplitudeProperty;

    #endregion

    #region Ctor

    public BackEase()
    {
      Init();
    }

    protected void Init()
    {
      _amplitudeProperty = new SProperty(typeof(double), 1.0);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      BackEase e = (BackEase)source;
      Amplitude = e.Amplitude;
    }

    #endregion

    #region Public properties

    public AbstractProperty AmplitudeProperty
    {
      get { return _amplitudeProperty; }
    }

    /// <summary>
    /// Gets or sets how much the function will pull back.
    /// </summary>
    public double Amplitude
    {
      get { return (double)_amplitudeProperty.GetValue(); }
      set { _amplitudeProperty.SetValue(value); }
    }

    #endregion

    #region EasingFunctionBase overrides

    protected override double EaseInCore(double normalizedTime)
    {
      double amp = Math.Max(0.0, Amplitude);
      return Math.Pow(normalizedTime, 3.0) - normalizedTime * amp * Math.Sin(Math.PI * normalizedTime);
    }

    #endregion
  }
}

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
  /// Easing function that bounces to the destination.
  /// </summary>
  public class BounceEase : EasingFunctionBase
  {
    #region Protected fields

    protected AbstractProperty _bouncesProperty;
    protected AbstractProperty _bouncinessProperty;

    #endregion

    #region Ctor

    public BounceEase()
    {
      Init();
    }

    protected void Init()
    {
      _bouncesProperty = new SProperty(typeof(int), 3);
      _bouncinessProperty = new SProperty(typeof(double), 2.0);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      BounceEase e = (BounceEase)source;
      Bounces = e.Bounces;
      Bounciness = e.Bounciness;
    }

    #endregion

    #region Public properties

    public AbstractProperty BouncesProperty
    {
      get { return _bouncesProperty; }
    }

    /// <summary>
    /// Gets or sets the number of bounces.
    /// </summary>
    public int Bounces
    {
      get { return (int)_bouncesProperty.GetValue(); }
      set { _bouncesProperty.SetValue(value); }
    }

    public AbstractProperty BouncinessProperty
    {
      get { return _bouncinessProperty; }
    }

    /// <summary>
    /// Gets or sets the bounciness.
    /// </summary>
    public double Bounciness
    {
      get { return (double)_bouncinessProperty.GetValue(); }
      set { _bouncinessProperty.SetValue(value); }
    }

    #endregion

    #region EasingFunctionBase overrides

    protected override double EaseInCore(double normalizedTime)
    {
      double bounces = Math.Max(0.0, (double)Bounces);
      double bounciness = Bounciness;

      //Anything less than one will cause a DivideByZeroException
      if (bounciness < 1.0 || DoubleUtils.IsOne(bounciness))
        // Just a bit more than one to get (almost) the same effect without dividing by zero
        bounciness = 1.001;

      double bouncinessCompliment = 1.0 - bounciness;
      double power = Math.Pow(bounciness, bounces);
      
      //Get the number of units, where the first bounce has a width of 1 unit, using a geometric series plus half a unit
      //for the final bounce
      double totalUnits = (1.0 - power) / bouncinessCompliment + power * 0.5;
      //The unit we are currently in
      double currentUnit = normalizedTime * totalUnits;

      // Which bounce are we in, based on current unit
      double currentBounce = Math.Log(-currentUnit * (1.0 - bounciness) + 1.0, bounciness);
      double start = Math.Floor(currentBounce);
      double end = start + 1.0;

      //Convert the start and end of the bounce into time
      double startTime = (1.0 - Math.Pow(bounciness, start)) / (bouncinessCompliment * totalUnits);
      double endTime = (1.0 - Math.Pow(bounciness, end)) / (bouncinessCompliment * totalUnits);

      //Create a curve to fit the bounce
      double peakTime = (startTime + endTime) * 0.5;
      double timeToPeak = normalizedTime - peakTime;
      double radius = peakTime - startTime;
      double amplitude = Math.Pow(1.0 / bounciness, (bounces - start));

      //Quadratic curve to match the start, end and peak
      return (-amplitude / (radius * radius)) * (timeToPeak - radius) * (timeToPeak + radius);
    }

    #endregion
  }
}

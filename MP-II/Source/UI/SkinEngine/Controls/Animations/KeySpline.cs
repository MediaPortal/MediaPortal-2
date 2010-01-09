#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using SlimDX;

namespace MediaPortal.UI.SkinEngine.Controls.Animations
{
  public class KeySpline
  {
    #region Private fields

    private double _Bx;
    private double _By;
    private Vector2 _controlVector1;
    private Vector2 _controlVector2;
    private double _Cx;
    private double _Cx_Bx;
    private double _Cy;
    private bool _isDirty;
    private bool _isSpecified;
    private double _parameter;
    private double _three_Cx;
    private const double accuracy = 0.001;
    private const double fuzz = 1E-06;

    #endregion

    #region Ctor

    public KeySpline()
    {
      ControlVector1 = new Vector2(0.0f, 0.0f);
      ControlVector2 = new Vector2(1.0f, 1.0f);
    }

    public KeySpline(Vector2 controlVector1, Vector2 controlVector2)
    {
      ControlVector1 = controlVector1;
      ControlVector2 = controlVector2;
      _isDirty = true;
    }

    public KeySpline(double x1, double y1, double x2, double y2):
        this(new Vector2((float) x1, (float) y1), new Vector2((float) x2, (float) y2))
    {
    }

    #endregion

    #region Private methods

    private void Build()
    {
      if ((_controlVector1 == new Vector2(0.0f, 0.0f)) && (_controlVector2 == new Vector2(1.0f, 1.0f)))
        _isSpecified = false;
      else
      {
        _isSpecified = true;
        _parameter = 0.0;
        _Bx = 3.0 * _controlVector1.X;
        _Cx = 3.0 * _controlVector2.X;
        _Cx_Bx = 2.0 * (_Cx - _Bx);
        _three_Cx = 3.0 - _Cx;
        _By = 3.0 * _controlVector1.Y;
        _Cy = 3.0 * _controlVector2.Y;
      }
      _isDirty = false;
    }

    private static double GetBezierValue(double b, double c, double t)
    {
      double num = 1.0 - t;
      double num2 = t * t;
      return (((((b * t) * num) * num) + ((c * num2) * num)) + (num2 * t));
    }


    public double GetSplineProgress(double linearProgress)
    {
      if (_isDirty)
        Build();
      if (!_isSpecified)
        return linearProgress;
      SetParameterFromX(linearProgress);
      return GetBezierValue(_By, _Cy, _parameter);
    }

    private void GetXAndDx(double t, out double x, out double dx)
    {
      double num = 1.0 - t;
      double num2 = t * t;
      double num3 = num * num;
      x = (((_Bx * t) * num3) + ((_Cx * num2) * num)) + (num2 * t);
      dx = ((_Bx * num3) + ((_Cx_Bx * num) * t)) + (_three_Cx * num2);
    }


    private bool IsValidControlVector(Vector2 point)
    {
      return ((((point.X >= 0.0) && (point.X <= 1.0)) && (point.Y >= 0.0)) && (point.Y <= 1.0));
    }

    protected void OnChanged()
    {
      _isDirty = true;
    }

    private void SetParameterFromX(double time)
    {
      double num2 = 0.0;
      double num = 1.0;
      if (time == 0.0)
        _parameter = 0.0;
      else if (time == 1.0)
        _parameter = 1.0;
      else
      {
        while ((num - num2) > 1E-06)
        {
          double num4;
          double num6;
          GetXAndDx(_parameter, out num4, out num6);
          double num5 = Math.Abs(num6);
          if (num4 > time)
          {
            num = _parameter;
          }
          else
          {
            num2 = _parameter;
          }
          if (Math.Abs((double)(num4 - time)) < (0.001 * num5))
          {
            return;
          }
          if (num5 > 1E-06)
          {
            double num3 = _parameter - ((num4 - time) / num6);
            if (num3 >= num)
            {
              _parameter = (_parameter + num) / 2.0;
            }
            else if (num3 <= num2)
            {
              _parameter = (_parameter + num2) / 2.0;
            }
            else
            {
              _parameter = num3;
            }
          }
          else
          {
            _parameter = (num2 + num) / 2.0;
          }
        }
      }
    }

    #endregion

    #region Public properties

    public Vector2 ControlVector1
    {
      get { return _controlVector1; }
      set
      {
        if (value != _controlVector1)
        {
          if (!IsValidControlVector(value))
            throw new ArgumentException(string.Format("Invalid control vector 1 '{0}'", value == null ? "null" : value.ToString()));
          _controlVector1 = value;
        }
      }
    }

    public Vector2 ControlVector2
    {
      get { return _controlVector2; }
      set
      {
        if (value != _controlVector2)
        {
          if (!IsValidControlVector(value))
            throw new ArgumentException(string.Format("Invalid control vector 2 '{0}'", value == null ? "null" : value.ToString()));
          _controlVector2 = value;
        }
      }
    }

    #endregion

  }
}


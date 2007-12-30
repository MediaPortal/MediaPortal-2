#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals;
using Microsoft.DirectX;

namespace SkinEngine.Controls.Animations
{
  public class KeySpline
  {
    // Fields
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

    // Methods
    public KeySpline()
    {
      this._controlVector1 = new Vector2(0.0f, 0.0f);
      this._controlVector2 = new Vector2(1.0f, 1.0f);
    }

    public KeySpline(Vector2 controlVector21, Vector2 controlVector22)
    {
      if (!this.IsValidControlVector2(controlVector21))
      {
        throw new ArgumentException("");

      }
      if (!this.IsValidControlVector2(controlVector22))
      {
        throw new ArgumentException("");
      }
      this._controlVector1 = controlVector21;
      this._controlVector2 = controlVector22;
      this._isDirty = true;
    }

    public KeySpline(double x1, double y1, double x2, double y2)
      : this(new Vector2((float)x1, (float)y1), new Vector2((float)x2, (float)y2))
    {
    }

    private void Build()
    {
      if ((this._controlVector1 == new Vector2(0.0f, 0.0f)) && (this._controlVector2 == new Vector2(1.0f, 1.0f)))
      {
        this._isSpecified = false;
      }
      else
      {
        this._isSpecified = true;
        this._parameter = 0.0;
        this._Bx = 3.0 * this._controlVector1.X;
        this._Cx = 3.0 * this._controlVector2.X;
        this._Cx_Bx = 2.0 * (this._Cx - this._Bx);
        this._three_Cx = 3.0 - this._Cx;
        this._By = 3.0 * this._controlVector1.Y;
        this._Cy = 3.0 * this._controlVector2.Y;
      }
      this._isDirty = false;
    }


    private static double GetBezierValue(double b, double c, double t)
    {
      double num = 1.0 - t;
      double num2 = t * t;
      return (((((b * t) * num) * num) + ((c * num2) * num)) + (num2 * t));
    }


    public double GetSplineProgress(double linearProgress)
    {
      if (this._isDirty)
      {
        this.Build();
      }
      if (!this._isSpecified)
      {
        return linearProgress;
      }
      this.SetParameterFromX(linearProgress);
      return GetBezierValue(this._By, this._Cy, this._parameter);
    }

    private void GetXAndDx(double t, out double x, out double dx)
    {
      double num = 1.0 - t;
      double num2 = t * t;
      double num3 = num * num;
      x = (((this._Bx * t) * num3) + ((this._Cx * num2) * num)) + (num2 * t);
      dx = ((this._Bx * num3) + ((this._Cx_Bx * num) * t)) + (this._three_Cx * num2);
    }


    private bool IsValidControlVector2(Vector2 point)
    {
      return ((((point.X >= 0.0) && (point.X <= 1.0)) && (point.Y >= 0.0)) && (point.Y <= 1.0));
    }

    protected void OnChanged()
    {
      this._isDirty = true;
    }

    private void SetParameterFromX(double time)
    {
      double num2 = 0.0;
      double num = 1.0;
      if (time == 0.0)
      {
        this._parameter = 0.0;
      }
      else if (time == 1.0)
      {
        this._parameter = 1.0;
      }
      else
      {
        while ((num - num2) > 1E-06)
        {
          double num4;
          double num6;
          this.GetXAndDx(this._parameter, out num4, out num6);
          double num5 = Math.Abs(num6);
          if (num4 > time)
          {
            num = this._parameter;
          }
          else
          {
            num2 = this._parameter;
          }
          if (Math.Abs((double)(num4 - time)) < (0.001 * num5))
          {
            return;
          }
          if (num5 > 1E-06)
          {
            double num3 = this._parameter - ((num4 - time) / num6);
            if (num3 >= num)
            {
              this._parameter = (this._parameter + num) / 2.0;
            }
            else if (num3 <= num2)
            {
              this._parameter = (this._parameter + num2) / 2.0;
            }
            else
            {
              this._parameter = num3;
            }
          }
          else
          {
            this._parameter = (num2 + num) / 2.0;
          }
        }
      }
    }
    // Properties
    public Vector2 ControlVector21
    {
      get
      {
        return this._controlVector1;
      }
      set
      {
        if (value != this._controlVector1)
        {
          if (!this.IsValidControlVector2(value))
          {
            throw new ArgumentException("");
          }
          this._controlVector1 = value;
        }
      }
    }

    public Vector2 ControlVector22
    {
      get
      {
        return this._controlVector2;
      }
      set
      {
        if (value != this._controlVector2)
        {
          if (!this.IsValidControlVector2(value))
          {
            throw new ArgumentException("");
          }
          this._controlVector2 = value;
        }
      }
    }
  }
}


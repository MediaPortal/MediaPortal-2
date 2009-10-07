#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *  Copyright (C) 2005-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;

namespace UPnP.Infrastructure.CP.DeviceTree
{
  /// <summary>
  /// Defines the range of allowed numeric values for a UPnP state variable.
  /// </summary>
  public class CpAllowedValueRange
  {
    protected double _minValue;
    protected double _maxValue;
    protected double? _step;

    public CpAllowedValueRange(double minValue, double maxValue, double? step)
    {
      _minValue = minValue;
      _maxValue = maxValue;
      _step = step;
    }

    public double MinValue
    {
      get { return _minValue; }
    }

    public double MaxValue
    {
      get { return _maxValue; }
    }

    public double? Step
    {
      get { return _step; }
    }

    public bool IsValueInRange(object value)
    {
      double doubleVal = (double) Convert.ChangeType(value, typeof(double));
      if (doubleVal < _minValue || doubleVal > _maxValue)
        return false;
      if (_step.HasValue)
      {
        double n = (doubleVal - _minValue) / _step.Value;
        return (n - (int) n) < 0.001;
      }
      else
        return true;
    }
  }
}

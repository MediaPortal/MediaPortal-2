#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System;
using System.Xml;
using System.Xml.XPath;
using UPnP.Infrastructure.Utils;

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
      return true;
    }

    #region Connection

    internal static CpAllowedValueRange CreateAllowedValueRange(XPathNavigator allowedValueRangeElementNav, IXmlNamespaceResolver nsmgr)
    {
      XPathNodeIterator minIt = allowedValueRangeElementNav.Select("s:minimum", nsmgr);
      if (!minIt.MoveNext())
        return null;
      double min = Convert.ToDouble(ParserHelper.SelectText(minIt.Current, "text()", null));
      XPathNodeIterator maxIt = allowedValueRangeElementNav.Select("s:maximum", nsmgr);
      if (!maxIt.MoveNext())
        return null;
      double max = Convert.ToDouble(ParserHelper.SelectText(maxIt.Current, "text()", null));
      XPathNodeIterator stepIt = allowedValueRangeElementNav.Select("s:step", nsmgr);
      double? step = null;
      if (stepIt.MoveNext())
        step = Convert.ToDouble(ParserHelper.SelectText(stepIt.Current, "text()", null));
      return new CpAllowedValueRange(min, max, step);
    }

    #endregion
  }
}

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

namespace UPnP.Infrastructure.Dv.DeviceTree
{
  /// <summary>
  /// Defines the range of allowed numeric values for a UPnP state variable.
  /// </summary>
  public class DvAllowedValueRange
  {
    protected DvDataType _dataType;
    protected double _minValue;
    protected double _maxValue;
    protected double? _step;

    public DvAllowedValueRange(DvDataType dataType, double minValue, double maxValue) :
        this(dataType, minValue, maxValue, null) { }

    public DvAllowedValueRange(DvDataType dataType, double minValue, double maxValue, double? step)
    {
      _minValue = minValue;
      _maxValue = maxValue;
      _dataType = dataType;
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

    #region Description generation

    internal void AddSCPDDescriptionForValueRange(XmlWriter writer)
    {
      writer.WriteStartElement("allowedValueRange");
      writer.WriteStartElement("minimum");
      _dataType.SoapSerializeValue(_minValue, true, writer);
      writer.WriteEndElement(); // minimum
      writer.WriteStartElement("maximum");
      _dataType.SoapSerializeValue(_maxValue, true, writer);
      writer.WriteEndElement(); // maximum
      if (_step.HasValue)
      {
        writer.WriteStartElement("step");
        _dataType.SoapSerializeValue(_step.Value, true, writer);
        writer.WriteEndElement(); // step
      }
      writer.WriteEndElement(); // allowedValueRange
    }

    #endregion
  }
}

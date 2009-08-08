using System;
using System.Text;

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
      else
        return true;
    }

    #region Description generation

    internal void AddSCDPDescriptionForValueRange(StringBuilder result)
    {
      result.Append(
          "<allowedValueRange>");
      result.Append(
            "<minimum>");
      result.Append(_dataType.SoapSerializeValue(_minValue, true));
      result.Append("</minimum>");
      result.Append(
            "<maximum>");
      result.Append(_dataType.SoapSerializeValue(_maxValue, true));
      result.Append("</maximum>");
      if (_step.HasValue)
      {
        result.Append(
              "<step>");
        result.Append(_dataType.SoapSerializeValue(_step.Value, true));
        result.Append("</step>");
      }
      result.Append(
          "</allowedValueRange>");
    }

    #endregion
  }
}

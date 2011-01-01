#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Globalization;
using MediaPortal.Core.Configuration.ConfigurationClasses;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Localization;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.UiComponents.Configuration.ConfigurationControllers
{
  /// <summary>
  /// Configuration controller for the <see cref="NumberSelect"/> and <see cref="LimitedNumberSelect"/> configuration settings.
  /// For <see cref="NumberSelect"/> the value's min/max definitions are used as internal limit.
  /// </summary>
  public class NumberSelectController : DialogConfigurationController
  {
    #region Consts

    public const string ERROR_FLOATING_POINT_VALUE_RESOURCE = "[Configuration.ErrorFloatingPointValue]";
    public const string ERROR_INTEGER_VALUE_RESOURCE = "[Configuration.ErrorIntegerValue]";

    public const string ERROR_NUMERIC_LOWER_LIMIT_ERROR_RESOURCE = "[Configuration.ErrorNumericLowerLimit]";
    public const string ERROR_NUMERIC_UPPER_LIMIT_ERROR_RESOURCE = "[Configuration.ErrorNumericUpperLimit]";

    #endregion

    #region Internal classes & interfaces

    public interface INumberModel
    {
      bool TrySetValue(string value, out string errorText);
      object Value { get; }
      double LowerLimit { get; set; }
      double UpperLimit { get; set; }
      void Up();
      void Down();
    }

    public class FloatingPointModel : INumberModel
    {
      #region Protected fields

      protected double _value;
      protected IResourceString _errorTextResource;
      protected double _step;
      protected int _maxNumDigits;
      protected double _lowerLimit = double.MinValue;
      protected double _upperLimit = double.MaxValue;

      #endregion

      public FloatingPointModel(double value, double step, int maxNumDigits)
      {
        _value = value;
        _step = step;
        _maxNumDigits = maxNumDigits;
        RoundValue();
      }

      protected static bool ToDouble(string value, out double result)
      {
        ILocalization localization = ServiceRegistration.Get<ILocalization>();
        CultureInfo culture = localization.CurrentCulture;
        return double.TryParse(value, NumberStyles.Float, culture, out result);
      }

      protected int GetNumberOfDigitsToPreserve()
      {
        if (_maxNumDigits == -1)
          // The following formular tries to find a sensible number of decimal digits to preserve.
          // We use at least one digit and a maximum of 4 digits, depending on the logarithm of our value.
          return Math.Max(1, 4 - (int) Math.Log10(Math.Abs(_value) + 1));
        return _maxNumDigits;
      }

      protected void RoundValue()
      {
        _value = Math.Round(_value, GetNumberOfDigitsToPreserve());
      }

      public bool TrySetValue(string value, out string errorText)
      {
        double result;
        if (ToDouble(value, out result))
        {
          if (result > _upperLimit)
          {
            errorText = LocalizationHelper.CreateResourceString(ERROR_NUMERIC_UPPER_LIMIT_ERROR_RESOURCE).Evaluate(value, _lowerLimit.ToString(), _upperLimit.ToString());
            return false;
          }
          if (result < _lowerLimit)
          {
            errorText = LocalizationHelper.CreateResourceString(ERROR_NUMERIC_LOWER_LIMIT_ERROR_RESOURCE).Evaluate(value, _lowerLimit.ToString(), _upperLimit.ToString());
            return false;
          }
          _value = result;
          errorText = string.Empty;
          return true;
        }
        errorText = LocalizationHelper.CreateResourceString(ERROR_FLOATING_POINT_VALUE_RESOURCE).Evaluate(value);
        return false;
      }

      public object Value
      {
        get { return _value; }
      }

      public double LowerLimit
      {
        get { return _lowerLimit; }
        set { _lowerLimit = value; }
      }

      public double UpperLimit
      {
        get { return _upperLimit; }
        set { _upperLimit = value; }
      }

      public void Up()
      {
        if (_value + _step > _upperLimit)
          _value = _upperLimit;
        else
          _value += _step;
        RoundValue();
      }

      public void Down()
      {
        if (_value - _step > _upperLimit)
          _value = _upperLimit;
        else
          _value -= _step;
        RoundValue();
      }

      public override string ToString()
      {
        ILocalization localization = ServiceRegistration.Get<ILocalization>();
        CultureInfo culture = localization.CurrentCulture;
        return _value.ToString("F" + GetNumberOfDigitsToPreserve(), culture);
      }
    }

    public class IntegerModel : INumberModel
    {
      #region Protected fields

      protected int _value;
      protected IResourceString _errorTextResource;
      protected int _step;
      protected int _lowerLimit = int.MinValue;
      protected int _upperLimit = int.MaxValue;

      #endregion

      public IntegerModel(int value, int step)
      {
        _value = value;
        _step = step;
      }

      public bool TrySetValue(string value, out string errorText)
      {
        int result;
        if (int.TryParse(value, out result))
        {
          if (result > _upperLimit)
          {
            errorText = LocalizationHelper.CreateResourceString(ERROR_NUMERIC_UPPER_LIMIT_ERROR_RESOURCE).Evaluate(value, _lowerLimit.ToString(), _upperLimit.ToString());
            return false;
          }
          if (result < _lowerLimit)
          {
            errorText = LocalizationHelper.CreateResourceString(ERROR_NUMERIC_LOWER_LIMIT_ERROR_RESOURCE).Evaluate(value, _lowerLimit.ToString(), _upperLimit.ToString());
            return false;
          } 
          _value = result;
          errorText = string.Empty;
          return true;
        }
        errorText = LocalizationHelper.CreateResourceString(ERROR_INTEGER_VALUE_RESOURCE).Evaluate(value);
        return false;
      }

      public override string ToString()
      {
        ILocalization localization = ServiceRegistration.Get<ILocalization>();
        CultureInfo culture = localization.CurrentCulture;
        return _value.ToString(culture);
      }

      public object Value
      {
        get { return _value; }
      }

      public double LowerLimit
      {
        get { return _lowerLimit; }
        set { _lowerLimit = (int)value; }
      }

      public double UpperLimit
      {
        get { return _upperLimit; }
        set { _upperLimit = (int)value; }
      }

      public void Up()
      {
        if (_value + _step > _upperLimit)
          _value = _upperLimit;
        else
          _value += _step;
      }

      public void Down()
      {
        if (_value - _step < _lowerLimit)
          _value = _lowerLimit;
        else
          _value -= _step;
      }
    }

    #endregion

    #region Protected fields

    protected AbstractProperty _valueProperty;
    protected AbstractProperty _isValueValidProperty;
    protected AbstractProperty _errorTextProperty;
    protected INumberModel _numberModel = null;

    #endregion

    public NumberSelectController()
    {
      _valueProperty = new WProperty(typeof(string), "0");
      _isValueValidProperty = new WProperty(typeof(bool), true);
      _errorTextProperty = new WProperty(typeof(string), string.Empty);

      _valueProperty.Attach(OnValueChanged);
    }

    protected void OnValueChanged(AbstractProperty prop, object oldValue)
    {
      string errorText;
      if (_numberModel.TrySetValue(Value, out errorText))
      {
        IsValueValid = true;
        ErrorText = string.Empty;
      }
      else
      {
        IsValueValid = false;
        ErrorText = errorText;
      }
    }

    public override Type ConfigSettingType
    {
      get { return typeof(NumberSelect); }
    }

    protected override string DialogScreen
    {
      get { return "dialog_configuration_numberselect"; }
    }

    protected override void SettingChanged()
    {
      base.SettingChanged();
      if (_setting == null)
        return;
      NumberSelect numberSelect = (NumberSelect) _setting;
      switch (numberSelect.ValueType)
      {
        case NumberSelect.NumberType.FloatingPoint:
          _numberModel = new FloatingPointModel(numberSelect.Value, numberSelect.Step, numberSelect.MaxNumDigits);
          break;
        case NumberSelect.NumberType.Integer:
          _numberModel = new IntegerModel((int) numberSelect.Value, (int) numberSelect.Step);
          break;
        default:
          throw new InvalidDataException("NumberType '{0}' is not supported", numberSelect.ValueType);
      }
      GetLimits();
      Value = _numberModel.ToString();
    }

    private void GetLimits()
    {
      LimitedNumberSelect limitedNumberSelect = _setting as LimitedNumberSelect;
      if (limitedNumberSelect != null)
      {
        _numberModel.LowerLimit = limitedNumberSelect.LowerLimit;
        _numberModel.UpperLimit = limitedNumberSelect.UpperLimit;
      }
    }

    protected override void UpdateSetting()
    {
      NumberSelect numberSelect = (NumberSelect) _setting;
      GetLimits();
      numberSelect.Value = (double)_numberModel.Value;
      base.UpdateSetting();
    }

    public AbstractProperty ValueProperty
    {
      get { return _valueProperty; }
    }

    public string Value
    {
      get { return (string) _valueProperty.GetValue(); }
      set { _valueProperty.SetValue(value); }
    }

    public AbstractProperty IsValueValidProperty
    {
      get { return _isValueValidProperty; }
    }

    public bool IsValueValid
    {
      get { return (bool) _isValueValidProperty.GetValue(); }
      set { _isValueValidProperty.SetValue(value); }
    }

    public AbstractProperty ErrorTextProperty
    {
      get { return _errorTextProperty; }
    }

    public string ErrorText
    {
      get { return (string) _errorTextProperty.GetValue(); }
      set { _errorTextProperty.SetValue(value); }
    }

    public void Up()
    {
      _numberModel.Up();
      Value = _numberModel.ToString();
    }

    public void Down()
    {
      _numberModel.Down();
      Value = _numberModel.ToString();
    }
  }
}

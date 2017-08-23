#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.UiComponents.Configuration.ConfigurationControllers
{
  /// <summary>
  /// Configuration controller for the <see cref="NumberSelect"/> and <see cref="LimitedNumberSelect"/> configuration settings.
  /// For <see cref="NumberSelect"/> the value's min/max definitions are used as internal limit.
  /// </summary>
  public class NumberSelectController : AbstractEntryController
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
      object Value { get; }
      bool IsUpEnabled { get; }
      bool IsDownEnabled { get; }
      int GetMaxNumCharacters();
      bool TrySetValue(string value, out string errorText);
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

      protected int GetNumberOfDigitsToPreserve(double value)
      {
        if (_maxNumDigits == -1)
          // The following formular tries to find a sensible number of decimal digits to preserve.
          // We use at least one digit and a maximum of 4 digits, depending on the logarithm of our value.
          return Math.Max(1, 4 - (int) Math.Log10(Math.Abs(value) + 1));
        return _maxNumDigits;
      }

      protected void RoundValue()
      {
        _value = Math.Round(_value, GetNumberOfDigitsToPreserve(_value));
      }

      public int GetMaxNumCharacters()
      {
        return (int) Math.Log10(_upperLimit) + 1 /* pre decimal point digits */ + 1 /* decimal point */ + GetNumberOfDigitsToPreserve(_upperLimit) /* number of decimal places */;
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

      public bool IsUpEnabled
      {
        get { return _value < _upperLimit; }
      }

      public bool IsDownEnabled
      {
        get { return _value > _lowerLimit; }
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
        return _value.ToString("F" + GetNumberOfDigitsToPreserve(_value), culture);
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

      public int GetMaxNumCharacters()
      {
        return (int) Math.Log10(_upperLimit) + 1;
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

      public bool IsUpEnabled
      {
        get { return _value < _upperLimit; }
      }

      public bool IsDownEnabled
      {
        get { return _value > _lowerLimit; }
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

    protected INumberModel _numberModel = null;
    protected AbstractProperty _isUpEnabledProperty;
    protected AbstractProperty _isDownEnabledProperty;

    #endregion

    public NumberSelectController()
    {
      _isUpEnabledProperty = new WProperty(typeof(bool), true);
      _isDownEnabledProperty = new WProperty(typeof(bool), true);

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
      IsUpEnabled = _numberModel.IsUpEnabled;
      IsDownEnabled = _numberModel.IsDownEnabled;
    }

    public override Type ConfigSettingType
    {
      get { return typeof(NumberSelect); }
    }

    protected override string DialogScreen
    {
      get { return "dialog_configuration_numberselect"; }
    }

    public AbstractProperty IsUpEnabledProperty
    {
      get { return _isUpEnabledProperty; }
    }

    public bool IsUpEnabled
    {
      get { return (bool) _isUpEnabledProperty.GetValue(); }
      set { _isUpEnabledProperty.SetValue(value); }
    }

    public AbstractProperty IsDownEnabledProperty
    {
      get { return _isDownEnabledProperty; }
    }

    public bool IsDownEnabled
    {
      get { return (bool) _isDownEnabledProperty.GetValue(); }
      set { _isDownEnabledProperty.SetValue(value); }
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
      DisplayLength = _numberModel.GetMaxNumCharacters();
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
      numberSelect.Value = Convert.ToDouble(_numberModel.Value);
      base.UpdateSetting();
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

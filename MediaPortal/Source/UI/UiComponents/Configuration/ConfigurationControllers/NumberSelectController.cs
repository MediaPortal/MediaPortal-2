#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Globalization;
using MediaPortal.Core.Configuration.ConfigurationClasses;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Localization;
using MediaPortal.Utilities.Exceptions;

namespace UiComponents.Configuration.ConfigurationControllers
{
  /// <summary>
  /// Configuration controller for the <see cref="NumberSelect"/> configuration setting.
  /// </summary>
  public class NumberSelectController : DialogConfigurationController
  {
    #region Consts

    public const string ERROR_FLOATING_POINT_VALUE_RESOURCE = "[Configuration.ErrorFloatingPointValue]";
    public const string ERROR_INTEGER_VALUE_RESOURCE = "[Configuration.ErrorIntegerValue]";

    #endregion

    #region Internal classes & interfaces
    
    public interface INumberModel
    {
      bool TrySetValue(string value, out string errorText);
      object Value { get; }
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
        ILocalization localization = ServiceScope.Get<ILocalization>();
        CultureInfo culture = localization.CurrentCulture;
        return double.TryParse(value, NumberStyles.Float, culture, out result);
      }

      protected int GetNumberOfDigitsToPreserve()
      {
        if (_maxNumDigits == -1)
          // The following formular tries to find a sensible number of decimal digits to preserve.
          // We use at least one digit and a maximum of 4 digits, depending on the logarithm of our value.
          return Math.Max(1, 4 - (int) Math.Log10(Math.Abs(_value) + 1));
        else
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
          _value = result;
          errorText = string.Empty;
          return true;
        }
        else
        {
          errorText = LocalizationHelper.CreateResourceString(ERROR_FLOATING_POINT_VALUE_RESOURCE).Evaluate(value);
          return false;
        }
      }

      public object Value
      {
        get { return _value; }
      }

      public void Up()
      {
        _value += _step;
        RoundValue();
      }

      public void Down()
      {
        _value -= _step;
        RoundValue();
      }

      public override string ToString()
      {
        ILocalization localization = ServiceScope.Get<ILocalization>();
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
          _value = result;
          errorText = string.Empty;
          return true;
        }
        else
        {
          errorText = LocalizationHelper.CreateResourceString(ERROR_INTEGER_VALUE_RESOURCE).Evaluate(value);
          return false;
        }
      }

      public override string ToString()
      {
        ILocalization localization = ServiceScope.Get<ILocalization>();
        CultureInfo culture = localization.CurrentCulture;
        return _value.ToString(culture);
      }

      public object Value
      {
        get { return _value; }
      }

      public void Up()
      {
        _value += _step;
      }

      public void Down()
      {
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
      Value = _numberModel.ToString();
    }

    protected override void UpdateSetting()
    {
      NumberSelect numberSelect = (NumberSelect) _setting;
      numberSelect.Value = (double) _numberModel.Value;
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

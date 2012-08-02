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
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;


namespace MP2_PluginWizard.Converter
{
  [ValueConversion(typeof(bool), typeof(Visibility))]
  public class BoolToVisibilityConverter : MarkupExtension, IValueConverter
  {
    public BoolToVisibilityConverter()
    {
      TrueValue = Visibility.Visible;
      FalseValue = Visibility.Collapsed;
    }

    public Visibility TrueValue { get; set; }
    public Visibility FalseValue { get; set; }

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      var val = System.Convert.ToBoolean(value);
      return val ? TrueValue : FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return TrueValue.Equals(value);
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }
  }

}

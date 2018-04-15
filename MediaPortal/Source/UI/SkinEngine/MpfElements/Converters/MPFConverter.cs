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
using System.ComponentModel;
using System.Globalization;

namespace MediaPortal.UI.SkinEngine.MpfElements.Converters
{
  /// <summary>
  /// <see cref="MPFConverter{T}"/> provides the MPF specific default converter. It provides a "bridge" between WPF and MPF based classes and interfaces.
  /// </summary>
  public class MPFConverter<T> : TypeConverter
  {
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
      // This wrapper expects all input to be convertible
      return true;
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
      // This wrapper expects all input to be convertible
      return true;
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
      return Xaml.TypeConverter.Convert(value, typeof(T));
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
      return Xaml.TypeConverter.Convert(value, destinationType);
    }
  }
}

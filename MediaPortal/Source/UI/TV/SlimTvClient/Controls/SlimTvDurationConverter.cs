#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.UI.SkinEngine.MarkupExtensions;
using MediaPortal.UI.SkinEngine.MpfElements.Converters;
using MediaPortal.UI.SkinEngine.Xaml;

namespace MediaPortal.Plugins.SlimTv.Client.Controls
{
  public abstract class AbstractDurationConverter : AbstractSingleDirectionConverter
  {
    public override bool Convert(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      result = null;
      if (val == null || val.GetType() != typeof(DateTime))
        return true;

      DateTime dtVal = (DateTime)val;
      TimeSpan delta = CalculateDifference(dtVal);
      FormatDuration(delta, culture, ref result);
      return true;
    }

    public static void FormatDuration(TimeSpan delta, CultureInfo culture, ref object result)
    {
      if (delta > TimeSpan.FromHours(1))
        result = delta.ToString("hh\\:mm\\:ss", culture);
      else if (delta > TimeSpan.Zero)
        result = delta.ToString("mm\\:ss", culture);
    }

    protected abstract TimeSpan CalculateDifference(DateTime dtVal);
  }

  /// <summary>
  /// Calculates the difference between the given DateTime and "Now".
  /// </summary>
  public class RemainingDurationConverter : AbstractDurationConverter
  {
    protected override TimeSpan CalculateDifference(DateTime dtVal)
    {
      return dtVal - DateTime.Now;
    }
  }

  /// <summary>
  /// Calculates the difference between "Now" and the given DateTime.
  /// </summary>
  public class ElapsedDurationConverter : AbstractDurationConverter
  {
    protected override TimeSpan CalculateDifference(DateTime dtVal)
    {
      return DateTime.Now - dtVal;
    }
  }

  /// <summary>
  /// Calculates the difference between two DateTime values: value2 - value1.
  /// </summary>
  public class DurationMultiConverter : IMultiValueConverter
  {
    public bool Convert(IDataDescriptor[] values, Type targetType, object parameter, out object result)
    {
      result = null;
      if (values == null || values.Length != 2 || values[0]?.Value?.GetType() != typeof(DateTime) || values[1]?.Value?.GetType() != typeof(DateTime))
        return true;

      TimeSpan delta = (DateTime)values[1].Value - (DateTime)values[0].Value;
      AbstractDurationConverter.FormatDuration(delta, ServiceRegistration.Get<ILocalization>().CurrentCulture, ref result);
      return true;
    }
  }
}

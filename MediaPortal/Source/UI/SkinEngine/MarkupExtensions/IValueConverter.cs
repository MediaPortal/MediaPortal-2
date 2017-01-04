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
using MediaPortal.UI.SkinEngine.MpfElements;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  /// <summary>
  /// Exposes methods that allow modifying the data as it passes through the binding engine.
  /// </summary>
  public interface IValueConverter : ISkinEngineManagedObject
  {
    /// <summary>
    /// Modifies the source data before passing it to the target for display in the UI.
    /// </summary>
    /// <param name="val">The source data being passed to the target.</param>
    /// <param name="targetType">The <see cref="Type"/> of data expected by the target dependency property.</param>
    /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
    /// <param name="culture">The culture of the conversion.</param>
    /// <param name="result">The value to be passed to the target dependency property.</param>
    /// <returns><c>true</c> if successful.</returns>
    bool Convert(object val, Type targetType, object parameter, CultureInfo culture, out object result);

    /// <summary>
    /// Modifies the target data before passing it to the source object. This method is called only in TwoWay bindings.
    /// </summary>
    /// <param name="val">The target data being passed to the source.</param>
    /// <param name="targetType">The <see cref="Type"/> of data expected by the source object.</param>
    /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
    /// <param name="culture">The culture of the conversion.</param>
    /// <param name="result">The value to be passed to the source object.</param>
    /// <returns><c>true</c> if successful.</returns>
    bool ConvertBack(object val, Type targetType, object parameter, CultureInfo culture, out object result);
  }
}

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
using System.Linq;

namespace MediaPortal.UI.SkinEngine.MpfElements.Converters
{
  /// <summary>
  /// Value converter which replaces invalid characters for filenames by <c>_</c>. This can be used to create filesystem conform names.
  /// It also supports same formatting arguments as <see cref="StringFormatConverter"/>.
  /// </summary>
  /// <remarks>
  /// This converter will often be used in XAML files. Note that in XAML, an attribute beginning with a <c>'{'</c> character
  /// is interpreted as an invocation of a markup extension. So the expression "{0}" must be escaped like this:
  /// <c>"{}{0}"</c>.
  /// </remarks>
  public class SafeFilenameConverter : AbstractSingleDirectionConverter
  {
    #region IValueConverter implementation

    public override bool Convert(object val, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
    {
      result = null;
      string filename = val as string;
      if (filename == null)
        return false;

      filename = System.IO.Path.GetInvalidFileNameChars().Aggregate(filename, (current, invalidFileNameChar) => current.Replace(invalidFileNameChar, '_'));

      var sfc = new StringFormatConverter();
      return sfc.Convert(filename, targetType, parameter, culture, out result);
    }

    #endregion
  }
}

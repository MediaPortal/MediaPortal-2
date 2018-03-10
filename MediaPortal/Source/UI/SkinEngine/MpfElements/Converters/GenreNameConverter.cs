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
using MediaPortal.Common;
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.UI.SkinEngine.MarkupExtensions;

namespace MediaPortal.UI.SkinEngine.MpfElements.Converters
{
  /// <summary>
  /// GenreConverter converts a genre id to a genre name.
  /// </summary>
  public class GenreNameConverter : IValueConverter
  {
    #region IValueConverter implementation

    public bool Convert(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      result = null;
      if (val == null)
        return false;

      string genreCategory = parameter as string;
      if (int.TryParse(val.ToString(), out int genreId) && genreCategory != null)
      {
        IGenreConverter converter = ServiceRegistration.Get<IGenreConverter>();
        if(converter.GetGenreName(genreId, genreCategory, culture.Name, out string genreName))
        {
          result = genreName;
          return true;
        }
      }

      return false;
    }

    public bool ConvertBack(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      // Back conversion not supported
      result = null;
      return false;
    }

    #endregion
  }
}

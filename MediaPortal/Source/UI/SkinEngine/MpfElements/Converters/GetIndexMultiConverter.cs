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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.UI.SkinEngine.MarkupExtensions;
using MediaPortal.UI.SkinEngine.Xaml;

namespace MediaPortal.UI.SkinEngine.MpfElements.Converters
{
  public class GetIndexMultiConverter : IMultiValueConverter
  {
    public bool Convert(IDataDescriptor[] values, Type targetType, object parameter, out object result)
    {
      var enumerable = values[1].Value as IEnumerable;
      if (enumerable != null)
      {
        var collection = new List<object>(enumerable.OfType<object>());
        var itemIndex = collection.IndexOf(values[0].Value);
        int indexOffset;
        // Support offset, usually "1" to show "1/50" instead "0/50"
        if (int.TryParse(parameter as string, out indexOffset))
          itemIndex += indexOffset;
        result = itemIndex;
      }
      else
      {
        result = -1;
      }
      return true;
    }
  }
}

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

using MediaPortal.Common.MediaManagement;
using MediaPortal.UI.SkinEngine.MarkupExtensions;
using MediaPortal.UI.SkinEngine.Xaml;
using System;
using System.Collections.Generic;
using System.Text;

namespace MediaPortal.UI.SkinEngine.MpfElements.Converters
{
  public class MultiAspectAttributeConverter : IMultiValueConverter
  {
    protected const string DEFAULT_SEPARATOR = ", ";

    public bool Convert(IDataDescriptor[] values, Type targetType, object parameter, out object result)
    {
      result = null;
      if (values.Length < 2)
        return false;

      MediaItem mi = values[0].Value as MediaItem;
      if (mi == null)
        return true;
      MediaItemAspectMetadata.MultipleAttributeSpecification mas = values[1].Value as MediaItemAspectMetadata.MultipleAttributeSpecification;
      if (mas == null)
        return true;
      List<object> attributes;
      if (!MediaItemAspect.TryGetAttribute(mi.Aspects, mas, out attributes) || attributes.Count == 0)
        return true;

      string separator = parameter as string;
      if (string.IsNullOrEmpty(separator))
        separator = DEFAULT_SEPARATOR;

      bool first = true;
      StringBuilder sb = new StringBuilder();
      foreach (object attribute in attributes)
      {
        if (attribute == null)
          continue;
        string attributeString = attribute.ToString();
        if (!string.IsNullOrEmpty(attributeString))
        {
          if (first)
            first = false;
          else
            sb.Append(separator);
          sb.Append(attributeString);
        }
      }
      result = sb.ToString();
      return true;
    }
  }
}
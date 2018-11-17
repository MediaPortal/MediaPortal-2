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

using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.MarkupExtensions;
using MediaPortal.UI.SkinEngine.MpfElements.Converters;
using MediaPortal.UI.SkinEngine.Xaml;
using System;
using System.Globalization;

namespace MediaPortal.UiComponents.WMCSkin.Converters
{
  public class MediaListVisibilityConverter : AbstractSingleDirectionConverter
  {
    public override bool Convert(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      ItemsList mediaList = val as ItemsList;
      VisibilityEnum visibility = mediaList != null && mediaList.Count > 0 ? VisibilityEnum.Visible : VisibilityEnum.Collapsed;
      result = TypeConverter.Convert(visibility, targetType);
      return true;
    }
  }

  public class MultiMediaListIsVisibleConverter : IMultiValueConverter
  {
    public bool Convert(IDataDescriptor[] values, Type targetType, object parameter, out object result)
    {
      bool visible = false;
      foreach (var value in values)
        if (value.Value is ItemsList mediaList && mediaList != null && mediaList.Count > 0)
        {
          visible = true;
          break;
        }
      result = TypeConverter.Convert(visible, targetType);
      return true;
    }
  }
}

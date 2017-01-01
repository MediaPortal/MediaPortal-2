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
using MediaPortal.UI.SkinEngine.Controls.ImageSources;
using MediaPortal.UI.SkinEngine.MpfElements.Converters;

namespace MediaPortal.Extensions.UserServices.FanArtService.Client
{
  public class FanArtImageSourceConverter : AbstractSingleDirectionConverter
  {
    public override bool Convert(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      result = null;
      FanArtImageSource imageSource = val as FanArtImageSource;
      if (imageSource == null)
      {
        ImageSource source = val as ImageSource;
        if (source != null)
        {
          result = source;
          return true;
        }
        return false;
      }

      string param = parameter as string;
      if (string.IsNullOrEmpty(param))
        return false;

      var args = param.Split(';');
      if (args.Length < 3)
        return false;

      string fanartType = args[0];
      int maxWidth;
      int maxHeight;
      int.TryParse(args[1], out maxWidth);
      int.TryParse(args[2], out maxHeight);

      bool useCache = true;
      if (args.Length == 4 && !bool.TryParse(args[3], out useCache))
        useCache = true;

      result = new FanArtImageSource
        {
          FanArtMediaType = imageSource.FanArtMediaType,
          FanArtName = imageSource.FanArtName,
          FanArtType = fanartType,
          MaxWidth = maxWidth,
          MaxHeight = maxHeight,
          Cache = useCache
        };

      return true;
    }
  }
}

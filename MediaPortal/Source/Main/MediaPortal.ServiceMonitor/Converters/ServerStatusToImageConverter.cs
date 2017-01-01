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
using System.Windows.Data;
using System.Windows.Markup;
using MediaPortal.ServiceMonitor.ViewModel;

namespace MediaPortal.ServiceMonitor.Converters
{
  [ValueConversion(typeof(ServerStatus), typeof(string))]
  public class ServerStatusToImageConverter : MarkupExtension, IValueConverter
  {
    public const string IMAGE_PATH = "../../";

    #region Overrides of MarkupExtension

    private static ServerStatusToImageConverter _converter;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return _converter ?? (_converter = new ServerStatusToImageConverter());
    }

    #endregion

    #region Implementation of IValueConverter

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      ServerStatus serverStatus = (ServerStatus) value;
      switch (serverStatus)
      {
        case ServerStatus.Attached:
        case ServerStatus.Connected:
        case ServerStatus.ClientConnected:
          return IMAGE_PATH + "Resources/Images/connected.png";
        case ServerStatus.Disconnected:
          return IMAGE_PATH + "Resources/Images/disconnected.png";
        default:
          return IMAGE_PATH + "Resources/Images/Info.png";
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }

    #endregion
  }
}

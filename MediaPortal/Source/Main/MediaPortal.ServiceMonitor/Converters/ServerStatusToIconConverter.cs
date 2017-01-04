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
  public class ServerStatusToIconConverter : MarkupExtension, IValueConverter
  {
    #region Overrides of MarkupExtension
    private static ServerStatusToIconConverter _converter;
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return _converter ?? (_converter = new ServerStatusToIconConverter());
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
          return "/MP2-ServiceMonitor1.ico";
        case ServerStatus.ClientConnected:
          return "/MP2-ServiceMonitor.ico";
        default:
          return "/MP2-ServiceMonitor0.ico";
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }

    #endregion
  }
}

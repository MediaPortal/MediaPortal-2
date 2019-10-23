using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace MP2BootstrapperApp
{
  public class PackageStateToVisibilityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is PackageState installedState && parameter is PackageState convertParameter)
      {
        return installedState == convertParameter ? Visibility.Visible : Visibility.Hidden;
      }
      return Visibility.Hidden;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}

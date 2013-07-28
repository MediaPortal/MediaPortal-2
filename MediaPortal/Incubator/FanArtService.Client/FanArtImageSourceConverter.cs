using System;
using System.Globalization;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
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
        return false;

      string param = parameter as string;
      if (string.IsNullOrEmpty(param))
        return false;

      var args = param.Split(';');
      if (args.Length < 3)
        return false;

      FanArtConstants.FanArtType fanartType;
      if (!Enum.TryParse(args[0], out fanartType))
        return false;

      int maxWidth;
      int maxHeight;
      int.TryParse(args[1], out maxWidth);
      int.TryParse(args[2], out maxHeight);

      result = new FanArtImageSource
        {
          FanArtMediaType = imageSource.FanArtMediaType,
          FanArtName = imageSource.FanArtName,
          FanArtType = fanartType,
          MaxWidth = maxWidth,
          MaxHeight = maxHeight
        };

      return true;
    }
  }
}

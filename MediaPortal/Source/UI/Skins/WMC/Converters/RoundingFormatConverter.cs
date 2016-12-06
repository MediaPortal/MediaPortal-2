using MediaPortal.UI.SkinEngine.MpfElements.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace MediaPortal.UiComponents.WMCSkin.Converters
{
  public class RoundingFormatConverter : AbstractSingleDirectionConverter
  {
    public override bool Convert(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      result = null;
      string parameterString = parameter as string;
      if (string.IsNullOrEmpty(parameterString))
        return false;
      string[] parameters = parameterString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

      int rounding;
      if (parameters.Length < 1 || !int.TryParse(parameters[0] as string, out rounding))
        return false;

      double number;
      try
      {
        number = System.Convert.ToDouble(val);
      }
      catch
      {
        return false;
      }

      int rounded = (int)Math.Round(number / rounding, MidpointRounding.AwayFromZero) * rounding;
      if (parameters.Length == 1)
      {
        result = rounded;
        return true;
      }

      try
      {
        result = string.Format(parameters[1], rounded);
        return true;
      }
      catch
      {
        return false;
      }
    }
  }
}

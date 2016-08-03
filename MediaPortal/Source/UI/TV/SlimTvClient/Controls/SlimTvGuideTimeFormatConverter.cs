using MediaPortal.UI.SkinEngine.MarkupExtensions;
using MediaPortal.UI.SkinEngine.Xaml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.SlimTv.Client.Controls
{
  public class SlimTvGuideTimeFormatConverter : IMultiValueConverter
  {
    public bool Convert(IDataDescriptor[] values, Type targetType, object parameter, out object result)
    {
      result = null;
      DateTime dtVal = (DateTime)values[0].Value;
      double durationPerc;
      if (double.TryParse(parameter as string, NumberStyles.Any, CultureInfo.InvariantCulture, out durationPerc) &&
        values.Length > 1 && values[1].Value is double)
        dtVal = dtVal.AddHours((double)values[1].Value * durationPerc);

      result = dtVal.ToString("t", CultureInfo.CurrentUICulture);
      return true;
    }
  }
}
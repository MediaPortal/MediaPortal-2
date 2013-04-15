using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.UI.SkinEngine.MarkupExtensions;

namespace MediaPortal.UI.SkinEngine.MpfElements.Converters
{
  /// <summary>
  /// Value converter which uses a format string to build a string from a given variable.
  /// </summary>
  public class StringFormatConverter : IValueConverter
  {
    #region IValueConverter implementation

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// This converter will often be used in XAML files. Note that in XAML, an attribute beginning with a <c>'{'</c> character
    /// is interpreted as an invocation of a markup extension. So the expression "{0}" must be escaped like this:
    /// <c>"{}{0}"</c>.
    /// </remarks>
    /// <param name="val"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public bool Convert(object val, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
    {
      result = null;
      string expression = parameter as string;
      if (string.IsNullOrEmpty(expression))
        result = val.ToString();
      else
      {
        try
        {
          result = string.Format(culture, expression, val);
        }
        catch (Exception)
        {
          return false;
        }
      }
      return true;
    }

    public bool ConvertBack(object val, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
    {
      // In general, we cannot invert the function given by the parameter
      result = null;
      return false;
    }

    #endregion
  }
}

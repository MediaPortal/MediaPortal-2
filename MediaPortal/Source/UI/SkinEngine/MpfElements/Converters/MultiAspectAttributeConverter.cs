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
      List<object> results;
      if (!MediaItemAspect.TryGetAttribute(mi.Aspects, mas, out results) || results.Count == 0)
        return true;

      string separator = parameter as string;
      if (string.IsNullOrEmpty(separator))
        separator = DEFAULT_SEPARATOR;

      StringBuilder sb = new StringBuilder(results[0].ToString());
      for (int i = 1; i < results.Count; i++)
      {
        sb.Append(separator);
        sb.Append(results[i]);
      }
      result = sb.ToString();
      return true;
    }
  }
}
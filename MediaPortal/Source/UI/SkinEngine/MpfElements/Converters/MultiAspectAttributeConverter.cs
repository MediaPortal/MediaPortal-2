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
      List<object> attributes;
      if (!MediaItemAspect.TryGetAttribute(mi.Aspects, mas, out attributes) || attributes.Count == 0)
        return true;

      string separator = parameter as string;
      if (string.IsNullOrEmpty(separator))
        separator = DEFAULT_SEPARATOR;

      bool first = true;
      StringBuilder sb = new StringBuilder();
      foreach (object attribute in attributes)
      {
        if (attribute == null)
          continue;
        string attributeString = attribute.ToString();
        if (!string.IsNullOrEmpty(attributeString))
        {
          if (first)
            first = false;
          else
            sb.Append(separator);
          sb.Append(attributeString);
        }
      }
      result = sb.ToString();
      return true;
    }
  }
}
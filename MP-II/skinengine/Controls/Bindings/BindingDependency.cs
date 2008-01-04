using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Bindings
{
  public class BindingDependency : Property
  {
    Property _source;

    public BindingDependency(Property source)
    {
      _source = source;
      _source.Attach(new PropertyChangedHandler(OnSourcePropertyChanged));
      OnSourcePropertyChanged(_source);
    }

    void OnSourcePropertyChanged(Property property)
    {
      SetValue(property.GetValue());
    }
  }
}

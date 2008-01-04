using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Bindings
{
  public class BindingDependency
  {
    Property _source;
    Property _destination;

    public BindingDependency(Property source, Property dest)
    {
      _source = source;
      _destination = dest;
      _source.Attach(new PropertyChangedHandler(OnSourcePropertyChanged));
      OnSourcePropertyChanged(_source);
    }

    void OnSourcePropertyChanged(Property property)
    {
      _destination.SetValue(property.GetValue());
    }
  }
}

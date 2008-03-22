using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using MediaPortal.Presentation.Properties;

namespace SkinEngine.Controls.Bindings
{
  public enum BindingMode
  {
    OneWay,
    TwoWay
  };
  public class BindingDependency
  {
    Property _source;
    Property _destination;
    MethodInfo _methodInfo;
    object _destinationObject;
    BindingMode _mode = BindingMode.TwoWay;
    bool _negate = false;

    public BindingDependency(Property source, Property dest, BindingMode mode, bool negate)
    {
      _negate = negate;
      _mode = mode;
      _source = source;
      _destination = dest;
      _source.Attach(new PropertyChangedHandler(OnSourcePropertyChanged));
      if (mode == BindingMode.TwoWay)
        _destination.Attach(new PropertyChangedHandler(OnDestinationPropertyChanged));
      OnSourcePropertyChanged(_source);
    }

    public BindingDependency(Property source, MethodInfo info, object destobject, BindingMode mode, bool negate)
    {
      _negate = negate;
      _mode = mode;
      _source = source;
      _destinationObject = destobject;
      _methodInfo = info;
      _source.Attach(new PropertyChangedHandler(OnSourcePropertyChanged));
      OnSourcePropertyChanged(_source);
    }
    public bool Negate
    {
      get
      {
        return _negate;
      }
      set
      {
        _negate = value;
      }
    }
    void OnSourcePropertyChanged(Property property)
    {
      if (_destination != null)
      {
        if (Negate)
        {
          bool v = (bool)property.GetValue();
          _destination.SetValue(!v);
        }
        else
        {
          _destination.SetValue(property.GetValue());
        }
      }
      else
      {

        if (Negate)
        {
          bool v = (bool)property.GetValue();
          _methodInfo.Invoke(_destinationObject, new object[] { !v });
        }
        else
        {
          _methodInfo.Invoke(_destinationObject, new object[] { property.GetValue() });
        }
      }
    }

    void OnDestinationPropertyChanged(Property property)
    {
      if (BindingMode == BindingMode.TwoWay)
      {
        _source.SetValue(property.GetValue());
      }
    }

    public BindingMode BindingMode
    {
      get
      {
        return _mode;
      }
      set
      {
        _mode = value;
      }
    }
  }
}

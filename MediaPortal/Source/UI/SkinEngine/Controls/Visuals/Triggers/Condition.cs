#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers
{
  public class Condition : DependencyObject
  {
    #region Protected fields

    protected UIElement _element;
    protected AbstractProperty _propertyProperty;
    protected AbstractProperty _bindingProperty;
    protected AbstractProperty _valueProperty;
    protected AbstractProperty _triggeredProperty;
    protected IDataDescriptor _dataDescriptor;

    #endregion

    #region Ctor

    public Condition()
    {
      Init();
    }

    void Init()
    {
      _propertyProperty = new SProperty(typeof(string), string.Empty);
      _bindingProperty = new SProperty(typeof(object), null);
      _valueProperty = new SProperty(typeof(object), null);
      _triggeredProperty = new SProperty(typeof(bool), false);
    }

    void Attach()
    {
      if (_element == null)
        return;
      if (!string.IsNullOrEmpty(Property))
      {
        if (ReflectionHelper.FindMemberDescriptor(_element, Property, out _dataDescriptor))
          _dataDescriptor.Attach(OnPropertyChanged);
      }
      else
      {
        _bindingProperty.Attach(OnBindingValueChanged);
      }
    }

    void Detach()
    {
      if (!string.IsNullOrEmpty(Property))
      {
        if (_dataDescriptor != null)
        {
          _dataDescriptor.Detach(OnPropertyChanged);
          _dataDescriptor = null;
        }
      }
      else
      {
        _bindingProperty.Detach(OnBindingValueChanged);
      }
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Condition c = (Condition)source;
      Property = c.Property;
      Binding = copyManager.GetCopy(c.Binding);
      Value = copyManager.GetCopy(c.Value);
      Attach();
    }

    public override void Dispose()
    {
      MPF.TryCleanupAndDispose(Value);
      if (_dataDescriptor != null)
        _dataDescriptor.Detach(OnPropertyChanged);
      base.Dispose();
    }

    #endregion

    #region Public properties

    public AbstractProperty PropertyProperty
    {
      get { return _propertyProperty; }
    }

    public string Property
    {
      get { return (string)_propertyProperty.GetValue(); }
      set { _propertyProperty.SetValue(value); }
    }

    public AbstractProperty BindingProperty
    {
      get { return _bindingProperty; }
    }

    public object Binding
    {
      get { return _bindingProperty.GetValue(); }
      set { _bindingProperty.SetValue(value); }
    }

    public AbstractProperty ValueProperty
    {
      get { return _valueProperty; }
    }

    public object Value
    {
      get { return _valueProperty.GetValue(); }
      set { _valueProperty.SetValue(value); }
    }

    internal AbstractProperty TriggeredProperty
    {
      get { return _triggeredProperty; }
    }

    internal bool Triggered
    {
      get { return (bool)_triggeredProperty.GetValue(); }
      set { _triggeredProperty.SetValue(value); }
    }

    #endregion

    public void Setup(UIElement element)
    {
      Detach();
      _element = element;
      Attach();
      TriggerIfValuesEqual();
    }

    public void Reset()
    {
      Detach();
    }

    /// <summary>
    /// Listens for changes of our trigger property data descriptor.
    /// </summary>
    protected void OnPropertyChanged(IDataDescriptor dd)
    {
      if (_dataDescriptor == null)
        return;
      TriggerIfValuesEqual(_dataDescriptor.Value, Value);
    }

    protected void OnBindingValueChanged(AbstractProperty bindingValue, object oldValue)
    {
      if (_element == null)
        return;
      TriggerIfValuesEqual(bindingValue.GetValue(), Value);
    }

    protected void TriggerIfValuesEqual()
    {
      if (_element == null)
        return;
      if (!string.IsNullOrEmpty(Property))
      {
        if (_dataDescriptor != null)
          TriggerIfValuesEqual(_dataDescriptor.Value, Value);
      }
      else
      {
        TriggerIfValuesEqual(Binding, Value);
      }
    }

    protected void TriggerIfValuesEqual(object triggerValue, object checkValue)
    {
      object obj = null;
      try
      {
        Triggered = (triggerValue == null && checkValue == null) || (triggerValue != null && TypeConverter.Convert(checkValue, triggerValue.GetType(), out obj) &&
            Equals(triggerValue, obj));
      }
      finally
      {
        if (!ReferenceEquals(obj, checkValue))
          // If the conversion created a copy of the object, dispose it
          MPF.TryCleanupAndDispose(obj);
      }
    }
  }
}

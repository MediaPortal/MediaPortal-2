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

using System;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers
{
  public class Trigger: TriggerBase, IAddChild<Setter>
  {
    #region Protected fields

    protected AbstractProperty _propertyProperty;
    protected AbstractProperty _valueProperty;
    protected IDataDescriptor _dataDescriptor;

    #endregion

    #region Ctor

    public Trigger()
    {
      Init();
    }

    void Init()
    {
      _propertyProperty = new SProperty(typeof(string), string.Empty);
      _valueProperty = new SProperty(typeof(object), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Trigger t = (Trigger) source;
      Property = t.Property;
      Value = copyManager.GetCopy(t.Value);
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
      get { return (string) _propertyProperty.GetValue(); }
      set { _propertyProperty.SetValue(value); }
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

    #endregion

    protected void DetachFromDataDescriptor()
    {
      if (_dataDescriptor != null)
      {
        _dataDescriptor.Detach(OnPropertyChanged);
        _dataDescriptor = null;
      }
    }

    protected void AttachToDataDescriptor(UIElement element)
    {
      if (_element == null)
        return;
      if (!String.IsNullOrEmpty(Property))
      {
        string property = Property;
        int index = property.IndexOf('.');
        if (index != -1)
        {
          string propertyProvider = property.Substring(0, index);
          string propertyName = property.Substring(index + 1);
          DefaultAttachedPropertyDataDescriptor result;
          if (!DefaultAttachedPropertyDataDescriptor.CreateAttachedPropertyDataDescriptor(new MpfNamespaceHandler(),
            element, propertyProvider, propertyName, out result))
          {
            ServiceRegistration.Get<ILogger>().Warn(
              string.Format("Attached property '{0}' cannot be set on element '{1}'", property, element));
            return;
          }
          _dataDescriptor = result;
          _dataDescriptor.Attach(OnPropertyChanged);
        }
        else
        {
          if (ReflectionHelper.FindMemberDescriptor(_element, Property, out _dataDescriptor))
            _dataDescriptor.Attach(OnPropertyChanged);
        }
      }
    }

    public override void Setup(UIElement element)
    {
      DetachFromDataDescriptor();
      base.Setup(element);
      AttachToDataDescriptor(element);
      if (_dataDescriptor == null)
        return;
      TriggerIfValuesEqual(_dataDescriptor.Value, Value);
    }

    public override void Reset()
    {
      DetachFromDataDescriptor();
      base.Reset();
    }

    /// <summary>
    /// Listens for changes of our trigger property data descriptor.
    /// </summary>
    void OnPropertyChanged(IDataDescriptor dd)
    {
      if (_dataDescriptor == null) return;
      TriggerIfValuesEqual(_dataDescriptor.Value, Value);
    }

    #region IAddChild Members

    public void AddChild(Setter s)
    {
      Setters.Add(s);
    }

    #endregion
  }
}

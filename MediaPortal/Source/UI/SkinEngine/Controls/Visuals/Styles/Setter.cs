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

using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Styles
{
  public class Setter : SetterBase
  {
    #region Protected fields

    protected AbstractProperty _valueProperty;

    #endregion

    #region Ctor

    public Setter()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _valueProperty = new SProperty(typeof(object), null);
    }

    void Attach()
    {
      _valueProperty.Attach(OnValueChanged);
    }

    void Detach()
    {
      _valueProperty.Detach(OnValueChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Setter s = (Setter) source;
      Value = copyManager.GetCopy(s.Value);
      Attach();
    }

    public override void Dispose()
    {
      MPF.CleanupAndDisposeResourceIfOwner(Value, this);
      base.Dispose();
    }

    #endregion

    void OnValueChanged(AbstractProperty prop, object oldVal)
    {
      MPF.CleanupAndDisposeResourceIfOwner(oldVal, this);
      MPF.SetOwner(Value, this, false);
    }

    #region Properties

    public AbstractProperty ValueProperty
    {
      get { return _valueProperty; }
    }

    /// <summary>
    /// Gets or sets the value to be set on our target. This value will be
    /// later converted to the correct target type.
    /// </summary>
    public object Value
    {
      get { return _valueProperty.GetValue(); }
      set { _valueProperty.SetValue(value); }
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Wrapper class for storing the original value of a property
    /// </summary>
    protected internal class SetterData
    {
      protected object _origValue;

      public SetterData(object origValue)
      {
        _origValue = origValue;
      }

      public object OrigValue
      {
        get { return _origValue; }
      }
    }

    protected string GetAttachedPropertyName()
    {
      return "Setter." + "." + Property + ".Data"; // The attached property will be attached to the target object so we don't need the TargetName here
    }

    protected SetterData GetSetterData(DependencyObject targetObject)
    {
      return targetObject.GetAttachedPropertyValue<SetterData>(GetAttachedPropertyName(), null);
    }

    protected void SetSetterData(DependencyObject targetObject, SetterData setterData)
    {
      targetObject.SetAttachedPropertyValue(GetAttachedPropertyName(), setterData);
    }

    protected void ClearSetterData(DependencyObject targetObject)
    {
      targetObject.RemoveAttachedProperty(GetAttachedPropertyName());
    }

    #endregion

    #region Public methods

    public override void Set(UIElement element)
    {
      IDataDescriptor dd;
      DependencyObject targetObject;
      if (!FindPropertyDescriptor(element, out dd, out targetObject))
        return;
      SetterData setterData = GetSetterData(targetObject);
      if (setterData != null)
        // If any setter is currently setting our property, we don't want to interfere
        return;
      object obj;

      // The next lines are necessary because the render thread is setting our values.
      // If there's still a value pending to be set by the render thread, we would get an old, obsolete value if
      // we just copied dd.Value to _originalValue.
      element.GetPendingOrCurrentValue(dd, out obj);
      SetSetterData(targetObject, new SetterData(obj));

      object value = Value;
      if (TypeConverter.Convert(value, dd.DataType, out obj))
        if (ReferenceEquals(value, obj))
          SetDataDescriptorValueWithLP(dd, MpfCopyManager.DeepCopyCutLVPs(obj));
        else
          // Avoid creating a copy twice
          SetDataDescriptorValueWithLP(dd, obj);
      else
      {
        // Value is not compatible: We cannot execute
        ServiceRegistration.Get<ILogger>().Warn("Setter for property '{0}': Cannot convert value {1} to target type {2}", _propertyName,
            value == null ? "'null'" : ("of type " + value.GetType().Name), dd.DataType.Name);
        return;
      }
    }

    public override void Restore(UIElement element)
    {
      IDataDescriptor dd;
      DependencyObject targetObject;
      if (!FindPropertyDescriptor(element, out dd, out targetObject))
        return;
      SetterData setterData = GetSetterData(targetObject);
      if (setterData == null)
        return;
      element.SetValueInRenderThread(dd, setterData.OrigValue);
      ClearSetterData(targetObject);
    }

    #endregion
  }
}

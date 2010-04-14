#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers
{
  public class DataTrigger : TriggerBase, IAddChild<Setter>
  {
    #region Private fields

    protected AbstractProperty _bindingProperty;
    protected AbstractProperty _valueProperty;

    #endregion

    #region Ctor

    public DataTrigger()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _bindingProperty = new SProperty(typeof(object), null);
      _valueProperty = new SProperty(typeof(object));
    }

    void Attach()
    {
      _bindingProperty.Attach(OnBindingValueChanged);
    }

    void Detach()
    {
      _bindingProperty.Detach(OnBindingValueChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      DataTrigger t = (DataTrigger) source;
      Binding = copyManager.GetCopy(t.Binding);
      Value = copyManager.GetCopy(t.Value);
      Attach();
    }

    #endregion

    #region Public properties

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

    #endregion

    public override void Setup(UIElement element)
    {
      base.Setup(element);
      OnBindingValueChanged(_bindingProperty, null);
    }

    /// <summary>
    /// Listens for changes of our trigger property data descriptor.
    /// </summary>
    void OnBindingValueChanged(AbstractProperty bindingValue, object oldValue)
    {
      if (!IsInitialized)
        return;
      TriggerIfValuesEqual(bindingValue.GetValue(), Value);
    }

    #region IAddChild Members

    public void AddChild(Setter s)
    {
      Setters.Add(s);
    }

    #endregion
  }
}

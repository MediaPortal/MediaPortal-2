#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  /// <summary>
  /// Implements the MPF MultiBinding markup extension.
  /// </summary>
  public class MultiBindingMarkupExtension: BindingBase, IAddChild<BindingMarkupExtension>
  {
    #region Protected fields

    // Binding configuration properties
    protected IList<BindingMarkupExtension> _childBindings = new List<BindingMarkupExtension>();
    protected IMultiValueConverter _converter = null;
    protected object _converterParameter = null;
    protected AbstractProperty _modeProperty = new SProperty(typeof(BindingMode), BindingMode.Default);

    // State variables
    protected bool _retryBinding = false; // Our BindingDependency could not be established because there were problems evaluating the binding source value -> UpdateBinding has to be called again
    protected AbstractProperty _sourceValueValidProperty = new SProperty(typeof(bool), false); // Cache-valid flag to avoid unnecessary calls to UpdateSourceValue()
    protected bool _isUpdatingBinding = false; // Used to avoid recursive calls to method UpdateBinding
    protected bool _isUpdatingSourceValue = false; // Avoid recursive calls to method UpdateSourceValue
    protected ICollection<IDataDescriptor> _attachedDataDescriptors = new List<IDataDescriptor>();

    // Derived properties
    protected DataDescriptorRepeater _evaluatedSourceValue = new DataDescriptorRepeater();
    protected BindingDependency _bindingDependency = null;

    #endregion

    #region Ctor

    /// <summary>
    /// Creates a new <see cref="BindingMarkupExtension"/> instance.
    /// </summary>
    public MultiBindingMarkupExtension()
    {
      Attach();
    }

    public override void Dispose()
    {
      Detach();
      if (_bindingDependency != null)
      {
        _bindingDependency.Detach();
        _bindingDependency = null;
      }
      // Child bindings will be disposed automatically by DependencyObject.Dispose, because they are
      // added to our binding collection (was done by method AddChild)
      ResetBindingAttachments();
      base.Dispose();
    }

    protected void Attach()
    {
      _evaluatedSourceValue.Attach(OnSourceValueChanged);
      _modeProperty.Attach(OnBindingPropertyChanged);
    }

    protected void Detach()
    {
      _evaluatedSourceValue.Detach(OnSourceValueChanged);
      _modeProperty.Detach(OnBindingPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      MultiBindingMarkupExtension mbme = (MultiBindingMarkupExtension) source;
      Converter = copyManager.GetCopy(mbme.Converter);
      ConverterParameter = copyManager.GetCopy(mbme.ConverterParameter);
      Mode = copyManager.GetCopy(mbme.Mode);
      foreach (BindingMarkupExtension childBinding in mbme._childBindings)
        _childBindings.Add(copyManager.GetCopy(childBinding));
      Attach();
    }

    #endregion

    /// <summary>
    /// Evaluates an <see cref="IDataDescriptor"/> instance which is our
    /// evaluated source value (or value object). This data descriptor
    /// will be the source endpoint for the binding operation, if any.
    /// If this binding is used to update a target property, the returned data descriptor
    /// is used as value for the assignment to the target property.
    /// </summary>
    /// <param name="result">Returns the data descriptor for the binding's source value.
    /// This value is only valid if this method returns <c>true</c>.</param>
    /// <returns><c>true</c>, if the source value could be resolved,
    /// <c>false</c> if it could not be resolved (yet).</returns>
    public bool Evaluate(out IDataDescriptor result)
    {
      result = null;
      try
      {
        if (!IsSourceValueValid && !UpdateSourceValue())
          return false;
        result = _evaluatedSourceValue;
        return true;
      } catch
      {
        return false;
      }
    }

    #region Properties

    /// <summary>
    /// Gets the list of our child bindings.
    /// </summary>
    public IList<BindingMarkupExtension> Bindings
    {
      get { return _childBindings; }
    }

    /// <summary>
    /// Gets or sets the value converter to evaluate a result value from the values of our child bindings.
    /// </summary>
    public IMultiValueConverter Converter
    {
      get { return _converter; }
      set
      {
        _converter = value;
        OnBindingPropertyChanged(null, null);
      }
    }

    /// <summary>
    /// Gets or sets the parameter to be used for the value converter.
    /// </summary>
    public object ConverterParameter
    {
      get { return _converterParameter; }
      set {
        _converterParameter = value;
        OnBindingPropertyChanged(null, null);
      }
    }

    public AbstractProperty ModeProperty
    {
      get { return _modeProperty; }
    }

    public BindingMode Mode
    {
      get { return (BindingMode) _modeProperty.GetValue(); }
      set { _modeProperty.SetValue(value); }
    }

    /// <summary>
    /// Holds the evaluated source value for this binding. Clients may attach change handlers to the returned
    /// data descriptor; if the evaluated source value changes, this data descriptor will keep its identity,
    /// only the value will change.
    /// </summary>
    public IDataDescriptor EvaluatedSourceValue
    {
      get { return _evaluatedSourceValue; }
    }

    public AbstractProperty IsSourceValueValidProperty
    {
      get { return _sourceValueValidProperty; }
    }

    /// <summary>
    /// Returns the information if the <see cref="EvaluatedSourceValue"/> data descriptor contains a correctly
    /// bound value. This is the case, if the last call to <see cref="Evaluate"/> was successful.
    /// </summary>
    public bool IsSourceValueValid
    {
      get { return (bool) _sourceValueValidProperty.GetValue(); }
      set { _sourceValueValidProperty.SetValue(value); }
    }

    #endregion

    #region Event handlers

    /// <summary>
    /// Called when some of our binding properties changed.
    /// Will trigger an update of our source value here.
    /// </summary>
    /// <param name="property">The binding property which changed.</param>
    /// <param name="oldValue">The old value of the property.</param>
    protected void OnBindingPropertyChanged(AbstractProperty property, object oldValue)
    {
      if (_active)
        UpdateSourceValue();
    }

    /// <summary>
    /// Called when one of our child bindings changed its source value.
    /// Will trigger an update of our source value here.
    /// </summary>
    /// <param name="dd">The source value of the child binding.</param>
    protected void OnSourceBindingChanged(IDataDescriptor dd)
    {
      if (_active)
        UpdateSourceValue();
    }

    /// <summary>
    /// Called after a new source value was evaluated for this binding.
    /// We will update our binding here, if necessary.
    /// </summary>
    /// <param name="sourceValue">Our <see cref="_evaluatedSourceValue"/> data descriptor.</param>
    protected void OnSourceValueChanged(IDataDescriptor sourceValue)
    {
      if (_active && _retryBinding)
        UpdateBinding();
    }

    #endregion

    #region Protected properties and methods

    /// <summary>
    /// Attaches this multi binding to change events of the specified (child) binding <paramref name="bme"/>.
    /// </summary>
    /// <param name="bme">(Child) binding to be attached to.</param>
    protected void AttachToSourceBinding(BindingMarkupExtension bme)
    {
      IDataDescriptor dd = bme.EvaluatedSourceValue;
      dd.Attach(OnSourceBindingChanged);
      _attachedDataDescriptors.Add(dd);
    }

    /// <summary>
    /// Will reset all change handler attachments to source property and
    /// source path properties. This should be called before the evaluation path
    /// to the binding's source will be processed again.
    /// </summary>
    protected void ResetBindingAttachments()
    {
      foreach (IDataDescriptor dd in _attachedDataDescriptors)
        dd.Detach(OnSourceBindingChanged);
      _attachedDataDescriptors.Clear();
    }

    /// <summary>
    /// Does the lookup for the binding source data values of all child values.
    /// </summary>
    /// <param name="result">Array of evaluated values evaluated by our child bindings.</param>
    /// <returns><c>true</c>, if all values could be evaluated, <c>false</c> if not all values are
    /// available (yet).</returns>
    protected bool GetSourceValues(out IDataDescriptor[] result)
    {
      ResetBindingAttachments();
      result = null;
      IDataDescriptor[] values = new IDataDescriptor[_childBindings.Count];
      for (int i = 0; i < _childBindings.Count; i++)
      {
        BindingMarkupExtension bme = _childBindings[i];
        IDataDescriptor evaluatedValue;
        AttachToSourceBinding(bme);
        if (!bme.Evaluate(out evaluatedValue))
          return false;
        values[i] = evaluatedValue;
      }
      result = values;
      return true;
    }

    /// <summary>
    /// Will be called to evaluate our source value based on all available
    /// property and context states.
    /// This method will also be automatically re-called when any object involved in the
    /// evaluation process of our source value was changed.
    /// </summary>
    /// <returns><c>true</c>, if the source value based on all input data
    /// could be evaluated, else <c>false</c>.</returns>
    protected bool UpdateSourceValue()
    {
      if (_isUpdatingSourceValue)
        return false;
      _isUpdatingSourceValue = true;
      bool sourceValueValid = false;
      try
      {
        IDataDescriptor[] values;
        if (!GetSourceValues(out values))
            // Do nothing if not all necessary child bindings can be resolved at the current time
          return false;
        if (_converter == null)
          throw new XamlBindingException("MultiBindingMarkupExtension: Converter must be set");
        object result;
        Type targetType = _targetDataDescriptor == null ? typeof(object) : _targetDataDescriptor.DataType;
        if (!_converter.Convert(values, targetType, ConverterParameter, out result))
          return false;
        IsSourceValueValid = sourceValueValid = true;
        _evaluatedSourceValue.SourceValue = new ValueDataDescriptor(result);
        return true;
      }
      finally
      {
        IsSourceValueValid = sourceValueValid;
        _isUpdatingSourceValue = false;
      }
    }

    protected bool UpdateBinding()
    {
      // Avoid recursive calls: For instance, this can occur when the later call to Evaluate will change our evaluated
      // source value, which will cause a recursive call to UpdateBinding.
      if (_isUpdatingBinding)
        return false;
      _isUpdatingBinding = true;
      try
      {
        if (KeepBinding) // This is the case if our target descriptor has a binding type
        { // In this case, this instance should be used rather than the evaluated source value
          if (_targetDataDescriptor != null)
            _contextObject.SetBindingValue(_targetDataDescriptor, this);
          _retryBinding = false;
          return true;
        }
        IDataDescriptor sourceDd;
        if (!Evaluate(out sourceDd))
        {
          _retryBinding = true;
          return false;
        }

        if (Mode == BindingMode.TwoWay || Mode == BindingMode.OneWayToSource)
          throw new XamlBindingException("MultiBindingMarkupExtension doesn't support BindingMode.TwoWay and BindingMode.OneWayToSource");
        else if (Mode == BindingMode.OneTime)
        {
          _contextObject.SetBindingValue(_targetDataDescriptor, sourceDd.Value);
          _retryBinding = false;
          Dispose();
          return true; // In this case, we have finished with only assigning the value
        }
        // else Mode == BindingMode.OneWay || Mode == BindingMode.Default
        if (_bindingDependency != null)
          _bindingDependency.Detach();
        _bindingDependency = new BindingDependency(sourceDd, _targetDataDescriptor, true,
            UpdateSourceTrigger.Explicit, _contextObject, null, null);
        _retryBinding = false;
        return true;
      }
      finally
      {
        _isUpdatingBinding = false;
      }
    }

    #endregion

    #region IBinding implementation

    public override void Activate()
    {
      base.Activate();
      // Child bindings are never really "bound", because they are instantiated normally without binding them
      // to a target property, so we have to activate them manually
      foreach (BindingMarkupExtension childBinding in _childBindings)
        childBinding.Activate();
      UpdateBinding();
    }

    #endregion

    #region IAddChild<BindingMarkupExtension> implementation

    public void AddChild(BindingMarkupExtension o)
    {
      _childBindings.Add(o);
      o.AttachToTargetObject(this);
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return "MultiBinding [" + _childBindings.Count + " Children]";
    }

    #endregion
  }
}

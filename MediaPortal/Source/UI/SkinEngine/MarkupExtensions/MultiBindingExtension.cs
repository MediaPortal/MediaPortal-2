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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Markup;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  /// <summary>
  /// Implements the MPF MultiBinding markup extension.
  /// </summary>

  // Implementation hint about multithreading/locking:
  // Actually, all bindings suffer from the potential to be called from multiple threads because we cannot avoid that multiple threads
  // update model properties we're bound to. But currently, we only synchronize the MultiBinding against multithreading problems
  // because the probability that a MultiBinding is updated by multiple threads is much higher than the probability for a normal Binding.
  // This comment can also be found in BindingMarkupExtension.
  [ContentProperty("Bindings")]
  public class MultiBindingExtension: BindingBase, IAddChild<BindingExtension>
  {
    #region Protected fields

    // Binding configuration properties
    protected Collection<BindingExtension> _childBindings = new Collection<BindingExtension>();
    protected IMultiValueConverter _converter = null;
    protected object _converterParameter = null;
    protected AbstractProperty _modeProperty = new SProperty(typeof(BindingMode), BindingMode.Default);
    protected AbstractProperty _allowEmptyBindingProperty = new SProperty(typeof(bool), false);

    // State variables
    protected object _syncObj = new object();
    protected bool _valueAssigned = false; // Our BindingDependency could not be established because there were problems evaluating the binding source value -> UpdateBinding has to be called again
    protected AbstractProperty _sourceValueValidProperty = new SProperty(typeof(bool), false); // Cache-valid flag to avoid unnecessary calls to UpdateSourceValue()
    protected bool _isUpdatingBinding = false; // Used to avoid recursive calls to method UpdateBinding
    protected bool _isUpdatingSourceValue = false; // Avoid recursive calls to method UpdateSourceValue
    protected ICollection<IDataDescriptor> _attachedDataDescriptors = new List<IDataDescriptor>();

    // Derived properties
    protected DataDescriptorRepeater _evaluatedSourceValue = new DataDescriptorRepeater();
    protected BindingDependency _bindingDependency = null;

    protected IDataDescriptor _lastUpdatedValue = null;

    #endregion

    #region Ctor

    /// <summary>
    /// Creates a new <see cref="BindingExtension"/> instance.
    /// </summary>
    public MultiBindingExtension()
    {
      Attach();
    }

    public override void Dispose()
    {
      lock (_syncObj)
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
    }

    protected void Attach()
    {
      _evaluatedSourceValue.Attach(OnSourceValueChanged);
      _modeProperty.Attach(OnBindingPropertyChanged);
      _allowEmptyBindingProperty.Attach(OnBindingPropertyChanged);
    }

    protected void Detach()
    {
      _evaluatedSourceValue.Detach(OnSourceValueChanged);
      _modeProperty.Detach(OnBindingPropertyChanged);
      _allowEmptyBindingProperty.Detach(OnBindingPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      MultiBindingExtension mbme = (MultiBindingExtension) source;
      Converter = copyManager.GetCopy(mbme.Converter);
      ConverterParameter = copyManager.GetCopy(mbme.ConverterParameter);
      Mode = mbme.Mode;
      AllowEmptyBinding = mbme.AllowEmptyBinding;
      foreach (BindingExtension childBinding in mbme._childBindings)
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
    public Collection<BindingExtension> Bindings
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

    public AbstractProperty AllowEmptyBindingProperty
    {
      get { return _allowEmptyBindingProperty; }
    }

    public bool AllowEmptyBinding
    {
      get { return (bool) _allowEmptyBindingProperty.GetValue(); }
      set { _allowEmptyBindingProperty.SetValue(value); }
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
      if (_active && !_valueAssigned)
        UpdateBinding();
    }

    #endregion

    #region Protected properties and methods

    /// <summary>
    /// Attaches this multi binding to change events of the specified (child) binding <paramref name="bme"/>.
    /// </summary>
    /// <param name="bme">(Child) binding to be attached to.</param>
    protected void AttachToSourceBinding(BindingExtension bme)
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
      bool allowEmptyBinding = AllowEmptyBinding;
      IDataDescriptor[] values = new IDataDescriptor[_childBindings.Count];
      for (int i = 0; i < _childBindings.Count; i++)
      {
        BindingExtension bme = _childBindings[i];
        IDataDescriptor evaluatedValue;
        AttachToSourceBinding(bme);
        if (!bme.Evaluate(out evaluatedValue) && !allowEmptyBinding)
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
        object result;
        bool copy = false;
        lock (_syncObj)
        {
          IDataDescriptor[] values;
          if (!GetSourceValues(out values))
              // Do nothing if not all necessary child bindings can be resolved at the current time
            return false;
          if (_converter == null)
            throw new XamlBindingException("MultiBindingMarkupExtension: Converter must be set");
          Type targetType = _targetDataDescriptor == null ? typeof(object) : _targetDataDescriptor.DataType;
          if (!_converter.Convert(values, targetType, ConverterParameter, out result))
            return false;
          copy = values.Any(dd => dd != null && ReferenceEquals(dd.Value, result));
          IsSourceValueValid = sourceValueValid = true;
        }
        object oldValue = _evaluatedSourceValue.SourceValue;
        // Set the binding's value outside the lock to comply with the MP2 threading policy
        _evaluatedSourceValue.SourceValue = new ValueDataDescriptor(copy ? MpfCopyManager.DeepCopyCutLVPs(result) : result);
        if (oldValue != null)
          MPF.TryCleanupAndDispose(oldValue);
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
          _valueAssigned = true;
          return true;
        }
        IDataDescriptor sourceDd;
        lock (_syncObj)
          if (!Evaluate(out sourceDd))
          {
            _valueAssigned = false;
            return false;
          }

        // We're called multiple times, for example when a resource dictionary changes.
        // To avoid too many updates, we remember the last updated value.
        if (ReferenceEquals(sourceDd, _lastUpdatedValue) && !_valueAssigned)
          return true;
        _lastUpdatedValue = sourceDd;

        // Don't lock the following lines because the binding dependency object updates source or target objects.
        // Holding a lock during that process would offend against the MP2 threading policy.

        if (_bindingDependency != null)
          _bindingDependency.Detach();
        _bindingDependency = null;
        switch (Mode)
        {
          case BindingMode.TwoWay:
          case BindingMode.OneWayToSource:
            throw new XamlBindingException(
                "MultiBindingMarkupExtension doesn't support BindingMode.TwoWay and BindingMode.OneWayToSource");
          case BindingMode.OneTime:
            object value = sourceDd.Value;
            _contextObject.SetBindingValue(_targetDataDescriptor, value);
            _valueAssigned = true;
            Dispose();
            return true; // In this case, we have finished with only assigning the value
          default: // Mode == BindingMode.OneWay || Mode == BindingMode.Default
            _bindingDependency = new BindingDependency(sourceDd, _targetDataDescriptor, true,
                UpdateSourceTrigger.Explicit, null, null, null);
            _valueAssigned = true;
            break;
        }
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
      if (_active)
        return;
      base.Activate();
      // Child bindings are never really "bound", because they are instantiated normally without binding them
      // to a target property, so we have to activate them manually
      foreach (BindingExtension childBinding in _childBindings)
        childBinding.Activate();
      UpdateBinding();
    }

    #endregion

    #region IAddChild<BindingMarkupExtension> implementation

    public void AddChild(BindingExtension o)
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

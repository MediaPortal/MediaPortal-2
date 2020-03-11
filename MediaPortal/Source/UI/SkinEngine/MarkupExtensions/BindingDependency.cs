#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common.Localization;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  /// <summary>
  /// Handles the dependency between two data endpoints.
  /// </summary>
  public class BindingDependency
  {
    protected IDataDescriptor _sourceDd;
    protected IDataDescriptor _targetDd;
    protected DependencyObject _targetObject;
    protected DependencyObject _sourceObject;
    protected IValueConverter _valueConverter;
    protected object _converterParameter;
    protected bool _notifyOnSourceUpdated;
    protected bool _notifyOnTargetUpdated;
    protected bool _attachedToSource = false;
    protected bool _attachedToTarget = false;
    protected bool _attachedToLostFocus = false;
    protected UIElement _parentUiElement = null;

    /// <summary>
    /// Creates a new <see cref="BindingDependency"/> object.
    /// </summary>
    /// <param name="sourceDd">Souce data descriptor for the dependency.</param>
    /// <param name="targetDd">Target data descriptor for the dependency.</param>
    /// <param name="autoAttachToSource">If set to <c>true</c>, the new dependency object will be
    /// automatically attached to the <paramref name="sourceDd"/> data descriptor. This means it will
    /// capture changes from it and reflect them on the <paramref name="targetDd"/> data descriptor.</param>
    /// <param name="updateSourceTrigger">This parameter controls, which target object event makes this
    /// binding dependency copy the target value to the <paramref name="sourceDd"/> data descriptor.
    /// If set to <see cref="UpdateSourceTrigger.PropertyChanged"/>, the new binding dependency object
    /// will automatically attach to property changes of the <paramref name="targetDd"/> data descriptor and
    /// reflect the changed value to the <paramref name="sourceDd"/> data descriptor. If set to
    /// <see cref="UpdateSourceTrigger.LostFocus"/>, the new binding dependency will attach to the
    /// <see cref="UIElement.EventOccured"/> event of the <paramref name="parentUiElement"/> object.
    /// If set to <see cref="UpdateSourceTrigger.Explicit"/>, the new binding dependency won't attach to
    /// the target at all.</param>
    /// <param name="parentUiElement">The parent <see cref="UIElement"/> of the specified <paramref name="targetDd"/>
    /// data descriptor. This parameter is used to raise the source/target updated events if
    /// <paramref name="notifyOnSourceUpdated"/> or <paramref name="notifyOntargetUpdated"/> are set to <c>true</c>
    /// and attach to the lost focus event if <paramref name="updateSourceTrigger"/> is set to
    /// <see cref="UpdateSourceTrigger.LostFocus"/>.</param>
    /// <param name="customValueConverter">Set a custom value converter with this parameter. If this parameter
    /// is set to <c>null</c>, the default <see cref="TypeConverter"/> will be used.</param>
    /// <param name="customValueConverterParameter">Parameter to be used in the custom value converter, if one is
    /// set.</param>
    /// <param name="notifyOnSourceUpdated">If set to <c>true</c>, the new dependency object will raise a
    /// <see cref="BindingExtension.SourceUpdatedEvent"/></param> on the <paramref name="parentUiElement"/>
    /// when the <paramref name="sourceDd"/> is updated with a value from the <paramref name="targetDd"/>.
    /// <param name="notifyOntargetUpdated">If set to <c>true</c>, the new dependency object will raise a
    /// <see cref="BindingExtension.TargetUpdatedEvent"/></param> on the <paramref name="parentUiElement"/>
    /// when the <paramref name="targetDd"/> is updated with a value from the <paramref name="sourceDd"/>.
    public BindingDependency(IDataDescriptor sourceDd, IDataDescriptor targetDd, bool autoAttachToSource,
        UpdateSourceTrigger updateSourceTrigger, UIElement parentUiElement,
        IValueConverter customValueConverter, object customValueConverterParameter,
        bool notifyOnSourceUpdated, bool notifyOntargetUpdated)
    {
      _sourceDd = sourceDd;
      _targetDd = targetDd;
      _targetObject = _targetDd.TargetObject as DependencyObject;
      _sourceObject = _sourceDd.TargetObject as DependencyObject;
      _valueConverter = customValueConverter;
      _converterParameter = customValueConverterParameter;
      _notifyOnSourceUpdated = notifyOnSourceUpdated;
      _notifyOnTargetUpdated = notifyOntargetUpdated;
      _parentUiElement = parentUiElement;
      if (autoAttachToSource && sourceDd.SupportsChangeNotification)
      {
        sourceDd.Attach(OnSourceChanged);
        _attachedToSource = true;
      }
      if (targetDd.SupportsChangeNotification)
      {
        if (updateSourceTrigger == UpdateSourceTrigger.PropertyChanged)
        {
          targetDd.Attach(OnTargetChanged);
          _attachedToTarget = true;
        }
        else if (updateSourceTrigger == UpdateSourceTrigger.LostFocus)
        {
          if (parentUiElement != null)
            parentUiElement.EventOccured += OnTargetElementEventOccured;
          _attachedToLostFocus = true;
        }
      }
      // Initially update endpoints
      if (autoAttachToSource)
        UpdateTarget();
      if (updateSourceTrigger != UpdateSourceTrigger.Explicit &&
          !autoAttachToSource) // If we are attached to both, only update one direction
        UpdateSource();
    }

    protected void OnTargetElementEventOccured(string eventName)
    {
      if (eventName == FrameworkElement.LOSTFOCUS_EVENT)
        UpdateSource();
    }

    protected void OnSourceChanged(IDataDescriptor source)
    {
      UpdateTarget();
    }

    protected void OnTargetChanged(IDataDescriptor target)
    {
      UpdateSource();
    }

    protected bool Convert(object val, Type targetType, out object result)
    {
      if (_valueConverter != null)
        return _valueConverter.Convert(val, targetType, _converterParameter, ServiceRegistration.Get<ILocalization>().CurrentCulture, out result);
      return TypeConverter.Convert(val, targetType, out result);
    }

    protected bool ConvertBack(object val, Type targetType, out object result)
    {
      if (_valueConverter != null)
        return _valueConverter.ConvertBack(val, targetType, _converterParameter, ServiceRegistration.Get<ILocalization>().CurrentCulture, out result);
      return TypeConverter.Convert(val, targetType, out result);
    }

    /// <summary>
    /// Gets or sets a custom value converter.
    /// </summary>
    public IValueConverter Converter
    {
      get { return _valueConverter; }
      set { _valueConverter = value; }
    }

    /// <summary>
    /// Gets or sets the parameter for the custom value converter.
    /// </summary>
    public object ConverterParameter
    {
      get { return _converterParameter; }
      set { _converterParameter = value; }
    }

    public void Detach()
    {
      if (_attachedToSource)
        _sourceDd.Detach(OnSourceChanged);
      _attachedToSource = false;
      if (_attachedToTarget)
        _targetDd.Detach(OnTargetChanged);
      _attachedToTarget = false;
      if (_attachedToLostFocus && _parentUiElement != null)
        _parentUiElement.EventOccured -= OnTargetElementEventOccured;
    }

    public void UpdateSource()
    {
      object convertedValue;
      if (!ConvertBack(_targetDd.Value, _sourceDd.DataType, out convertedValue))
        return;
      if (ReferenceEquals(_targetDd.Value, convertedValue))
        convertedValue = MpfCopyManager.DeepCopyCutLVPs(convertedValue);
      if (_sourceObject != null)
        _sourceObject.SetBindingValue(_sourceDd, convertedValue);
      else
      _sourceDd.Value = convertedValue;
      if (_notifyOnSourceUpdated && _parentUiElement != null)
        _parentUiElement.RaiseEvent(new DataTransferEventArgs(BindingExtension.SourceUpdatedEvent, _parentUiElement, _targetDd));
    }

    public void UpdateTarget()
    {
      object convertedValue;
      if (!Convert(_sourceDd.Value, _targetDd.DataType, out convertedValue))
        return;
      if (ReferenceEquals(_sourceDd.Value, convertedValue))
        convertedValue = MpfCopyManager.DeepCopyCutLVPs(convertedValue);
      if (_targetObject != null)
        _targetObject.SetBindingValue(_targetDd, convertedValue);
      else
        _targetDd.Value = convertedValue;
      if (_notifyOnTargetUpdated && _parentUiElement != null)
        _parentUiElement.RaiseEvent(new DataTransferEventArgs(BindingExtension.TargetUpdatedEvent, _parentUiElement, _targetDd));
    }
  }
}

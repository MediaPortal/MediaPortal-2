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

using MP2BootstrapperApp.Localization;
using System;
using System.Windows;
using System.Windows.Data;

namespace MP2BootstrapperApp.MarkupExtensions
{
  /// <summary>
  /// Provides the translated value of a string with a given string id and propagates changes to the bound target.
  /// </summary>
  public class LocalizeValueProvider : DisposableValueProvider
  {
    #region Attached properties

    /// <summary>
    /// Attached property to store this instance on the target.
    /// </summary>
    public static readonly DependencyProperty LocalizeValueProviderProperty = DependencyProperty.RegisterAttached(
      "LocalizeValueProvider", typeof(LocalizeValueProvider), typeof(LocalizeValueProvider), new PropertyMetadata(OnPropertyChanged)
    );

    /// <summary>
    /// Attached property to bind the StringId binding to, needed as the binding needs to be bound
    /// to a DependencyObject in the logical tree so that the appropriate DataContext is available.
    /// </summary>
    public static readonly DependencyProperty LocalizeIdProperty = DependencyProperty.RegisterAttached(
      "LocalizeId", typeof(string), typeof(LocalizeValueProvider), new PropertyMetadata(OnPropertyChanged)
    );

    public static readonly DependencyProperty LocalizeParametersProperty = DependencyProperty.RegisterAttached(
      "LocalizeParameters", typeof(object), typeof(LocalizeValueProvider), new PropertyMetadata(OnPropertyChanged)
    );

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      // One of the dependency properties has changed, update the StringId value of the provider.
      LocalizeValueProvider source = d.GetValue(LocalizeValueProviderProperty) as LocalizeValueProvider;
      if (source != null)
        source.Update(d.GetValue(LocalizeIdProperty) as string, d.GetValue(LocalizeParametersProperty));
    }

    #endregion

    private string _stringId;
    private object[] _parameters;
    private ILanguageChanged _localization;

    protected bool _isUpdating = false;
    protected bool _hasChanged = false;

    /// <summary>
    /// Creates a new instance of <see cref="LocalizeValueProvider"/> that listens for changes to the <paramref name="stringIdBinding"/>
    /// and language and updates the the <paramref name="target"/> with the translated value.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="stringIdBinding"></param>
    /// <param name="parametersBinding"></param>
    /// <param name="localization"></param>
    public LocalizeValueProvider(DependencyObject target, BindingBase stringIdBinding, BindingBase parametersBinding, ILanguageChanged localization)
    : this(localization)
    {
      AttachTarget(target, stringIdBinding, parametersBinding);
    }

    public LocalizeValueProvider(ILanguageChanged localization)
    {
      _localization = localization;
      AttachLanguageChanged();
    }

    /// <summary>
    /// Updates the string id and parameters, deferring any change notifications until the update is complete.
    /// </summary>
    /// <param name="stringId">The updated string id.</param>
    /// <param name="parameters">The updated paramters.</param>
    public void Update(string stringId, object parameters)
    {
      _isUpdating = true;
      bool hasChanged;
      try
      {
        StringId = stringId;
        Parameters = GetParameters(parameters);
      }
      finally
      {
        hasChanged = _hasChanged;
        _hasChanged = false;
        _isUpdating = false;
      }

      if (hasChanged)
        RaisePropertyChanged(nameof(Value));
    }

    protected object[] GetParameters(object parameters)
    {
      if (parameters == null)
        return null;
      else if (parameters is object[] array)
        return array;
      else
        return new object[] { parameters };
    }

    /// <summary>
    /// Id of the string to translate.
    /// </summary>
    public string StringId
    {
      get { return _stringId; }
      set
      {
        if (_stringId == value)
          return;
        _stringId = value;
        // String id has changed, trigger an update of the translated value.
        if (_isUpdating)
          _hasChanged = true;
        else
          RaisePropertyChanged(nameof(Value));
      }
    }

    /// <summary>
    /// Parameters to pass when formatting the translated string.
    /// </summary>
    public object[] Parameters
    {
      get { return _parameters; }
      set
      {
        if (_parameters == value)
          return;
        _parameters = value;
        // Paramters have changed, trigger an update of the translated value.
        if (_isUpdating)
          _hasChanged = true;
        else
          RaisePropertyChanged(nameof(Value));
      }
    }

    /// <summary>
    /// Translated value.
    /// </summary>
    public object Value
    {
      get
      {
        return _localization?.ToString(_stringId, _parameters) ?? _stringId;
      }
    }

    /// <summary>
    /// Attaches this instance and the specified binding to the target.
    /// </summary>
    /// <param name="target">The target DependencyObject that recieves the translated string.</param>
    /// <param name="stringIdBinding">The binding that is bound to the string id value.</param>
    /// <param name="parametersBinding">The binding that is bound to the parameters value.</param>
    private void AttachTarget(DependencyObject target, BindingBase stringIdBinding, BindingBase parametersBinding)
    {
      // Bind the string id binding, paramter binding and this instance to the target, when the bindings change the
      // Attached property's change handler will update this instance with the new values.
      BindingOperations.ClearBinding(target, LocalizeIdProperty);
      BindingOperations.SetBinding(target, LocalizeIdProperty, stringIdBinding);
      BindingOperations.ClearBinding(target, LocalizeParametersProperty);
      BindingOperations.SetBinding(target, LocalizeParametersProperty, parametersBinding);
      target.SetValue(LocalizeValueProviderProperty, this);
    }

    private void AttachLanguageChanged()
    {
      if (_localization != null)
        WeakEventManager<ILanguageChanged, EventArgs>.AddHandler(_localization, nameof(ILanguageChanged.LanguageChanged), OnLanguageChanged);
    }

    private void DetachLanguageChanged()
    {
      if (_localization != null)
        WeakEventManager<ILanguageChanged, EventArgs>.RemoveHandler(_localization, nameof(ILanguageChanged.LanguageChanged), OnLanguageChanged);
    }

    private void OnLanguageChanged(object sender, EventArgs e)
    {
      RaisePropertyChanged(nameof(Value));
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
        DetachLanguageChanged();
      base.Dispose(disposing);
    }
  }
}

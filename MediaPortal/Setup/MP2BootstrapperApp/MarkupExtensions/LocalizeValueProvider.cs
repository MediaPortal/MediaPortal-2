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
  /// Implementation of <see cref="FreezableCollection{T}"/> for storing a collection of <see cref="LocalizeValueProvider"/> on a <see cref="DependencyObject"/>.
  /// </summary>
  public class LocalizeValueProviderCollection : FreezableCollection<LocalizeValueProvider>
  {
    /// <summary>
    /// Removes and disposes any <see cref="LocalizeValueProvider"/> that is bound to the specified <paramref name="targetProperty"/>.
    /// </summary>
    /// <param name="targetProperty"></param>
    public void RemoveExistingValueProvider(DependencyProperty targetProperty)
    {
      for (int i = 0; i < Count; i++)
      {
        LocalizeValueProvider valueProvider = this[i];
        if (valueProvider.Targetproperty != targetProperty)
          continue;
        Remove(valueProvider);
        valueProvider.Dispose();
        break;
      }
    }
  }

  /// <summary>
  /// Provides the translated value of a string with a given string id and propagates changes to the bound target.
  /// </summary>
  public class LocalizeValueProvider : DisposableValueProvider
  {
    #region Dependency properties

    /// <summary>
    /// Dependency property for the StringId property.
    /// </summary>
    public static readonly DependencyProperty StringIdProperty = DependencyProperty.Register(
      "StringId", typeof(string), typeof(LocalizeValueProvider), new PropertyMetadata(OnPropertyChanged)
    );

    /// <summary>
    /// Dependency property for the Parameters property.
    /// </summary>
    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
      "Parameters", typeof(object), typeof(LocalizeValueProvider), new PropertyMetadata(OnPropertyChanged)
    );

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      // One of the dependency properties has changed, update the provider.
      LocalizeValueProvider lvp = d as LocalizeValueProvider;
      if (lvp != null)
        lvp.FireChanged();
    }

    #endregion

    private ILanguageChanged _localization;

    /// <summary>
    /// Creates a new instance of <see cref="LocalizeValueProvider"/> that listens for changes to the <paramref name="stringIdBinding"/>,
    /// <paramref name="parametersBinding"/> and language and updates the translated value.
    /// </summary>
    /// <param name="targetProperty"></param>
    /// <param name="stringIdBinding"></param>
    /// <param name="parametersBinding"></param>
    /// <param name="localization"></param>
    public LocalizeValueProvider(DependencyProperty targetProperty, BindingBase stringIdBinding, BindingBase parametersBinding, ILanguageChanged localization)
    : this(localization)
    {
      Targetproperty = targetProperty;
      SetBindings(stringIdBinding, parametersBinding);
    }

    protected LocalizeValueProvider(ILanguageChanged localization)
    {
      _localization = localization;
      AttachLanguageChanged();
    }

    /// <summary>
    /// Notifies any listeners that the translated value might have changed.
    /// </summary>
    public void FireChanged()
    {
      RaisePropertyChanged(nameof(Value));
    }

    /// <summary>
    /// The depndency property that the <see cref="Value"/> property is bound to.
    /// </summary>
    public DependencyProperty Targetproperty { get; }

    /// <summary>
    /// Id of the string to translate.
    /// </summary>
    public string StringId
    {
      get { return (string)GetValue(StringIdProperty); }
      set { SetValue(StringIdProperty, value); }
    }

    /// <summary>
    /// Parameters to pass when formatting the translated string.
    /// </summary>
    public object Parameters
    {
      get { return GetValue(ParametersProperty); }
      set { SetValue(ParametersProperty, value); }
    }

    /// <summary>
    /// Translated value.
    /// </summary>
    public object Value
    {
      get
      {
        string stringId = StringId;
        return _localization?.ToString(StringId, GetParameters(Parameters)) ?? stringId;
      }
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
    /// Binds the specified bindings to the dependency properties.
    /// </summary>
    /// <param name="stringIdBinding">The binding that is bound to the string id value.</param>
    /// <param name="parametersBinding">The binding that is bound to the parameters value.</param>
    protected void SetBindings(BindingBase stringIdBinding, BindingBase parametersBinding)
    {
      // Bind the string id binding, paramter binding and this instance to the target, when the bindings change the
      // Attached property's change handler will update this instance with the new values.
      BindingOperations.ClearBinding(this, StringIdProperty);
      BindingOperations.SetBinding(this, StringIdProperty, stringIdBinding);
      BindingOperations.ClearBinding(this, ParametersProperty);
      BindingOperations.SetBinding(this, ParametersProperty, parametersBinding);
    }

    /// <summary>
    /// Clears all bindings from the dependency properties.
    /// </summary>
    protected void ClearBindings()
    {
      BindingOperations.ClearBinding(this, StringIdProperty);
      BindingOperations.ClearBinding(this, ParametersProperty);
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
      FireChanged();
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        DetachLanguageChanged();
        ClearBindings();
      }
      base.Dispose(disposing);
    }

    protected override Freezable CreateInstanceCore()
    {
      return new LocalizeValueProvider(_localization);
    }
  }
}

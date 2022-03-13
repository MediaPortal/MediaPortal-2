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
  /// Provides that translated value of a string with a given string id and propagates changes to the bound target.
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
    /// to a DependencyObject in the logical tree so that appropriate DataContext is available.
    /// </summary>
    public static readonly DependencyProperty LocalizeIdProperty = DependencyProperty.RegisterAttached(
      "LocalizeId", typeof(string), typeof(LocalizeValueProvider), new PropertyMetadata(OnPropertyChanged)
    );

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      // One of the dependency properties has changed, update the StringId value of the provider.
      LocalizeValueProvider source = d.GetValue(LocalizeValueProviderProperty) as LocalizeValueProvider;
      if (source != null)
        source.StringId = d.GetValue(LocalizeIdProperty) as string;
    }

    #endregion

    private string _stringId;
    private ILanguageChanged _localization;

    /// <summary>
    /// Creates a new instance of <see cref="LocalizeValueProvider"/> that listens for changes to the <paramref name="stringIdBinding"/>
    /// and language and updates the the <paramref name="target"/> with the translated value.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="stringIdBinding"></param>
    /// <param name="localization"></param>
    public LocalizeValueProvider(DependencyObject target, BindingBase stringIdBinding, ILanguageChanged localization)
    : this(localization)
    {
      AttachTarget(target, stringIdBinding);
    }

    protected LocalizeValueProvider(ILanguageChanged localization)
    {
      _localization = localization;
      AttachLanguageChanged();
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
        return _localization?.ToString(_stringId) ?? _stringId;
      }
    }

    /// <summary>
    /// Attaches this instance and the specified binding to the target.
    /// </summary>
    /// <param name="target">The target DependencyObject that recieves the translated string.</param>
    /// <param name="stringIdBinding">The binding that is bound to the string id value.</param>
    private void AttachTarget(DependencyObject target, BindingBase stringIdBinding)
    {
      // Bind the string id binding and this instance to the target, when the binding changes the
      // Attached property's change handler will update this instance with the new id.
      BindingOperations.ClearBinding(target, LocalizeIdProperty);
      BindingOperations.SetBinding(target, LocalizeIdProperty, stringIdBinding);
      target.SetValue(LocalizeValueProviderProperty, this);
    }

    private void AttachLanguageChanged()
    {
      if (_localization is ILanguageChanged languageChanged)
        WeakEventManager<ILanguageChanged, EventArgs>.AddHandler(languageChanged, nameof(ILanguageChanged.LanguageChanged), OnLanguageChanged);
    }

    private void DetachLanguageChanged()
    {
      if (_localization is ILanguageChanged languageChanged)
        WeakEventManager<ILanguageChanged, EventArgs>.RemoveHandler(languageChanged, nameof(ILanguageChanged.LanguageChanged), OnLanguageChanged);
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

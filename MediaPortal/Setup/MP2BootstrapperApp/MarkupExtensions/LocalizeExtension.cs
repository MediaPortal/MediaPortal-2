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
using MP2BootstrapperApp.Localization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace MP2BootstrapperApp.MarkupExtensions
{
  /// <summary>
  /// Implementation of <see cref="UpdatableMarkupExtension"/> that can bind to a DependencyProperty and provide a
  /// translated string that gets updated if the language is changed.
  /// </summary>
  public class LocalizeExtension : UpdatableMarkupExtension
  {
    #region Attached properties

    /// <summary>
    /// Attached property to contain all <see cref="LocalizeValueProvider"/>s bound to a <see cref="DependencyObject"/>.
    /// </summary>
    public static readonly DependencyProperty LocalizeValueProvidersProperty = DependencyProperty.RegisterAttached(
      "LocalizeValueProviders", typeof(LocalizeValueProviderCollection), typeof(LocalizeExtension)
    );

    #endregion

    protected string _stringId;
    protected string _parameters;
    protected BindingBase _stringIdBinding;
    protected BindingBase _parametersBinding;
    private ILanguageChanged _localization;

    /// <summary>
    /// Creates a new instance of <see cref="LocalizeExtension"/> that provides the translated string with the specified id.
    /// </summary>
    /// <param name="stringId">The id of the string to translate.</param>
    public LocalizeExtension(string stringId)
     : this()
    {
      _stringId = stringId;
    }

    /// <summary>
    /// Creates a new instance of <see cref="LocalizeExtension"/>, callers must set either
    /// <see cref="StringId"/> or <see cref="StringIdBinding"/> before using the class.
    /// </summary>
    public LocalizeExtension()
    {
      _localization = ServiceRegistration.Get<ILanguageChanged>(false);
    }

    /// <summary>
    /// The id of the string to translate. Updating this will automatically update the translation.<br/>
    /// If <see cref="StringIdBinding"/> is set then this value will be ignored.
    /// </summary>
    [ConstructorArgument("stringId")]
    public string StringId
    {
      get { return _stringId; }
      set
      {
        if (_stringId == value)
          return;
        _stringId = value;
        RaisePropertyChanged();
      }
    }

    /// <summary>
    /// Parameters to pass when formatting the translated string. Updating this will automatically update the translation.<br/>
    /// If <see cref="ParametersBinding"/> is set then this value will be ignored.
    /// </summary>
    public string Parameters
    {
      get { return _parameters; }
      set
      {
        if (_parameters == value)
          return;
        _parameters = value;
        RaisePropertyChanged();
      }
    }

    /// <summary>
    /// The binding to use to get the id of the string to translate.<br/>
    /// If set, this will be preferred over <see cref="StringId"/>.
    /// </summary>
    /// <remarks>
    /// WPF only allows binding to DependencyObjects, and MarkupExtensions cannot
    /// inherit from DependencyObject, so this separate property with the specific binding
    /// type is needed so WPF allows a binding to passed in.
    /// </remarks>
    public BindingBase StringIdBinding
    {
      get { return _stringIdBinding; }
      set { _stringIdBinding = value; }
    }

    /// <summary>
    /// The binding to use to get the paramters to use when formatting the translated string.<br/>
    /// If set, this will be preferred over <see cref="Parameters"/>.
    /// </summary>
    /// <remarks>
    /// WPF only allows binding to DependencyObjects, and MarkupExtensions cannot
    /// inherit from DependencyObject, so this separate property with the specific binding
    /// type is needed so WPF allows a binding to passed in.
    /// </remarks>
    public BindingBase ParametersBinding
    {
      get { return _parametersBinding; }
      set { _parametersBinding = value; }
    }

    protected override BindingBase ProvideValueOverride(DependencyObject target, DependencyProperty targetProperty)
    {
      // If we have a binding then use that directly, else create a binding to the StringId property so it's changes are propagated to the target
      BindingBase idBinding = _stringIdBinding != null ? _stringIdBinding : new Binding(nameof(StringId)) { Source = this };
      BindingBase parametersBinding = _parametersBinding != null ? _parametersBinding : new Binding(nameof(Parameters)) { Source = this };

      // Create a new LocalizeValueProvider and add it to the target object
      LocalizeValueProvider lvp = new LocalizeValueProvider(targetProperty, idBinding, parametersBinding, _localization);
      AddValueProviderToTarget(lvp, target, targetProperty);

      // Return a binding to the Value property on the the LocalizeValueProvider which will get updated when the properties/language changes
      return new Binding(nameof(LocalizeValueProvider.Value))
      {
        Source = lvp
      };
    }

    protected void AddValueProviderToTarget(LocalizeValueProvider valueProvider, DependencyObject target, DependencyProperty targetProperty)
    {
      // Get the value providers currently attached to the target
      LocalizeValueProviderCollection localizeValueProviders = target.GetValue(LocalizeValueProvidersProperty) as LocalizeValueProviderCollection;
      if (localizeValueProviders == null)
      {
        localizeValueProviders = new LocalizeValueProviderCollection();
        target.SetValue(LocalizeValueProvidersProperty, localizeValueProviders);
      }
      else
      {
        // Remove and dispose any existing value providers bound to the same target property
        localizeValueProviders.RemoveExistingValueProvider(targetProperty);
      }
      localizeValueProviders.Add(valueProvider);
    }
  }
}

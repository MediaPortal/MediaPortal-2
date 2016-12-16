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
using MediaPortal.Core.General;

namespace MyPlugin.Models
{
  /// <summary>
  /// Template for a simple model.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Models are used for providing data from the system to the skin and for executing actions (commands)
  /// which are triggered by the Skin, for example by clicking a button.
  /// </para>
  /// <para>
  /// All public properties can be data-bound by the skin.
  /// Note that properties, which are updated by the model and whose new value should be propagated to the
  /// skin, must be backed by an instance of <see cref="AbstractProperty"/>. Such a property is called a "dependency property".
  /// That instance must be made available to the skin engine by publishing it under the same name as the actual property plus "Property".
  /// In models, always <see cref="WProperty"/> instances are used.
  /// </para>
  /// <para>
  /// Simple models cannot attend the workflow. To attend the workflow, use a workflow model (see <see cref="MyWorkflowModel"/>).
  /// </para>
  /// <para>
  /// To make an UI model known by the system (and thus loadable by the skin), it is necessary to register it
  /// in the <c>plugin.xml</c> file.
  /// </para>
  /// </remarks>
  public class MySimpleModel : IDisposable
  {
    #region Consts

    /// <summary>
    /// This is a localized string resource.
    /// </summary>
    /// <remarks>
    /// Localized string resources always look like this:
    /// <example>
    /// [Section.Name]
    /// </example>
    /// Localized resources must be present at least in the english language, as this is the default.
    /// In the english language file of this plugin, you'll find the translation of this string.
    /// The language file in this template is located at: /Language/strings_en.xml
    /// 
    /// Choose meaningful string names, such as <c>[MyPlugin.MenuDisplayLabelHomeMenu]</c>.
    /// </remarks>
    protected const string RES_1 = "[MyPlugin.String1]";

    /// <summary>
    /// Second localized string.
    /// </summary>
    protected const string RES_2 = "[MyPlugin.String2]";

    #endregion

    #region Protected fields

    protected readonly AbstractProperty _valueProperty;

    #endregion

    #region Ctor & maintainance

    public MySimpleModel()
    {
      // Always instantiate WProperty dependency properties in models to be data-bound from the skin
      _valueProperty = new WProperty(typeof(string), string.Empty);
    }

    public void Dispose()
    {
      // Optional disposal method. If not needed, remove the IDisposable interface declaration and this method
    }

    #endregion

    #region Public members

    /// <summary>
    /// A value which can be data-bound by the skin because it has a corresponding <see cref="ValueProperty"/> property.
    /// </summary>
    public string Value
    {
      get { return (string) _valueProperty.GetValue(); }
      set { _valueProperty.SetValue(value); }
    }

    /// <summary>
    /// Counterpart for the <see cref="Value"/> property.
    /// </summary>
    public AbstractProperty ValueProperty
    {
      get { return _valueProperty; }
    }

    /// <summary>
    /// Public method which can be called from the skin.
    /// </summary>
    public void PublicMethod()
    {
      // Do something
      Value = RES_1;
    }

    #endregion
  }
}

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

using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.Models;

namespace HelloWorld.Models
{
  /// <summary>
  /// Example for a simple model.
  /// The screenfile to this model is located at:
  /// /Skins/default/screens/hello_world.xaml
  /// </summary>
  /// <remarks>
  /// <para>
  /// Models are used for providing data from the system to the skin and for executing actions (commands)
  /// which are triggered by the Skin, for example by clicking a button.
  /// </para>
  /// <para>
  /// All public properties can be data-bound by the skin, for example the <see cref="HelloString"/> property.
  /// Note that properties, which are updated by the model and whose new value should be propagated to the
  /// skin, must be backed by an instance of <see cref="AbstractProperty"/>. That instance must be made available
  /// to the skin engine by publishing it under the same name as the actual property plus "Property", see for example
  /// <see cref="HelloStringProperty"/>. In models, always <see cref="WProperty"/> instances are used.
  /// </para>
  /// <para>
  /// You can also consider to implement the interface <see cref="IWorkflowModel"/>, which makes it
  /// possible to attend the screenflow = workflow of the user session. When that interface is implemented and this
  /// model is registered in a workflow state as backing workflow model, the model will get notifications when the
  /// GUI navigation switches to or away from its workflow state.
  /// </para>
  /// <para>
  /// To make an UI model known by the system (and thus loadable by the skin), it is necessary to register it
  /// in the <c>plugin.xml</c> file.
  /// </para>
  /// </remarks>
  public class HelloWorldModel
  {
    #region Consts

    /// <summary>
    /// This is a localized string resource. Localized string resources always look like this:
    /// <example>
    /// [Section.Name]
    /// </example>
    /// Localized resources must be present at least in the english language, as this is the default.
    /// In the english language file of this hello world plugin, you'll find the translation of this string.
    /// The language file is located at: /Language/strings_en.xml
    /// </summary>
    protected const string HELLOWORLD_RESOURCE = "[HelloWorld.HelloWorldText]";

    /// <summary>
    /// Another localized string resource.
    /// </summary>
    protected const string COMMAND_TRIGGERED_RESOURCE = "[HelloWorld.ButtonTextCommandExecuted]";

    #endregion

    #region Protected properties

    /// <summary>
    /// This property holds a string that we will modify in this tutorial.
    /// </summary>
    protected readonly AbstractProperty _helloStringProperty;

    #endregion

    #region Ctor & maintainance

    /// <summary>
    /// Constructor... this one is called by the WorkflowManager when this model is loaded due to a screen reference.
    /// </summary>
    public HelloWorldModel()
    {
      // In models, properties will always be WProperty instances. When using SProperties for screen databinding,
      // the system might run into memory leaks.
      _helloStringProperty = new WProperty(typeof(string), HELLOWORLD_RESOURCE);
    }

    #endregion

    #region Public members

    /// <summary>
    /// This sample property will be accessed by the hello_world screen. Note that the data type must be the same
    /// as given in the instantiation of our backing property <see cref="_helloStringProperty"/>.
    /// </summary>
    public string HelloString
    {
      get { return (string) _helloStringProperty.GetValue(); }
      set { _helloStringProperty.SetValue(value); }
    }

    /// <summary>
    /// This is the dependency property for our sample string. It is needed to propagate changes to the skin.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the screen databinds to the <see cref="HelloString"/> property in a binding mode which will propagate data
    /// changes from the model to the skin (OneWay, TwoWay), the SkinEngine will attach a change handler to this property
    /// and react to changes.
    /// </para>
    /// <para>
    /// In other words: For each property <c>Xyz</c>, which should be able to be attached to, there must be an
    /// <see cref="AbstractProperty"/> with name <c>XyzProperty</c>.
    /// Only if <c>XyzProperty</c> is present in the model, value changes can be propagated to the skin.
    /// </para>
    /// </remarks>
    public AbstractProperty HelloStringProperty
    {
      get { return _helloStringProperty; }
    }

    /// <summary>
    /// Method which will be called from our screen. We will change the value of our HelloWorld string here.
    /// </summary>
    public void ChangeHelloWorldString()
    {
      // Localized resources in the form [Section.Name] can be used in each Label in screens. Labels automatically
      // request the localized string from the system if a text of that form is written into their Content property.
      HelloString = COMMAND_TRIGGERED_RESOURCE;
    }

    #endregion
  }
}

#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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

using MediaPortal.Presentation.DataObjects;

namespace Models.HelloWorld
{
  /// <summary>
  /// Example for a simple model. Models are used for providing data from the system to the skin.
  /// There are also models which implement special interfaces, for example
  /// <see cref="MediaPortal.Presentation.Models.IWorkflowModel"/>. Those special models can be plugged into
  /// several system components, for example workflow models can contribute at the screenflow = workflow of
  /// the user session.
  /// The screenfile to this model is located at:
  /// /Skins/default/screens/helloworld.xaml
  /// </summary>
  public class Model
  {
    /// <summary>
    /// This is a localized string resource. Localized string resources always look like this:<br/>
    /// <para>
    /// [Section.Name]
    /// </para>
    /// Localized resources must be present at least in the english language, as this is the default.
    /// Look into the language file: /Language/strings_en.xml
    /// </summary>
    protected const string HELLOWORLD_RESOURCE = "[HelloWorld.HelloWorldText]";

    /// <summary>
    /// Another localized string resource.
    /// </summary>
    protected const string COMMAND_TRIGGERED_RESOURCE = "[HelloWorld.HelloWorldCommandExecuted]";

    /// <summary>
    /// This property holds a string that we will modify in this tutorial.
    /// </summary>
    private Property _helloStringProperty;

    /// <summary>
    /// Constructor... this one is called by the WorkflowManager when accessed from a screen.
    /// </summary>
    public Model()
    {
      _helloStringProperty = new Property(typeof(string), HELLOWORLD_RESOURCE);
    }

    /// <summary>
    /// Some property that can be accessed. This property
    /// </summary>
    public string HelloString
    {
      get { return (string) _helloStringProperty.GetValue(); }
      set { _helloStringProperty.SetValue(value); }
    }

    /// <summary>
    /// Dependency Property for our string... This is needed
    /// if our string shouldn't be static, so the SkinEngine can attach to the change handler
    /// of the property. So it knows when it should update the display when the string has changed.
    ///
    /// Every property Xyz, which should be able to be attached to, must be present also as XyzProperty.
    /// Only if XyzProperty is present in the model, the binding can attach to that property.
    /// </summary>
    public Property HelloStringProperty
    {
      get { return _helloStringProperty; }
    }

    /// <summary>
    /// Method which will be called from our screen. This will change the HelloWorld Property string.
    /// </summary>
    public void ChangeHelloWorldString()
    {
      HelloString = COMMAND_TRIGGERED_RESOURCE;
    }
  }
}

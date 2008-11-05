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

using System;
using MediaPortal.Presentation.DataObjects;

namespace Models.HelloWorld
{
  /// <summary>
  /// Example for a Viewmodel
  /// the screenfile to this model is located at:
  /// /Skins/default/helloworld.xaml
  /// </summary>
  public class Model
  {
    /// <summary>
    ///  this property holds a string that we will modify 
    ///  later on in this tutorial
    /// </summary>
    private Property _helloStringProperty;

    
    /// <summary>
    /// Constructor... this one is called by the ModelManager when access from the screenfiles
    /// to the model is needed (via reflection)
    /// </summary>
    public Model()
    {
      _helloStringProperty = new Property(typeof(string), "Hello World!");
    }

    /// <summary>
    /// some property that can be accessed
    /// </summary>
    public String HelloString
    {
      get
      {
        return (String)HelloStringProperty.GetValue();
      }
      set
      {
        HelloStringProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Dependency Property for our string... this is needed
    /// if our string shouldn't be static, so the skinning engine knows
    /// that it should update the display when the string has changed
    /// 
    /// NOTE: when databinding from the XAML to this a property it will always first look for "XYZProperty"
    /// if that one is not found, it will bind to "XYZ"... so in our case if you databind to "HelloString" it will
    /// first try to find a "HelloStringProperty" property to bind to, if that doesn't succeed it will bind to the
    /// "HelloString" String property...
    /// </summary>
    public Property HelloStringProperty
    {
      get { return _helloStringProperty; }
    }


    /// <summary>
    /// this will change the HelloWorld Property string
    /// </summary>
    public void ChangeHelloWorldString()
    {
      HelloString = "Congrats, you just triggered a Command!";
    }
  }
}

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
using SkinEngine;
using SkinEngine.Controls;
using MediaPortal.Core;
using MediaPortal.Core.Collections;
using MediaPortal.Core.Properties;
using SkinEngine.Properties;

public class Scriptlet : IScriptProperty
{
  public Property Get(IControl control, string param)
  {
    Control c = control as Control;
    if (c == null) return null;
    ListContainer list = c as ListContainer;
    if (list != null)
      return new BooleanProperty(list.HasFocusProperty, list.ScrollingDownProperty, list.ScrollingUpProperty, list.ScrollingLeftProperty, list.ScrollingRightProperty);
    return Get(c.Container, param);
  }

}

public class BooleanProperty : Dependency
{
  Property _property1;
  Property _property2;
  Property _property3;
  Property _property4;
  Property _property5;
  bool _value1, _value2, _value3, _value4, _value5;
  public BooleanProperty(Property p1, Property p2, Property p3, Property p4, Property p5)
  {
    p1.Attach(new PropertyChangedHandler(onProperty1Changed));
    p2.Attach(new PropertyChangedHandler(onProperty2Changed));
    p3.Attach(new PropertyChangedHandler(onProperty3Changed));
    p4.Attach(new PropertyChangedHandler(onProperty4Changed));
    p5.Attach(new PropertyChangedHandler(onProperty5Changed));
  }

  public override object GetValue()
  {
    return (_value1 == true && _value2 == true && _value3 == true && _value4 == true & _value5 == true);
  }
  void onProperty1Changed(Property property)
  {
    _value1 = (bool)property.GetValue();
  }
  void onProperty2Changed(Property property)
  {
    _value2 = (bool)property.GetValue();
  }
  void onProperty3Changed(Property property)
  {
    _value3 = (bool)property.GetValue();
  }
  void onProperty4Changed(Property property)
  {
    _value4 = (bool)property.GetValue();
  }
  void onProperty5Changed(Property property)
  {
    _value5 = (bool)property.GetValue();
  }
};

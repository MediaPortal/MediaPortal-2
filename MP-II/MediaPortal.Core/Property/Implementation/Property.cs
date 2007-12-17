#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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

namespace MediaPortal.Core.Properties
{
  public delegate void PropertyChangedHandler(Property property);

  public class Property
  {
    private event PropertyChangedHandler PropertyChanged;
    protected Object _object;
    protected string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="Property"/> class.
    /// </summary>
    public Property() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Property"/> class.
    /// </summary>
    /// <param name="val">The property value</param>
    public Property(object val)
    {
      _object = val;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Property"/> class.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="val">The property value</param>
    public Property(string name, object val)
    {
      _name = name;
      _object = val;
    }

    /// <summary>
    /// Gets the value of the property
    /// </summary>
    /// <returns></returns>
    public virtual object GetValue()
    {
      return _object;
    }

    /// <summary>
    /// Sets the value of the property
    /// </summary>
    /// <param name="value">The value.</param>
    public void SetValue(object value)
    {
      bool changed = true;
      if (_object != null)
        changed = !(_object.Equals(value));
      else if (value == null)
        changed = false;
      _object = value;
      if (changed && PropertyChanged != null)
      {
        PropertyChanged(this);
      }
    }

    /// <summary>
    /// Attaches the an event handler.
    /// The event handler gets called when the property's value gets changed
    /// </summary>
    /// <param name="handler">The handler.</param>
    public void Attach(PropertyChangedHandler handler)
    {
      PropertyChanged += handler;
    }

    /// <summary>
    /// Detaches the specified event handler.
    /// </summary>
    /// <param name="handler">The handler.</param>
    public void Detach(PropertyChangedHandler handler)
    {
      PropertyChanged -= handler;
    }

    /// <summary>
    /// Gets or sets the property name (usefull only for debugging)
    /// </summary>
    /// <value>The name.</value>
    public string Name
    {
      get { return _name; }
      set { _name = value; }
    }
  }
}
#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

namespace MediaPortal.Core.General
{
  public delegate void PropertyChangedHandler(AbstractProperty property, object oldValue);

  /// <summary>
  /// Represents a typed property which can have a value. Changes on the value of this property
  /// can be tracked by adding a <see cref="PropertyChangedHandler"/> to it. Sub classes will introduce the concrete
  /// event handler implementation.
  /// </summary>
  public abstract class AbstractProperty
  {
    #region Protected fields and events

    protected object _syncObj = new object();
    protected object _value;
    protected Type _type;

    #endregion

    #region Ctor & maintainance

    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractProperty"/> class without a value.
    /// </summary>
    /// <param name="type">The type of the property.</param>
    protected AbstractProperty(Type type) : this(type, null) {}

    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractProperty"/> class with an initial value.
    /// </summary>
    /// <param name="value">The property value.</param>
    /// <param name="type">The type of the property.</param>
    protected AbstractProperty(Type type, object value)
    {
      _type = type;
      _value = null;
      SetValue(value);
    }

    #endregion

    #region Public properties

    public Type PropertyType
    {
      get { return _type; }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Returns the information if this property has a value, i.e. if <see cref="GetValue()"/> will return another value
    /// than <c>null</c>.
    /// </summary>
    /// <returns><c>true</c>, if this property has a not-<c>null</c>value, else <c>false</c>.</returns>
    public bool HasValue()
    {
      lock (_syncObj)
        return _value != null;
    }

    /// <summary>
    /// Gets the value of the property.
    /// </summary>
    public object GetValue()
    {
      lock (_syncObj)
        return _value;
    }

    /// <summary>
    /// Sets the value of the property.
    /// </summary>
    public void SetValue(object value)
    {
      if (value == null && _type.IsPrimitive)
        value = Activator.CreateInstance(_type); // Assign default value
      if (value != null && _type != null && !_type.IsAssignableFrom(value.GetType()))
        throw new InvalidCastException(
          String.Format("Value '{0}' cannot be assigned to property of type '{1}'", value, _type.Name));
      bool changed;
      object oldValue;
      lock (_syncObj)
      {
        if (_value == null)
          changed = value != null;
        else
          changed = !(_value.Equals(value));
        oldValue = _value;
        _value = value;
      }
      if (changed)
        Fire(oldValue);
    }

    public abstract void Fire(object oldValue);

    /// <summary>
    /// Attaches an event handler.
    /// The event handler gets called when the property's value gets changed.
    /// </summary>
    /// <remarks>
    /// The given <paramref name="handler"/> will be referenced weakly by this class, i.e. attaching an event handler
    /// will not prevent the target object of the handler from being garbage collected.
    /// </remarks>
    /// <param name="handler">The handler.</param>
    public abstract void Attach(PropertyChangedHandler handler);

    /// <summary>
    /// Detaches the specified event handler.
    /// </summary>
    /// <param name="handler">The handler.</param>
    public abstract void Detach(PropertyChangedHandler handler);

    #endregion
  }
}

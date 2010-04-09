#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;

namespace MediaPortal.Core.General
{
  /// <summary>
  /// Represents a property which binds its change event handlers with strong references.
  /// </summary>
  public class SProperty : AbstractProperty
  {
    #region Protected fields and events

    protected PropertyChangedHandler _eventDelegate;

    #endregion

    #region Ctor & maintainance

    /// <summary>
    /// Initializes a new instance of the <see cref="SProperty"/> class without a value.
    /// </summary>
    /// <param name="type">The type of the property.</param>
    public SProperty(Type type) : base(type) {}

    /// <summary>
    /// Initializes a new instance of the <see cref="SProperty"/> class with an initial value.
    /// </summary>
    /// <param name="value">The property value.</param>
    /// <param name="type">The type of the property.</param>
    public SProperty(Type type, object value) : base(type, value) {}

    #endregion

    #region Base overrides

    public override void Fire(object oldValue)
    {
      PropertyChangedHandler dlgt = _eventDelegate;
      if (dlgt != null)
        dlgt(this, oldValue);
    }

    /// <summary>
    /// Attaches an event handler. The event handler will be bound with a strong reference.
    /// </summary>
    /// <param name="handler">The handler.</param>
    public override void Attach(PropertyChangedHandler handler)
    {
      _eventDelegate += handler;
    }

    /// <summary>
    /// Detaches the specified event handler.
    /// </summary>
    /// <param name="handler">The handler.</param>
    public override void Detach(PropertyChangedHandler handler)
    {
      _eventDelegate -= handler;
    }

    #endregion
  }
}

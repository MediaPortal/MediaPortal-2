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

namespace MediaPortal.Common.General
{
  /// <summary>
  /// Represents a property which binds its change event handlers with weak references.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Hint: <see cref="WProperty"/> instances should be used in UI models, rather than <see cref="SProperty"/> instances.
  /// <see cref="WProperty"/> instances bind their change event handlers with weak references which allows a proper garbage collection
  /// of screens and the theme if they bind to <see cref="WProperty"/> instances in UI models.
  /// </para>
  /// <para>
  /// <see cref="WProperty"/> instances are much less performant than <see cref="SProperty"/> instances.
  /// </para>
  /// </remarks>
  public class WProperty : AbstractProperty
  {
    #region Protected fields and events

    protected WeakEventMulticastDelegate _eventDelegate = new WeakEventMulticastDelegate();

    #endregion

    #region Ctor & maintainance

    /// <summary>
    /// Initializes a new instance of the <see cref="WProperty"/> class without a value.
    /// </summary>
    /// <param name="type">The type of the property.</param>
    public WProperty(Type type) : base(type) {}

    /// <summary>
    /// Initializes a new instance of the <see cref="WProperty"/> class with an initial value.
    /// </summary>
    /// <param name="value">The property value.</param>
    /// <param name="type">The type of the property.</param>
    public WProperty(Type type, object value) : base(type, value) {}

    #endregion

    #region Base overrides

    public override void Fire(object oldValue)
    {
      _eventDelegate.Fire(new object[] {this, oldValue});
    }

    /// <summary>
    /// Attaches an event handler. The event handler will be bound with a weak reference.
    /// </summary>
    /// <param name="handler">The handler.</param>
    public override void Attach(PropertyChangedHandler handler)
    {
      _eventDelegate.Attach(handler);
    }

    /// <summary>
    /// Detaches the specified event handler.
    /// </summary>
    /// <param name="handler">The handler.</param>
    public override void Detach(PropertyChangedHandler handler)
    {
      _eventDelegate.Detach(handler);
    }

    #endregion
  }
}

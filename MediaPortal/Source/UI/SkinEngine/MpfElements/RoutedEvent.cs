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
using System.Globalization;

namespace MediaPortal.UI.SkinEngine.MpfElements
{
  /// <summary>
  /// Represents and identifies a routed event.
  /// </summary>
  public sealed class RoutedEvent
  {
    internal RoutedEvent(string name, RoutingStrategy routingStrategy, Type handlerType, Type ownerType)
    {
      Name = name;
      RoutingStrategy = routingStrategy;
      HandlerType = handlerType;
      OwnerType = ownerType;
    }

    #region Public properties

    /// <summary>
    /// Gets the handler type of the routed event.
    /// </summary>
    public Type HandlerType { get; private set; }

    /// <summary>
    /// Gets the identifying name of the routed event.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the registered owner type of the routed event.
    /// </summary>
    public Type OwnerType { get; private set; }

    /// <summary>
    /// Gets the routing strategy of the routed event.
    /// </summary>
    public RoutingStrategy RoutingStrategy { get; private set; }

    #endregion

    #region public methods

    /// <summary>
    /// Associates another owner with the routed event represented by a <see cref="RoutedEvent"/> instance.
    /// </summary>
    /// <param name="ownerType">The type where the routed event is added. Must not be null.</param>
    /// <returns></returns>
    public RoutedEvent AddOwner(Type ownerType)
    {
      if (ownerType == null) throw new ArgumentNullException("ownerType");
      GlobalEventManager.AddOwner(this, ownerType);
      return this;
    }

    #endregion

    #region base overrides

    public override string ToString()
    {
      return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", OwnerType.Name, Name);
    }

    #endregion
  }

  /// <summary>
  /// Represents the method that will handle routed events.
  /// </summary>
  /// <param name="sender">Sender of the event</param>
  /// <param name="e">Event arguments for this event.</param>
  public delegate void RoutedEventHandler(object sender, RoutedEventArgs e);

  /// <summary>
  /// Routing strategies for <see cref="RoutedEvent"/>s
  /// </summary>
  public enum RoutingStrategy
  {
    Bubble,
    Direct,
    Tunnel
  }
}

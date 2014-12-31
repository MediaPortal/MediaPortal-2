#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using MediaPortal.Common.Services.Logging;

namespace MediaPortal.UI.SkinEngine.MpfElements
{
  /// <summary>
  /// Provides event-handling utility methods.
  /// </summary>
  public static class EventManager
  {
    #region public static methods

    /// <summary>
    /// Returns identifiers for registered <see cref="RoutedEvent"/>s.
    /// </summary>
    /// <returns>An array of registered routed events.</returns>
    public static RoutedEvent[] GetRoutedEvents()
    {
      return GlobalEventManager.GetRoutedEvents();
    }

    /// <summary>
    /// Finds all <see cref="RoutedEvent"/> identifiers owned by a specified object type.
    /// </summary>
    /// <param name="ownerType">The type to start search with. Base types are included. Must not be null.</param>
    /// <returns></returns>
    public static RoutedEvent[] GetRoutedEventsForOwner(Type ownerType)
    {
      if(ownerType == null) throw new ArgumentNullException("ownerType");

      return GlobalEventManager.GetRoutedEventsForOwner(ownerType);
    }

    /// <summary>
    /// REgisteres a class handler for a particular <see cref="RoutedEvent"/>.
    /// </summary>
    /// <param name="classType">Type of the class that is declaring class handling. Must not be null.</param>
    /// <param name="routedEvent">The <see cref="RoutedEvent"/> identifier of the event to handle. Must not be null.</param>
    /// <param name="handler">A reference to the class handler. Must not be null.</param>
    public static void RegisterClassHandler(Type classType, RoutedEvent routedEvent, Delegate handler)
    {
      RegisterClassHandler(classType, routedEvent, handler, false);
    }

    /// <summary>
    /// REgisteres a class handler for a particular <see cref="RoutedEvent"/> with the option to handle events that have been marked as handled already.
    /// </summary>
    /// <param name="classType">Type of the class that is declaring class handling. Must not be null.</param>
    /// <param name="routedEvent">The <see cref="RoutedEvent"/> identifier of the event to handle. Must not be null.</param>
    /// <param name="handler">A reference to the class handler. Must not be null.</param>
    /// <param name="handledEventsToo"><c>true</c> to invoke the handler, even if the <see>RoutedEventArgs.Handled</see> property has been set to <c>true</c> already;
    /// <c>false</c> to retain the default behavior of not invoking handled events</param>.
    public static void RegisterClassHandler(Type classType, RoutedEvent routedEvent, Delegate handler, bool handledEventsToo)
    {
      if (classType == null) throw new ArgumentNullException("classType");
      if (routedEvent == null) throw new ArgumentNullException("routedEvent");
      if (handler == null) throw new ArgumentNullException("handler");

      GlobalEventManager.RegisterClassHandler(classType, routedEvent, handler, handledEventsToo);
    }

    /// <summary>
    /// Registers a new event with the event system.
    /// </summary>
    /// <param name="name">Name of the event. The name must be unique within the owner class. It must not be <c>null</c> or empty.</param>
    /// <param name="routingStrategy">The routing strategy for this event.</param>
    /// <param name="handlerType">The type of the event handler. Can not be <c>null</c></param>
    /// <param name="ownerType">The owner class type of the event. Can not be <c>null</c>.</param>
    /// <returns></returns>
    public static RoutedEvent RegisterRoutedEvent(string name, RoutingStrategy routingStrategy, Type handlerType, Type ownerType)
    {
      if (name == null) throw new ArgumentNullException("name");
      if (handlerType == null) throw new ArgumentNullException("handlerType");
      if (ownerType == null) throw new ArgumentNullException("ownerType");
      if (String.IsNullOrEmpty(name)) throw new ArgumentException(@"name must not be an empty string", "name");

      return GlobalEventManager.RegisterRoutedEvent(name, routingStrategy, handlerType, ownerType);
    }

    #endregion
  }
}

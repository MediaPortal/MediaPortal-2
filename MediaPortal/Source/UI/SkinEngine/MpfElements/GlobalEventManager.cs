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
using System.Collections.Generic;
using MediaPortal.UI.SkinEngine.Commands;

namespace MediaPortal.UI.SkinEngine.MpfElements
{
  internal static class GlobalEventManager
  {
    internal static object Synchronized = new object();

    private static readonly Dictionary<Type, List<RoutedEvent>> _routedEventOwnerDictionary = new Dictionary<Type, List<RoutedEvent>>();

    public static RoutedEvent RegisterRoutedEvent(string name, RoutingStrategy routingStrategy, Type handlerType, Type ownerType)
    {
      lock (Synchronized)
      {
        var routedEvent = new RoutedEvent(name, routingStrategy, handlerType, ownerType);
        AddOwner(routedEvent, ownerType);
        return routedEvent;
      }
    }

    public static void AddOwner(RoutedEvent routedEvent, Type ownerType)
    {
      lock (Synchronized)
      {
        List<RoutedEvent> eventList;
        if (!_routedEventOwnerDictionary.TryGetValue(ownerType, out eventList))
        {
          eventList = new List<RoutedEvent>();
          _routedEventOwnerDictionary.Add(ownerType, eventList);
        }
        if (!eventList.Contains(routedEvent))
        {
          eventList.Add(routedEvent);
        }
      }
    }

    public static RoutedEvent[] GetRoutedEvents()
    {
      lock (Synchronized)
      {
        var eventList = new List<RoutedEvent>();
        foreach (var list in _routedEventOwnerDictionary.Values)
        {
          foreach (var routedEvent in list)
          {
            eventList.Add(routedEvent);
          }
        }
        return eventList.ToArray();
      }
    }

    public static RoutedEvent[] GetRoutedEventsForOwner(Type ownerType)
    {
      lock (Synchronized)
      {
        List<RoutedEvent> eventList;
        if (_routedEventOwnerDictionary.TryGetValue(ownerType, out eventList))
        {
          return eventList.ToArray();
        }
        return new RoutedEvent[0];
      }
    }

    private static readonly Dictionary<Type, Dictionary<RoutedEvent, List<RoutedEventHandlerInfo>>> _typedClassListeners = new Dictionary<Type, Dictionary<RoutedEvent, List<RoutedEventHandlerInfo>>>();

    public static void RegisterClassHandler(Type classType, RoutedEvent routedEvent, Delegate handler, bool handledEventsToo)
    {
      lock (Synchronized)
      {
        Dictionary<RoutedEvent, List<RoutedEventHandlerInfo>> classHandlers;
        if (!_typedClassListeners.TryGetValue(classType, out classHandlers))
        {
          classHandlers = new Dictionary<RoutedEvent, List<RoutedEventHandlerInfo>>();
          _typedClassListeners.Add(classType, classHandlers);
        }
        List<RoutedEventHandlerInfo> handlerInfoList;
        if (!classHandlers.TryGetValue(routedEvent, out handlerInfoList))
        {
          handlerInfoList = new List<RoutedEventHandlerInfo>();
          classHandlers.Add(routedEvent, handlerInfoList);
        }
        var handlerInfo = new RoutedEventHandlerInfo(handler, handledEventsToo);
        handlerInfoList.Add(handlerInfo);
      }
    }

    public static IEnumerable<RoutedEventHandlerInfo> GetTypedClassEventHandlers(Type classType, RoutedEvent routedEvent)
    {
      foreach (var classListener in _typedClassListeners)
      {
        if (classListener.Key == classType || classType.IsSubclassOf(classListener.Key))
        {
          List<RoutedEventHandlerInfo> handlerInfoList;
          if (classListener.Value.TryGetValue(routedEvent, out handlerInfoList))
          {
            foreach (var handler in handlerInfoList)
            {
              yield return handler;
            }
          }
        }
      }
    }
  }

  internal struct RoutedEventHandlerInfo
  {
    internal RoutedEventHandlerInfo(Delegate handler, bool handledEventsToo)
      : this()
    {
      Handler = handler;
      HandledEventsToo = handledEventsToo;
    }

    internal RoutedEventHandlerInfo(ICommandStencil handler, bool handledEventsToo)
      : this()
    {
      CommandStencilHandler = handler;
      HandledEventsToo = handledEventsToo;
    }

    internal Delegate Handler { get; private set; }

    internal ICommandStencil CommandStencilHandler { get; private set; }

    internal bool HandledEventsToo { get; private set; }

    internal void InvokeHandler(object target, RoutedEventArgs routedEventArgs)
    {
      if (routedEventArgs.Handled && !HandledEventsToo)
        return;

      if (CommandStencilHandler != null)
      {
        CommandStencilHandler.Execute(new Object[] { target, routedEventArgs });
      }
      else
      {
        var handler = Handler as RoutedEventHandler;
        if (handler != null)
        {
          handler(target, routedEventArgs);
        }
        else
        {
          Handler.DynamicInvoke(target, routedEventArgs);
        }
      }
    }
  }
}

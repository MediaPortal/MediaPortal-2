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
using MediaPortal.UI.SkinEngine.Commands;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Styles
{
  /// <summary>
  /// EventSetter allows to attach event handlers in a style.
  /// </summary>
  /// <remarks>
  /// Until now, only <see cref="_routedEvent"/>s are supported.
  /// </remarks>
  public class EventSetter : SetterBase
  {
    #region protected fields

    protected RoutedEvent _routedEvent;

    #endregion

    #region public properties

    /// <summary>
    /// Gets or sets the event name.
    /// </summary>
    /// <remarks>Event is an alias for the property <see cref="SetterBase.Property"/>.</remarks>
    public string Event
    {
      get { return Property; }
      set { Property = value; }
    }

    /// <summary>
    /// Gets or sets if the <see cref="Handler"/> should be called, even that the event is already handled.
    /// </summary>
    public bool HandledEventsToo { get; set; }

    /// <summary>
    /// Gets or sets the handler <see cref="ICommandStencil"/> which is invoked when the event is raised.
    /// </summary>
    public ICommandStencil Handler { get; set; }

    #endregion

    #region base overrides

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      var setterSource = (EventSetter) source;
      // Event property is only a alias for Property which is copied by the base class
      HandledEventsToo = setterSource.HandledEventsToo;
      var disposableHandler = Handler as IDisposable;
      if (disposableHandler != null)
      {
        disposableHandler.Dispose();
      }
      Handler = null;
      if (setterSource.Handler != null)
      {
        Handler = copyManager.GetCopy(setterSource.Handler);
      }
    }

    public override void Dispose()
    {
      base.Dispose();
      var disposableHandler = Handler as IDisposable;
      if (disposableHandler != null)
      {
        disposableHandler.Dispose();
      }
      Handler = null;
    }

    public override void FinishInitialization(IParserContext context)
    {
      // if we have a type specified in the event name, we get the RoutedEvent here
      string localName;
      string namespaceUri;
      context.LookupNamespace(Event, out localName, out namespaceUri);
      var namespaceHandler = context.GetNamespaceHandler(namespaceUri);
      if (namespaceHandler != null)
      {
        int n = localName.IndexOf('.');
        if (n >= 0)
        {
          var sourceType = namespaceHandler.GetElementType(localName.Substring(0, n), true);
          var eventName = localName.Substring(n + 1);

          _routedEvent = EventManager.GetRoutedEventForOwner(sourceType, eventName, true);
        }
        // for events without explicit set type, the type and RoutedEvent is looked up in Set/Reset on the target object type.
      }
      base.FinishInitialization(context);
    }

    public override void Set(UIElement element)
    {
      if (Handler != null)
      {
        UIElement targetObject;
        var routedEvent = GetRoutedEvent(element, out targetObject);
        if (routedEvent != null && targetObject != null)
        {
          targetObject.AddHandler(routedEvent, Handler, HandledEventsToo);
        }
      }
    }

    public override void Restore(UIElement element)
    {
      if (Handler != null)
      {
        UIElement targetObject;
        var routedEvent = GetRoutedEvent(element, out targetObject);
        if (routedEvent != null && targetObject != null)
        {
          targetObject.RemoveHandler(routedEvent, Handler);
        }
      }
    }

    #endregion

    #region private methods

    private RoutedEvent GetRoutedEvent(UIElement element, out UIElement targetObject)
    {
      if (string.IsNullOrEmpty(TargetName))
      {
        targetObject = element;
      }
      else
      {
        // Search the element in "normal" name scope and in the dynamic structure via the FindElement method
        // I think this is more than WPF does. It makes it possible to find elements instantiated
        // by a template, for example.
        targetObject = (element.FindElementInNamescope(TargetName) ??
            element.FindElement(new NameMatcher(TargetName))) as UIElement;
        if (targetObject == null)
          return null;
      }

      var routedEvent = _routedEvent;
      if (routedEvent == null)
      {
        // the routed event is not set already, so we try to get it from the targetObject
        return EventManager.GetRoutedEventForOwner(targetObject.GetType(), Event, true);
      }
      return routedEvent;
    }

    #endregion
  }
}

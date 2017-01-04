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

namespace MediaPortal.UI.SkinEngine.MpfElements
{
  /// <summary>
  /// Contains state information and data associated with an <see cref="RoutedEvent"/>.
  /// </summary>
  public class RoutedEventArgs : EventArgs
  {
    private object _source;

    /// <summary>
    /// Initializes a new instance of the RoutedEventArgs class.
    /// </summary>
    public RoutedEventArgs() 
      : this(null, null)
    { }

    /// <summary>
    /// Initializes a new instance of the RoutedEventArgs class, using the supplied routed event identifier.
    /// </summary>
    /// <param name="routedEvent">The routed event identifier for this <see cref="RoutedEventArgs"/> class.</param>
    public RoutedEventArgs(RoutedEvent routedEvent) 
      : this(routedEvent, null)
    { }

    /// <summary>
    /// Initializes a new instance of the RoutedEventArgs class, using the supplied routed event identifier with a different source for the event.
    /// </summary>
    /// <param name="routedEvent">The routed event identifier for this <see cref="RoutedEventArgs"/> class.</param>
    /// <param name="source">An alternate source that is used when the event is handled.</param>
    public RoutedEventArgs(RoutedEvent routedEvent, object source)
    {
      RoutedEvent = routedEvent;
      Handled = false;
      Source = source;
    }

    #region public properties

    /// <summary>
    /// Gets or sets the routed event associated with this <see cref="RoutedEventArgs"/> instance.
    /// </summary>
    public RoutedEvent RoutedEvent { get; set; }

    /// <summary>
    /// Gets or sets a values that influences the invocation of registered event handlers.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Gets or sets the object that raised the event.
    /// </summary>
    public Object Source
    {
      get { return _source; }
      set
      {
        object source = value;
        if (_source == null && OriginalSource == null)
        {
          _source = source;
          OriginalSource = source;
          OnSetSource(source);
        }
        else
        {
          if (_source == source)
            return;
          _source = source;
          OnSetSource(source);
        }
      }
    }

    /// <summary>
    /// Gets the original source of the event, as determined by hit testing.
    /// </summary>
    public Object OriginalSource { get; private set; }

    #endregion

    #region protected methods

    /// <summary>
    /// When overridden in a derived class, provides a way to invoke event handlers in a type-specific way, which can increase efficiency over the base implementation.
    /// </summary>
    /// <param name="genericHandler">The generic handler to be invoked. Must not be <c>null</c>.</param>
    /// <param name="genericTarget">The target on which the handler should be invoked. Must not be <c>null</c>.</param>
    protected virtual void InvokeEventHandler(Delegate genericHandler, object genericTarget)
    {
      if (genericHandler == null) throw new ArgumentNullException("genericHandler");
      if (genericTarget == null) throw new ArgumentNullException("genericTarget");

      var handler = genericHandler as RoutedEventHandler;
      if (handler != null)
      {
        handler(genericTarget, this);
      }
      else
      {
        genericHandler.DynamicInvoke(genericTarget, this);
      }
    }

    /// <summary>
    /// When overridden, provides an notification when ever the <see cref="Source"/> property is changed.
    /// </summary>
    /// <param name="source">The new value <see cref="Source"/> is set to</param>
    /// <remarks>
    /// The default implementation is empty.
    /// </remarks>
    protected virtual void OnSetSource(object source)
    { }

    #endregion
  }
}

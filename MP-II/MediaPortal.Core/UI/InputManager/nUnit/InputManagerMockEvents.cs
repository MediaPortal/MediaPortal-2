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

#if DEBUG

using System;
using System.IO;
using NMock2;
using NMock2.Monitoring;

namespace MediaPortal.Control.InputManager
{
  public class MockKeyPressedEvent
  {
    private KeyPressedHandler handler;

    public void Initialize(KeyPressedHandler handler)
    {
      this.handler = handler;
    }

    public void Raise()
    {
      Key key = Key.None;
      handler.Invoke(ref key);
    }

    public static IAction Hookup(MockKeyPressedEvent mockEvent)
    {
      return new MockKeyPressedEventHookup(mockEvent);
    }
  }

  public class MockKeyPressedEventHookup : IAction
  {
    private readonly MockKeyPressedEvent mockEvent;

    public MockKeyPressedEventHookup(MockKeyPressedEvent mockEvent)
    {
      this.mockEvent = mockEvent;
    }

    public void Invoke(Invocation invocation)
    {
      KeyPressedHandler handler = invocation.Parameters[0] as KeyPressedHandler;
      if (handler == null)
      {
        throw new Exception("Unknown event handler type.");
      }
      mockEvent.Initialize(handler);
    }

    public void DescribeTo(TextWriter writer)
    {
      // do nothing
    }
  }


  public class MockMouseMoveEvent
  {
    private MouseMoveHandler handler;

    public void Initialize(MouseMoveHandler handler)
    {
      this.handler = handler;
    }

    public void Raise()
    {
      handler.Invoke(10f, 10f);
    }

    public static IAction Hookup(MockMouseMoveEvent mockEvent)
    {
      return new MockMouseMoveEventHookup(mockEvent);
    }
  }

  public class MockMouseMoveEventHookup : IAction
  {
    private readonly MockMouseMoveEvent mockEvent;

    public MockMouseMoveEventHookup(MockMouseMoveEvent mockEvent)
    {
      this.mockEvent = mockEvent;
    }

    public void Invoke(Invocation invocation)
    {
      MouseMoveHandler handler = invocation.Parameters[0] as MouseMoveHandler;
      if (handler == null)
      {
        throw new Exception("Unknown event handler type.");
      }
      mockEvent.Initialize(handler);
    }

    public void DescribeTo(TextWriter writer)
    {
      // do nothing
    }
  }
}

#endif

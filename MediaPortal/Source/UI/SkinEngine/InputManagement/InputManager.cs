#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Core;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.Actions;
using MediaPortal.UI.Presentation.Screens;

namespace MediaPortal.UI.SkinEngine.InputManagement
{
  /// <summary>
  /// Delegate for a mouse handler.
  /// </summary>
  /// <param name="x">X coordinate of the new mouse position.</param>
  /// <param name="y">Y coordinate of the new mouse position.</param>
  public delegate void MouseMoveHandler(float x, float y);

  /// <summary>
  /// Delegate for a mouse click handler.
  /// </summary>
  /// <param name="buttons">Buttons that have been clicked.</param>
  public delegate void MouseClickHandler(MouseButtons buttons);

  /// <summary>
  /// Delegate for a mouse wheel handler.
  /// </summary>
  /// <param name="numDetents">Number of detents which have been scrolled.</param>
  public delegate void MouseWheelHandler(int numDetents);

  /// <summary>
  /// Delegate for a key handler.
  /// </summary>
  /// <param name="key">The key which was pressed. This parmeter should be set to <see cref="Key.None"/> when the
  /// key was consumed.</param>
  public delegate void KeyPressedHandler(ref Key key);

  /// <summary>
  /// Input manager class which provides the public <see cref="IInputManager"/> interface and some other
  /// skin engine internal functionality.
  /// </summary>
  /// <remarks>
  /// Here, we provide the functionality to decouple the main applications event handlers from the internal
  /// event handling functions to provide a busy cursor.
  /// </remarks>
  public class InputManager : IInputManager
  {
    #region Inner classes

    protected class InputEvent
    {}

    protected class KeyEvent : InputEvent
    {
      protected Key _key;

      public KeyEvent(Key key)
      {
        _key = key;
      }

      public Key Key
      {
        get { return _key; }
      }
    }

    protected class MouseMoveEvent : InputEvent
    {
      protected float _x;
      protected float _y;

      public MouseMoveEvent(float x, float y)
      {
        _x = x;
        _y = y;
      }

      public float X
      {
        get { return _x; }
      }

      public float Y
      {
        get { return _y; }
      }
    }

    protected class MouseClickEvent : InputEvent
    {
      protected MouseButtons _buttons;

      public MouseClickEvent(MouseButtons buttons)
      {
        _buttons = buttons;
      }

      public MouseButtons Buttons
      {
        get { return _buttons; }
      }
    }

    protected class MouseWheelEvent : InputEvent
    {
      protected int _numDetents;

      public MouseWheelEvent(int numDetents)
      {
        _numDetents = numDetents;
      }

      public int NumDetents
      {
        get { return _numDetents; }
      }
    }

    #endregion

    #region Consts

    // TODO: Make this configurable
    protected static TimeSpan MOUSE_CONTROLS_TIMEOUT = TimeSpan.FromSeconds(5);

    protected static TimeSpan BUSY_TIMEOUT = TimeSpan.FromSeconds(1);

    #endregion

    #region Protected fields

    protected DateTime _lastMouseUsageTime = DateTime.MinValue;
    protected DateTime _lastInputTime = DateTime.MinValue;
    protected IDictionary<Key, KeyAction> _keyBindings = new Dictionary<Key, KeyAction>();
    protected PointF _mousePosition = new PointF();
    protected DateTime? _callingClientStart = null;
    protected bool _busyScreenVisible = false;
    protected Thread _workThread;
    protected Queue<InputEvent> _inputEventQueue = new Queue<InputEvent>(30);
    protected ManualResetEvent _terminatedEvent = new ManualResetEvent(false);
    protected AutoResetEvent _inputAvailableEvent = new AutoResetEvent(false);

    protected static InputManager _instance = null;
    protected static object _syncObj = new object();

    #endregion

    private InputManager()
    {
      _workThread = new Thread(DoWork) { IsBackground = true, Name = "InputMgr" };  //InputManager dispatch thread
      _workThread.Start();
    }

    public void Dispose()
    {
      ClearInputBuffer();
      Terminate();
    }

    public static InputManager Instance
    {
      get
      {
        lock (_syncObj)
          if (_instance == null)
            _instance = new InputManager();
        return _instance;
      }
    }

    /// <summary>
    /// Can be registered by classes of the skin engine to be informed about mouse movements.
    /// </summary>
    public event MouseMoveHandler MouseMoved;

    /// <summary>
    /// Can be registered by classes of the skin engine to be informed about mouse clicks.
    /// </summary>
    public event MouseClickHandler MouseClicked;

    /// <summary>
    /// Can be registered by classes of the skin engine to be informed about mouse wheel events.
    /// </summary>
    public event MouseWheelHandler MouseWheeled;

    /// <summary>
    /// Can be registered by classes of the skin engine to be informed about key events.
    /// </summary>
    public event KeyPressedHandler KeyPressed;

    /// <summary>
    /// Can be registered by classes of the skin engine to preview key events before shortcuts are triggered.
    /// </summary>
    public event KeyPressedHandler KeyPreview;

    public void Terminate()
    {
      _terminatedEvent.Set();
    }

    public bool IsTerminated
    {
      get { return _terminatedEvent.WaitOne(0); }
    }

    protected bool IsEventAvailable
    {
      get
      {
        lock (_syncObj)
          return _inputEventQueue.Count > 0;
      }
    }

    protected InputEvent DequeueEvent()
    {
      lock (_syncObj)
        return _inputEventQueue.Count == 0 ? null : _inputEventQueue.Dequeue();
    }

    protected void ClearInputBuffer()
    {
      lock (_syncObj)
        _inputEventQueue.Clear();
    }

    protected void TryEvent_NoLock(InputEvent evt)
    {
      bool showBusyScreen = false;
      lock (_syncObj)
      {
        if (IsTerminated)
          return;
        if (_callingClientStart.HasValue && _callingClientStart.Value < DateTime.Now - BUSY_TIMEOUT)
        { // Client call lasts longer than our BUSY_TIMEOUT
          ClearInputBuffer(); // Discard all later input
          if (!_busyScreenVisible)
            showBusyScreen = true;
        }
      }
      if (showBusyScreen)
      {
        ISuperLayerManager superLayerManager = ServiceRegistration.Get<ISuperLayerManager>();
        superLayerManager.ShowBusyScreen();
        lock (_syncObj)
          _busyScreenVisible = true;
        return; // Finished, no further processing
      }
      EnqueueEvent(evt);
    }

    protected void DispatchEvent(InputEvent evt)
    {
      lock (_syncObj)
        _callingClientStart = DateTime.Now;
      try
      {
        Type eventType = evt.GetType();
        if (eventType == typeof(KeyEvent))
          ExecuteKeyPress((KeyEvent) evt);
        else if (eventType == typeof(MouseMoveEvent))
          ExecuteMouseMove((MouseMoveEvent) evt);
        else if (eventType == typeof(MouseClickEvent))
          ExecuteMouseClick((MouseClickEvent) evt);
        else if (eventType == typeof(MouseWheelEvent))
          ExecuteMouseWheel((MouseWheelEvent) evt);
      }
      finally
      {
        bool hideBusyScreen;
        lock (_syncObj)
        {
          hideBusyScreen = _busyScreenVisible;
          _busyScreenVisible = false;
          _callingClientStart = null;
        }
        if (hideBusyScreen && !IsTerminated)
        {
          ISuperLayerManager superLayerManager = ServiceRegistration.Get<ISuperLayerManager>();
          superLayerManager.HideBusyScreen();
        }
      }
    }

    protected void EnqueueEvent(InputEvent evt)
    {
      lock (_syncObj)
      {
        _inputEventQueue.Enqueue(evt);
        _inputAvailableEvent.Set();
      }
    }

    protected void ExecuteKeyPress(KeyEvent evt)
    {
      Key key = evt.Key;
      if (KeyPreview != null)
        KeyPreview(ref key);
      if (key == Key.None)
        return;
      // Try key bindings...
      KeyAction keyAction;
      lock (_syncObj)
        if (!_keyBindings.TryGetValue(key, out keyAction))
          keyAction = null;
      if (keyAction != null)
        keyAction.Action();
      else
      {
        KeyPressedHandler dlgt = KeyPressed;
        if (dlgt != null)
          dlgt(ref key);
      }
    }

    protected void ExecuteMouseMove(MouseMoveEvent evt)
    {
      MouseMoveHandler dlgt = MouseMoved;
      if (dlgt != null)
        dlgt(evt.X, evt.Y);
    }

    protected void ExecuteMouseClick(MouseClickEvent evt)
    {
      MouseClickHandler dlgt = MouseClicked;
      if (dlgt != null)
        dlgt(evt.Buttons);
    }

    protected void ExecuteMouseWheel(MouseWheelEvent evt)
    {
      MouseWheelHandler dlgt = MouseWheeled;
      if (dlgt != null)
        dlgt(evt.NumDetents);
    }

    protected void DoWork()
    {
      while (true)
      {
        InputEvent evt;
        while ((evt = DequeueEvent()) != null)
        {
          DispatchEvent(evt);
          if (IsTerminated)
            break;
        }
        WaitHandle.WaitAny(new WaitHandle[] {_terminatedEvent, _inputAvailableEvent});
        if (IsTerminated)
          break;
      }
    }

    #region IInputManager implementation

    public DateTime LastMouseUsageTime
    {
      get { return _lastMouseUsageTime; }
      internal set { _lastMouseUsageTime = value; }
    }

    public DateTime LastInputTime
    {
      get { return _lastInputTime; }
      internal set { _lastInputTime = value; }
    }

    public bool IsMouseUsed
    {
      get { return DateTime.Now - _lastMouseUsageTime < MOUSE_CONTROLS_TIMEOUT; }
    }

    public PointF MousePosition
    {
      get { return _mousePosition; }
    }

    public void MouseMove(float x, float y)
    {
      DateTime now = DateTime.Now;
      _lastInputTime = now;
      _lastMouseUsageTime = now;
      _mousePosition = new PointF(x, y);
      TryEvent_NoLock(new MouseMoveEvent(x, y));
    }

    public void MouseClick(MouseButtons mouseButtons)
    {
      DateTime now = DateTime.Now;
      _lastInputTime = now;
      _lastMouseUsageTime = now;
      TryEvent_NoLock(new MouseClickEvent(mouseButtons));
    }

    public void MouseWheel(int numDetents)
    {
      DateTime now = DateTime.Now;
      _lastInputTime = now;
      _lastMouseUsageTime = now;
      TryEvent_NoLock(new MouseWheelEvent(numDetents));
    }

    public void KeyPress(Key key)
    {
      _lastInputTime = DateTime.Now;
      TryEvent_NoLock(new KeyEvent(key));
    }

    public void AddKeyBinding(Key key, KeyActionDlgt action)
    {
      lock (_syncObj)
        _keyBindings[key] = new KeyAction(key, action);
    }

    public void RemoveKeyBinding(Key key)
    {
      lock (_syncObj)
        _keyBindings.Remove(key);
    }

    #endregion
  }
}

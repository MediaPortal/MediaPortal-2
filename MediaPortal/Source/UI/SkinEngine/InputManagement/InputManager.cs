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
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Runtime;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.Actions;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.MpfElements.Input;
using KeyEventArgs = MediaPortal.UI.SkinEngine.MpfElements.Input.KeyEventArgs;
using MouseEventArgs = MediaPortal.UI.SkinEngine.MpfElements.Input.MouseEventArgs;

namespace MediaPortal.UI.SkinEngine.InputManagement
{

  /// <summary>
  /// Delegate for a mouse click handler.
  /// </summary>
  /// <param name="buttons">Buttons that have been clicked.</param>
  public delegate void MouseClickHandler(MouseButtons buttons);

  /// <summary>
  /// Delegate for a key handler.
  /// </summary>
  /// <param name="key">The key which was pressed. This parmeter should be set to <see cref="Key.None"/> when the
  /// key was consumed.</param>
  public delegate void KeyPressedHandler(ref Key key);

  internal delegate void RoutedInputEventHandler(RoutedEventArgs args, RoutedEvent[] events);

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

    public class InputEvent
    {
      public override string ToString()
      {
        return GetType().Name;
      }
    }

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

    // EventArgs passed to Touch handlers
    internal abstract class InputTouchEvent : InputEvent
    {
      // touch X client coordinate in pixels
      public float LocationX { get; set; }

      // touch Y client coordinate in pixels
      public float LocationY { get; set; }

      // contact ID
      public int Id { get; set; }

      // flags
      public TouchEventFlags Flags { get; set; }

      // mask which fields in the structure are valid
      public TouchInputMask Mask { get; set; }

      // touch event time
      public int Time { get; set; }

      // X size of the contact area in pixels
      public float ContactX { get; set; }

      // Y size of the contact area in pixels
      public float ContactY { get; set; }

      public bool IsPrimaryContact
      {
        get { return (Flags.HasFlag(TouchEventFlags.Primary)); }
      }

      public override string ToString()
      {
        return string.Format("Touch: F: {0} M: {1} LX: {2} LY: {3}", Flags, Mask, LocationX, LocationY);
      }
    }

    internal class InputTouchDownEvent : InputTouchEvent { }
    internal class InputTouchUpEvent : InputTouchEvent { }
    internal class InputTouchMoveEvent : InputTouchEvent { }

    protected class RoutedInputEvent : InputEvent
    {
      public RoutedInputEvent(RoutedEventArgs args, params RoutedEvent[] events)
      {
        EventArgs = args;
        RoutedEvents = events;
      }

      public RoutedEventArgs EventArgs { get; private set; }
      public RoutedEvent[] RoutedEvents { get; private set; }
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
    protected Queue<ParameterlessMethod> _commandQueue = new Queue<ParameterlessMethod>(30);
    protected ManualResetEvent _terminatedEvent = new ManualResetEvent(false);
    protected AutoResetEvent _inputAvailableEvent = new AutoResetEvent(false);

    protected static InputManager _instance = null;
    protected static object _syncObj = new object();

    #endregion

    private InputManager()
    {
      _lastMouseUsageTime = _lastInputTime = DateTime.Now;
      _workThread = new Thread(DoWork) { IsBackground = true, Name = "InputMgr" };  //InputManager dispatch thread
      _workThread.Start();
    }

    public void Dispose()
    {
      ClearInputBuffer();
      Terminate();
      _terminatedEvent.Set();
      _terminatedEvent.Close();
      _inputAvailableEvent.Close();
      _terminatedEvent = null;
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
    /// Can be registered by classes of the skin engine to be informed about key events.
    /// </summary>
    public event KeyPressedHandler KeyPressed;

    /// <summary>
    /// Can be registered by classes of the skin engine to preview key events before shortcuts are triggered.
    /// </summary>
    public event KeyPressedHandler KeyPreview;

    public event EventHandler<TouchDownEvent> TouchDown;   // touch down event handler
    public event EventHandler<TouchUpEvent> TouchUp;       // touch up event handler
    public event EventHandler<TouchMoveEvent> TouchMove;   // touch move event handler

    internal event RoutedInputEventHandler RoutedInputEventFired;

    public event ParameterlessMethod Activated;

    public event ParameterlessMethod Deactivated;

    public void Terminate()
    {
      _terminatedEvent.Set();
      _workThread.Join();
    }

    public bool IsTerminated
    {
      get { return _terminatedEvent == null || _terminatedEvent.WaitOne(0); }
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

    protected ParameterlessMethod DequeueCommand()
    {
      lock (_syncObj)
        return _commandQueue.Count == 0 ? null : _commandQueue.Dequeue();
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
      Type eventType = evt.GetType();
      if (eventType == typeof(KeyEvent))
        ExecuteKeyPress((KeyEvent)evt);
      else if (eventType == typeof(InputTouchDownEvent))
        ExecuteTouchDown(ToUiEvent<TouchDownEvent>((InputTouchDownEvent)evt));
      else if (eventType == typeof(InputTouchUpEvent))
        ExecuteTouchUp(ToUiEvent<TouchUpEvent>((InputTouchUpEvent)evt));
      else if (eventType == typeof(InputTouchMoveEvent))
        ExecuteTouchMove(ToUiEvent<TouchMoveEvent>((InputTouchMoveEvent)evt));
      else if (eventType == typeof(RoutedInputEvent))
        ExecuteRoutedInputEvent((RoutedInputEvent)evt);
    }

    protected void Dispatch(object o)
    {
      lock (_syncObj)
        _callingClientStart = DateTime.Now;
      try
      {
        ParameterlessMethod cmd = o as ParameterlessMethod;
        if (cmd != null)
          cmd();
        InputEvent evt = o as InputEvent;
        if (evt != null)
          DispatchEvent(evt);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("InputManager: Error dispatching '{0}'", e, o);
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
      if (IsTerminated)
        return;
      lock (_syncObj)
      {
        _inputEventQueue.Enqueue(evt);
        _inputAvailableEvent.Set();
      }

      // Reset system's idle time. For remote input i.e. it is not reset by the system itself.
      ServiceRegistration.Get<ISystemStateService>().SetCurrentSuspendLevel(SuspendLevel.DisplayRequired);
    }

    protected void EnqueueCommand(ParameterlessMethod command)
    {
      if (IsTerminated)
        return;
      lock (_syncObj)
      {
        _commandQueue.Enqueue(command);
        _inputAvailableEvent.Set();
      }
    }

    protected void ExecuteKeyPress(KeyEvent evt)
    {
      Key key = evt.Key;
      if (KeyPreview != null)
        KeyPreview(ref key);

      var routedKeyEventArgs = new KeyEventArgs(Environment.TickCount, key);

      // invoke routed KeyPress event
      // if event is already handled, we set Handled to true. By this only handlers registered with handledEventsToo = true will be invoked
      if (key == Key.None)
      {
        routedKeyEventArgs.Handled = true;
      }
      ExecuteRoutedInputEvent(new RoutedInputEvent(routedKeyEventArgs, UIElement.PreviewKeyPressEvent));
      if (routedKeyEventArgs.Handled)
      {
        key = Key.None;
      }

      if (key != Key.None)
      {
        // Try key bindings...
        KeyAction keyAction;
        lock (_syncObj)
          if (!_keyBindings.TryGetValue(key, out keyAction))
            keyAction = null;
        if (keyAction != null)
          keyAction.Action();
      }

      // invoke routed KeyPress event
      // if event is already handled, we set Handled to true. By this only handlers registered with handledEventsToo = true will be invoked
      // it is important to invoke routed KeyPressed event before 'internal' OnKeyPressed, 
      // b/c internal OnKeyPress makes focus handling in Screen as final action if event was not handled
      if (key == Key.None)
      {
        routedKeyEventArgs.Handled = true;
      }
      ExecuteRoutedInputEvent(new RoutedInputEvent(routedKeyEventArgs, UIElement.KeyPressEvent));
      if (routedKeyEventArgs.Handled)
      {
        key = Key.None;
      }

      if (key != Key.None)
      {
        KeyPressedHandler dlgt = KeyPressed;
        if (dlgt != null)
          dlgt(ref key);
      }
    }

    internal void ExecuteTouchMove(TouchMoveEvent evt)
    {
      var dlgt = TouchMove;
      if (dlgt != null)
        dlgt(this, evt);
    }

    internal void ExecuteTouchDown(TouchDownEvent evt)
    {
      var dlgt = TouchDown;
      if (dlgt != null)
        dlgt(this, evt);
    }

    internal void ExecuteTouchUp(TouchUpEvent evt)
    {
      var dlgt = TouchUp;
      if (dlgt != null)
        dlgt(this, evt);
    }

    protected void ExecuteRoutedInputEvent(RoutedInputEvent evt)
    {
      var dlgt = RoutedInputEventFired;
      if (dlgt != null)
        dlgt(evt.EventArgs, evt.RoutedEvents);
    }

    protected void ExecuteApplicationActivated()
    {
      var dlgt = Activated;
      if (dlgt != null)
        dlgt();
    }

    protected void ExecuteApplicationDeactivated()
    {
      var dlgt = Deactivated;
      if (dlgt != null)
        dlgt();
    }

    internal static TE ToUiEvent<TE>(InputTouchEvent inputEvent) where TE : TouchEvent, new()
    {
      TE uiEvent = new TE
      {
        ContactX = inputEvent.ContactX,
        ContactY = inputEvent.ContactY,
        LocationX = inputEvent.LocationX,
        LocationY = inputEvent.LocationY,
        Flags = inputEvent.Flags,
        Id = inputEvent.Id,
        Mask = inputEvent.Mask,
        Time = inputEvent.Time
      };
      return uiEvent;
    }

    internal static TE ToInputEvent<TE>(TouchEvent inputEvent) where TE : InputTouchEvent, new()
    {
      TE uiEvent = new TE
      {
        ContactX = inputEvent.ContactX,
        ContactY = inputEvent.ContactY,
        LocationX = inputEvent.LocationX,
        LocationY = inputEvent.LocationY,
        Flags = inputEvent.Flags,
        Id = inputEvent.Id,
        Mask = inputEvent.Mask,
        Time = inputEvent.Time
      };
      return uiEvent;
    }

    protected void DoWork()
    {
      while (true)
      {
        ParameterlessMethod cmd;
        while ((cmd = DequeueCommand()) != null)
          Dispatch(cmd);
        InputEvent evt;
        while ((evt = DequeueEvent()) != null)
        {
          Dispatch(evt);
          if (IsTerminated)
            break;
        }
        WaitHandle.WaitAny(new WaitHandle[] { _terminatedEvent, _inputAvailableEvent });
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
      TryEvent_NoLock(new RoutedInputEvent(
        new MouseEventArgs(Environment.TickCount),
        UIElement.PreviewMouseMoveEvent, UIElement.MouseMoveEvent));
    }

    public void MouseClick(MouseButtons mouseButtons)
    {
      DateTime now = DateTime.Now;
      _lastInputTime = now;
      _lastMouseUsageTime = now;
      TryEvent_NoLock(new RoutedInputEvent(
        new MouseButtonEventArgs(Environment.TickCount, mouseButtons),
        UIElement.PreviewMouseClickEvent, UIElement.MouseClickEvent));
    }


    public void MouseDown(MouseButtons mouseButtons, int clicks)
    {
      DateTime now = DateTime.Now;
      _lastInputTime = now;
      _lastMouseUsageTime = now;
      TryEvent_NoLock(new RoutedInputEvent(
        new MouseButtonEventArgs(Environment.TickCount, mouseButtons)
        {
          ClickCount = clicks
        },
        UIElement.PreviewMouseDownEvent, UIElement.MouseDownEvent));
    }

    public void MouseUp(MouseButtons mouseButtons, int clicks)
    {
      DateTime now = DateTime.Now;
      _lastInputTime = now;
      _lastMouseUsageTime = now;
      TryEvent_NoLock(new RoutedInputEvent(
        new MouseButtonEventArgs(Environment.TickCount, mouseButtons)
        {
          ClickCount = clicks
        },
        UIElement.PreviewMouseUpEvent, UIElement.MouseUpEvent));
    }

    public void MouseWheel(int delta)
    {
      DateTime now = DateTime.Now;
      _lastInputTime = now;
      _lastMouseUsageTime = now;
      TryEvent_NoLock(new RoutedInputEvent(
        new MouseWheelEventArgs(Environment.TickCount, delta),
        UIElement.PreviewMouseWheelEvent, UIElement.MouseWheelEvent));
    }

    public void KeyPress(Key key)
    {
      _lastInputTime = DateTime.Now;
      TryEvent_NoLock(new KeyEvent(key));
    }

    void IInputManager.TouchDown(TouchDownEvent downEvent)
    {
      ServiceRegistration.Get<ILogger>().Debug("IInputManager: {0}", downEvent);
      DateTime now = DateTime.Now;
      _lastInputTime = now;
      _lastMouseUsageTime = now; //TODO: same as mouse, or another?
      TryEvent_NoLock(ToInputEvent<InputTouchDownEvent>(downEvent));
    }

    void IInputManager.TouchUp(TouchUpEvent upEvent)
    {
      ServiceRegistration.Get<ILogger>().Debug("IInputManager: {0}", upEvent);
      DateTime now = DateTime.Now;
      _lastInputTime = now;
      _lastMouseUsageTime = now; //TODO: same as mouse, or another?
      TryEvent_NoLock(ToInputEvent<InputTouchUpEvent>(upEvent));
    }

    void IInputManager.TouchMove(TouchMoveEvent moveEvent)
    {
      DateTime now = DateTime.Now;
      _lastInputTime = now;
      _lastMouseUsageTime = now; //TODO: same as mouse, or another?
      TryEvent_NoLock(ToInputEvent<InputTouchMoveEvent>(moveEvent));
    }

    public void ExecuteCommand(ParameterlessMethod command)
    {
      EnqueueCommand(command);
    }

    public void AddKeyBinding(Key key, KeyActionDlgt action)
    {
      lock (_syncObj)
        _keyBindings[key] = new KeyAction(key, action);
    }

    public void AddKeyBinding(Key key, VoidKeyActionDlgt action)
    {
      AddKeyBinding(key, () =>
          {
            action();
            return true;
          });
    }

    public void RemoveKeyBinding(Key key)
    {
      lock (_syncObj)
        _keyBindings.Remove(key);
    }

    public void ApplicationActivated()
    {
      EnqueueCommand(ExecuteApplicationActivated);
    }

    public void ApplicationDeactivated()
    {
      EnqueueCommand(ExecuteApplicationDeactivated);
    }

    #endregion
  }
}

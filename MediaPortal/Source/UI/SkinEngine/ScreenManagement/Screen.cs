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
using System.Linq;
using System.Windows.Forms;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.UI.Presentation.Actions;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers;
using MediaPortal.UI.SkinEngine.InputManagement;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.Exceptions;
using SlimDX;

namespace MediaPortal.UI.SkinEngine.ScreenManagement
{
  public delegate void ClosedEventDlgt(Screen screen);

  /// <summary>
  /// Screen class respresenting a logical screen represented by a particular skin.
  /// </summary>
  public class Screen : UIElement, INameScope, IAddChild<FrameworkElement>
  {
    #region Consts

    public const string VIRTUAL_KEYBOARD_DIALOG = "DialogVirtualKeyboard";

    public const string SHOW_EVENT = "Screen.Show";
    public const string CLOSE_EVENT = "Screen.Close";

    #endregion

    #region Classes

    /// <summary>
    /// Encapsulates an UI element together with its depth where it is located in the visual tree.
    /// </summary>
    protected class InvalidControl : IComparable<InvalidControl>
    {
      protected int _treeDepth = -1;
      protected FrameworkElement _element;

      public InvalidControl(FrameworkElement element)
      {
        _element = element;
      }

      protected void InitializeTreeDepth()
      {
        Visual current = _element;
        while (current != null)
        {
          _treeDepth++;
          current = current.VisualParent;
        }
      }

      /// <summary>
      /// Returns the number of steps wich are necessary to follow the <see cref="Visual.VisualParent"/>
      /// reference on the <see cref="Element"/> until the root control is reached.
      /// </summary>
      public int TreeDepth
      {
        get
        {
          if (_treeDepth == -1)
            InitializeTreeDepth();
          return _treeDepth;
        }
      }

      /// <summary>
      /// Returns the invalid element.
      /// </summary>
      public FrameworkElement Element
      {
        get { return _element; }
      }

      public int CompareTo(InvalidControl other)
      {
        return TreeDepth - other.TreeDepth;
      }

      public override int GetHashCode()
      {
        return _element.GetHashCode();
      }

      public override bool Equals(object o)
      {
        if (!(o is InvalidControl))
          return false;
        return _element.Equals(((InvalidControl) o)._element);
      }

      public override string ToString()
      {
        return string.Format("Element: {0}, Depth: {1}", _element, TreeDepth);
      }
    }

    #endregion

    #region Enums

    public enum State
    {
      Preparing,
      Running,
      Closing,
      Closed
    }

    #endregion

    #region Protected fields

    protected State _state = State.Preparing;
    protected DateTime _closeTime = DateTime.MinValue;
    protected Guid _screenInstanceId = Guid.NewGuid();
    protected string _resourceName;
    protected int _skinWidth;
    protected int _skinHeight;
    protected bool _hasBackground = true;

    /// <summary>
    /// Holds the information if our input handlers are currently attached at the <see cref="InputManager"/>.
    /// </summary>
    protected AbstractProperty _hasInputFocusProperty = new SProperty(typeof(bool), false);

    /// <summary>
    /// Always contains the currently focused element in this screen.
    /// </summary>
    protected FrameworkElement _focusedElement = null;
    protected RectangleF? _lastFocusRect = null;

    protected FrameworkElement _root;
    protected PointF? _mouseMovePending = null;
    protected bool _screenShowEventPending = false;
    protected Animator _animator = new Animator();
    protected IDictionary<Key, KeyAction> _keyBindings = null;
    protected Guid? _virtualKeyboardDialogGuid = null;
    protected RenderContext _renderContext;
    protected IDictionary<string, object> _names = new Dictionary<string, object>();

    #endregion

    /// <summary>
    /// Initializes this instance.
    /// </summary>
    /// <param name="resourceName">The logical screen name.</param>
    /// <param name="skinWidth">Native width of the skin providing this screen.</param>
    /// <param name="skinHeight">Native height of the skin providing this screen.</param>
    public void Initialize(string resourceName, int skinWidth, int skinHeight)
    {
      _resourceName = resourceName;
      _skinWidth = skinWidth;
      _skinHeight = skinHeight;
      SkinContext.WindowSizeProperty.Attach(OnWindowSizeChanged);
      InitializeRenderContext();
    }

    public event ClosedEventDlgt Closed;

    public Animator Animator
    {
      get { return _animator; }
    }

    public FrameworkElement Root
    {
      get { return _root; }
    }

    /// <summary>
    /// Gets the native width of the skin which provides this screen. All length coordinates in this screen
    /// refer to that width and to <see cref="SkinHeight"/>.
    /// </summary>
    public int SkinWidth
    {
      get { return _skinWidth; }
    }

    /// <summary>
    /// Gets the native height of the skin which provides this screen. All length coordinates in this screen
    /// refer to that height and to <see cref="SkinWidth"/>.
    /// </summary>
    public int SkinHeight
    {
      get { return _skinHeight; }
    }

    public State ScreenState
    {
      get { return _state; }
      set
      {
        if (value == State.Preparing)
          _root.SetElementState(ElementState.Available);
        else if (value == State.Running)
          _root.SetElementState(ElementState.Running);
        else if (value == State.Closing)
          { } // Nothing to do
        else if (value == State.Closed)
          _root.SetElementState(ElementState.Disposing);
        else
          throw new IllegalCallException("Invalid screen state transition from {0} to {1}", _state, value);
        _state = value;
      }
    }

    public string ResourceName
    {
      get { return _resourceName; }
    }

    public AbstractProperty HasInputFocusProperty
    {
      get { return _hasInputFocusProperty; }
    }

    public bool HasInputFocus
    {
      get { return (bool) _hasInputFocusProperty.GetValue(); }
      set { _hasInputFocusProperty.SetValue(value); }
    }

    public bool HasBackground
    {
      get { return _hasBackground; }
      set { _hasBackground = value; }
    }

    /// <summary>
    /// Shows a virtual keyboard input control which fills the given <paramref name="textProperty"/>.
    /// </summary>
    /// <param name="textProperty">String property instance which will be filled by the virtual keyboard.</param>
    /// <param name="settings">Settings to be used to configure the to-be-shown virtual keyboard.</param>
    public void ShowVirtualKeyboard(AbstractProperty textProperty, VirtualKeyboardSettings settings)
    {
      ScreenManager screenManager = ServiceRegistration.Get<IScreenManager>() as ScreenManager;
      if (screenManager == null)
        // We need to interact with the screen, thus we cannot continue in this case
        return;
      DialogData dd = screenManager.ShowDialogEx(VIRTUAL_KEYBOARD_DIALOG, OnVirtualKeyboardDialogClosed);
      if (dd == null)
        // Virtual keyboard dialog screen not found
        return;
      _virtualKeyboardDialogGuid = dd.DialogInstanceId;
      VirtualKeyboardControl vkc = (VirtualKeyboardControl)
          dd.DialogScreen.Root.FindElement(new TypeMatcher(typeof(VirtualKeyboardControl)));
      if (vkc == null)
        // No virtual keyboard control in our virtual keyboard dialog!?
        return;
      vkc.Closed += OnVirtualKeyboardClosed;
      vkc.Show(textProperty, settings);
    }

    protected void OnVirtualKeyboardDialogClosed(string dialogName, Guid dialogInstanceId)
    {
      if (dialogInstanceId != _virtualKeyboardDialogGuid)
        return;
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.CloseDialog(_virtualKeyboardDialogGuid.Value);
      _virtualKeyboardDialogGuid = null;
    }

    protected void OnVirtualKeyboardClosed(VirtualKeyboardControl virtualKeyboardControl)
    {
      if (!_virtualKeyboardDialogGuid.HasValue)
        return;
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.CloseDialog(_virtualKeyboardDialogGuid.Value);
      _virtualKeyboardDialogGuid = null;
    }

    /// <summary>
    /// Adds a key binding to a command for this screen. Screen key bindings will only concern the current screen.
    /// They will be evaluated before the global key bindings in the InputManager.
    /// </summary>
    /// <param name="key">The key which triggers the command.</param>
    /// <param name="action">The action which should be executed.</param>
    public void AddKeyBinding(Key key, KeyActionDlgt action)
    {
      if (_keyBindings == null)
        _keyBindings = new Dictionary<Key, KeyAction>();
      _keyBindings[key] = new KeyAction(key, action);
    }

    /// <summary>
    /// Removes a key binding from this screen.
    /// </summary>
    /// <param name="key">The key which triggers the command.</param>
    public void RemoveKeyBinding(Key key)
    {
      if (_keyBindings != null)
        _keyBindings.Remove(key);
    }

    public void ResetSize()
    {
      _root.InvalidateLayout(true, false);
    }

    public void Animate()
    {
      lock (_root)
        _animator.Animate();
    }

    public void Render()
    {
      uint time = (uint) Environment.TickCount;
      SkinContext.SystemTickCount = time;

      lock (_root)
      {
        if (_mouseMovePending.HasValue)
        {
          float x = _mouseMovePending.Value.X;
          float y = _mouseMovePending.Value.Y;
          _mouseMovePending = null;
          DoHandleMouseMove(x, y);
        }
        _root.UpdateLayoutRoot();
        if (_screenShowEventPending)
        {
          DoFireScreenShowingEvent();
          _screenShowEventPending = false;
        }
        _root.Render(_renderContext);
      }
    }

    public void AttachInput()
    {
      if (!HasInputFocus)
      {
        InputManager inputManager = InputManager.Instance;
        inputManager.KeyPreview += HandleKeyPreview;
        inputManager.KeyPressed += HandleKeyPress;
        inputManager.MouseMoved += HandleMouseMove;
        inputManager.MouseClicked += HandleMouseClick;
        inputManager.MouseWheeled += HandleMouseWheel;
        HasInputFocus = true;
      }
      PretendMouseMove();
    }

    public void DetachInput()
    {
      if (HasInputFocus)
      {
        InputManager inputManager = InputManager.Instance;
        inputManager.KeyPreview -= HandleKeyPreview;
        inputManager.KeyPressed -= HandleKeyPress;
        inputManager.MouseMoved -= HandleMouseMove;
        inputManager.MouseClicked -= HandleMouseClick;
        inputManager.MouseWheeled -= HandleMouseWheel;
        HasInputFocus = false;
        RemoveCurrentFocus();
      }
    }

    public void Prepare()
    {
      lock (_root)
      {
        _root.Allocate();

        _root.InvalidateLayout(true, true);
        _root.UpdateLayoutRoot();
        _root.SetElementState(ElementState.Running);
      }
    }

    public void Close()
    {
      ScreenState = State.Closed;
      SkinContext.WindowSizeProperty.Detach(OnWindowSizeChanged);
      lock (_root)
      {
        Animator.StopAll();
        _root.Deallocate();
      }
      FireClosed();
      _root.CleanupAndDispose();
    }

    protected void FireClosed()
    {
      ClosedEventDlgt dlgt = Closed;
      if (dlgt != null)
        dlgt(this);
    }

    protected void PretendMouseMove()
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      if (inputManager.IsMouseUsed)
        DoHandleMouseMove(inputManager.MousePosition.X, inputManager.MousePosition.Y);
    }

    protected void DoHandleMouseMove(float x, float y)
    {
      lock (_root)
        if (_root.CanHandleMouseMove())
          _root.OnMouseMove(x, y);
        else
          _mouseMovePending = new PointF(x, y);
    }

    private void OnWindowSizeChanged(AbstractProperty property, object oldVal)
    {
      InitializeRenderContext();
    }

    private void HandleKeyPreview(ref Key key)
    {
      if (!HasInputFocus)
        return;
      _root.OnKeyPreview(ref key);
      // Try key bindings...
      KeyAction keyAction;
      if (_keyBindings != null && _keyBindings.TryGetValue(key, out keyAction))
      {
        keyAction.Action();
        key = Key.None;
      }
    }

    private void HandleKeyPress(ref Key key)
    {
      if (!HasInputFocus)
        return;
      _root.OnKeyPressed(ref key);
      if (key != Key.None)
        UpdateFocus(ref key);
    }

    private void HandleMouseWheel(int numberOfDeltas)
    {
      if (!HasInputFocus)
        return;
      _root.OnMouseWheel(numberOfDeltas);
    }

    private void HandleMouseMove(float x, float y)
    {
      if (!HasInputFocus)
        return;
      DoHandleMouseMove(x, y);
    }

    private void HandleMouseClick(MouseButtons buttons)
    {
      if (!HasInputFocus)
        return;
      bool handled = false;
      _root.OnMouseClick(buttons, ref handled);
      if (handled)
        return;
      // If mouse click was not handled explicitly, map it to an appropriate key event
      Key key = Key.None;
      switch (buttons)
      {
        case MouseButtons.Left:
          key = Key.Ok;
          break;
        case MouseButtons.Right:
          key = Key.ContextMenu;
          break;
      }
      if (key != Key.None)
      {
        HandleKeyPreview(ref key);
        if (key == Key.None)
          return;
        HandleKeyPress(ref key);
      }
    }

    public override bool IsInArea(float x, float y)
    {
      return true; // Screens always cover the whole physical screen
    }

    protected void InitializeRenderContext()
    {
      _renderContext = CreateInitialRenderContext();
    }

    public RenderContext CreateInitialRenderContext()
    {
      Matrix transform = Matrix.Scaling((float) SkinContext.WindowSize.Width / _skinWidth, (float) SkinContext.WindowSize.Height / _skinHeight, 1);
      return new RenderContext(transform, null, new RectangleF(0, 0, _skinWidth, _skinHeight));
    }

    /// <summary>
    /// Returns the currently focused element in this screen.
    /// </summary>
    public FrameworkElement FocusedElement
    {
      get { return _focusedElement; }
    }

    /// <summary>
    /// Dialog/screen instance id. With this id, this screen can uniquely be identified.
    /// </summary>
    public Guid ScreenInstanceId
    {
      get { return _screenInstanceId; }
      internal set { _screenInstanceId = value; }
    }

    public void FireScreenShowingEvent()
    {
      _screenShowEventPending = true;
    }

    protected void DoFireScreenShowingEvent()
    {
      FireEvent(SHOW_EVENT);
    }

    public void FireScreenClosingEvent()
    {
      FireEvent(CLOSE_EVENT);
      _closeTime = FindCloseEventCompletionTime();
    }

    public bool DoneClosing
    {
      get { return SkinContext.FrameRenderingStartTime.CompareTo(_closeTime) > 0; }
    }

    /// <summary>
    /// Informs the screen that the specified <paramref name="focusedElement"/> gained the
    /// focus. This will reset the focus on the former focused element.
    /// This will be called from the <see cref="FrameworkElement"/> class.
    /// </summary>
    /// <param name="focusedElement">The element which gained focus.</param>
    /// <returns><c>true</c>, if the focus could be set. This is the case when the given <paramref name="focusedElement"/>
    /// has already valid <see cref="FrameworkElement.ActualBounds"/>. Else, <c>false</c>; in that case, this method
    /// should be called again after the element arranged its layout.</returns>
    public void FrameworkElementGotFocus(FrameworkElement focusedElement)
    {
      if (_focusedElement == focusedElement)
        return;
      RemoveCurrentFocus();
      if (focusedElement == null)
        return;
      _focusedElement = focusedElement;
      _lastFocusRect = focusedElement.ActualBounds;
      focusedElement.FireEvent(FrameworkElement.GOTFOCUS_EVENT);
      return;
    }

    /// <summary>
    /// Informs the screen about a change in the location of the focused element.
    /// </summary>
    /// <param name="focusRect">Actual bounds of the element which currently has focus.</param>
    internal void UpdateFocusRect(RectangleF focusRect)
    {
      _lastFocusRect = focusRect;
    }

    /// <summary>
    /// Checks the specified <paramref name="key"/> if it changes the focus and uses it to set a new
    /// focused element.
    /// </summary>
    /// <param name="key">A key which was pressed.</param>
    protected void UpdateFocus(ref Key key)
    {
      FrameworkElement focusedElement = FocusedElement;
      FrameworkElement cntl = PredictFocus(focusedElement == null || !focusedElement.IsVisible ? new RectangleF?() :
          focusedElement.ActualBounds, key);
      if (cntl != null)
      {
        if (cntl.TrySetFocus(true))
          key = Key.None;
      }
    }

    /// <summary>
    /// Removes the focus on the currently focused element. After this method, no element has the focus any
    /// more.
    /// </summary>
    public void RemoveCurrentFocus()
    {
      if (_focusedElement != null)
        if (_focusedElement.HasFocus)
          _focusedElement.ResetFocus(); // Will trigger the FrameworkElementLostFocus method, which sets _focusedElement to null
    }

    /// <summary>
    /// Informs the screen that the specified <paramref name="focusedElement"/> lost its
    /// focus. This will be called from the <see cref="FrameworkElement"/> class.
    /// </summary>
    /// <param name="focusedElement">The element which had focus before.</param>
    public void FrameworkElementLostFocus(FrameworkElement focusedElement)
    {
      if (focusedElement != null && _focusedElement == focusedElement)
      {
        _focusedElement = null;
        focusedElement.FireEvent(FrameworkElement.LOSTFOCUS_EVENT);
      }
    }

    public static FrameworkElement FindFirstFocusableElement(FrameworkElement searchRoot)
    {
      return searchRoot.PredictFocus(null, MoveFocusDirection.Down);
    }

    /// <summary>
    /// Predicts which FrameworkElement should get the focus when the specified <paramref name="key"/>
    /// was pressed.
    /// </summary>
    /// <param name="currentFocusRect">The borders of the currently focused control.</param>
    /// <param name="key">The key to evaluate.</param>
    /// <returns>Framework element which gets focus when the specified <paramref name="key"/> was
    /// pressed, or <c>null</c>, if no focus change should take place.</returns>
    public FrameworkElement PredictFocus(RectangleF? currentFocusRect, Key key)
    {
      FrameworkElement element = _root;
      if (element == null)
        return null;
      if (key == Key.Up)
        return element.PredictFocus(currentFocusRect, MoveFocusDirection.Up);
      if (key == Key.Down)
        return element.PredictFocus(currentFocusRect, MoveFocusDirection.Down);
      if (key == Key.Left)
        return element.PredictFocus(currentFocusRect, MoveFocusDirection.Left);
      if (key == Key.Right)
        return element.PredictFocus(currentFocusRect, MoveFocusDirection.Right);
      return null;
    }

    /// <summary>
    /// Calculates how long is necessary for any animations triggered by the screen CLOSE_EVENT
    /// to complete.
    /// </summary>
    /// <returns>The time at which all triggered animations will be completed.</returns>
    public DateTime FindCloseEventCompletionTime()
    {
      DateTime endTime = DateTime.MinValue;

      double duration = Triggers.OfType<EventTrigger>().Where(trigger => trigger.RoutedEvent == CLOSE_EVENT).
          Aggregate(0.0, (current, closeTriggers) => Math.Max(current, closeTriggers.Actions.Max(action => action.DurationInMilliseconds)));
      if (duration > 0.001)
        endTime = SkinContext.FrameRenderingStartTime.AddMilliseconds(duration);

      return endTime;
    }

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      if (_root != null)
        childrenOut.Add(_root);
    }

    public override string ToString()
    {
      return string.IsNullOrEmpty(_resourceName) ? "Unnamed screen" : _resourceName;
    }

    #region INamescope implementation

    public object FindName(string name)
    {
      object obj;
      if (_names.TryGetValue(name, out obj))
        return obj;
      return null;
    }

    public void RegisterName(string name, object instance)
    {
      object old;
      if (_names.TryGetValue(name, out old) && ReferenceEquals(old, instance))
        return;
      _names.Add(name, instance);
    }

    public void UnregisterName(string name)
    {
      _names.Remove(name);
    }

    #endregion

    #region IAddChild<FrameworkElement> implementation

    public void AddChild(FrameworkElement child)
    {
      _root = child;
      SetScreen(this);
      ScreenState = _state; // Set the visual's element state via our ScreenState setter
      _root.VisualParent = this;
      InitializeTriggers();
    }

    #endregion
  }
}

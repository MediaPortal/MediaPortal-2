#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.UI.Presentation.Actions;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.InputManagement;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UI.SkinEngine.Utils;
using SlimDX;

namespace MediaPortal.UI.SkinEngine.ScreenManagement
{
  public delegate void ClosedEventDlgt(Screen screen);

  /// <summary>
  /// Screen class respresenting a logical screen represented by a particular skin.
  /// </summary>
  public class Screen : NameScope
  {
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
    }

    #endregion

    #region Enums

    public enum State
    {
      Opening,
      Running,
      Closing
    }

    #endregion

    #region Protected fields

    protected AbstractProperty _virtualKeyboardControlProperty = new SProperty(typeof(VirtualKeyboardControl), null);
    protected AbstractProperty _opened = new SProperty(typeof(bool), true);
    protected State _state = State.Running;
    protected Guid _screenInstanceId = Guid.NewGuid();
    protected string _name;
    protected int _skinWidth;
    protected int _skinHeight;

    /// <summary>
    /// Holds the information if our input handlers are currently attached at the <see cref="InputManager"/>.
    /// </summary>
    protected bool _attachedInput = false;

    /// <summary>
    /// Always contains the currently focused element in this screen.
    /// </summary>
    protected FrameworkElement _focusedElement = null;
    protected RectangleF? _lastFocusRect = null;

    protected FrameworkElement _visual;
    protected Animator _animator;
    protected List<InvalidControl> _invalidLayoutControls = new List<InvalidControl>();
    protected IDictionary<Key, KeyAction> _keyBindings = null;
    protected RenderContext _renderContext;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Screen"/> class.
    /// </summary>
    /// <param name="name">The logical screen name.</param>
    /// <param name="skinWidth">Native width of the skin providing this screen.</param>
    /// <param name="skinHeight">Native height of the skin providing this screen.</param>
    public Screen(string name, int skinWidth, int skinHeight)
    {
      _name = name;
      _skinWidth = skinWidth;
      _skinHeight = skinHeight;
      _animator = new Animator();
      SkinContext.WindowSizeProperty.Attach(OnWindowSizeChanged);
      InitializeRenderContext();
    }

    public event ClosedEventDlgt Closed;

    public Animator Animator
    {
      get { return _animator; }
    }

    public FrameworkElement Visual
    {
      get { return _visual; }
      set
      {
        _visual = value;
        if (_visual != null)
          _visual.SetScreen(this);
      }
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

    /// <summary>
    /// Returns if this screen is still open.
    /// </summary>
    public bool IsOpened
    {
      get { return (bool) _opened.GetValue(); }
      set { _opened.SetValue(value); }
    }

    public AbstractProperty IsOpenedProperty
    {
      get { return _opened; }
      set { _opened = value; }
    }

    public State ScreenState
    {
      get { return _state; }
      set { _state = value; }
    }

    public string Name
    {
      get { return _name; }
    }

    public bool HasInputFocus
    {
      get { return _attachedInput; }
    }

    public AbstractProperty VirtualKeyboardControlProperty
    {
      get { return _virtualKeyboardControlProperty; }
    }

    public VirtualKeyboardControl VirtualKeyboardControl
    {
      get { return (VirtualKeyboardControl) _virtualKeyboardControlProperty.GetValue(); }
      private set { _virtualKeyboardControlProperty.SetValue(value); }
    }

    /// <summary>
    /// Called from the (single) virtual keyboard control instance in this screen during initialization.
    /// </summary>
    /// <param name="virtualKeyboardControl">The single virtual keyboard control to be used this screen.</param>
    public void SetVirtalKeyboardControl(VirtualKeyboardControl virtualKeyboardControl)
    {
      VirtualKeyboardControl = virtualKeyboardControl;
      if (virtualKeyboardControl != null)
        virtualKeyboardControl.IsVisible = false;
    }

    /// <summary>
    /// Shows a virtual keyboard input control which fills the given <paramref name="textProperty"/>.
    /// </summary>
    /// <param name="textProperty">String property instance which will be filled by the virtual keyboard.</param>
    /// <param name="settings">Settings to be used to configure the to-be-shown virtual keyboard.</param>
    public void ShowVirtualKeyboard(AbstractProperty textProperty, VirtualKeyboardSettings settings)
    {
      VirtualKeyboardControl virtualKeyboardControl = VirtualKeyboardControl;
      if (virtualKeyboardControl == null)
        return;
      virtualKeyboardControl.Show(textProperty, settings);
    }

    /// <summary>
    /// Adds a key binding to a command for this screen. Screen key bindings will only concern the current screen.
    /// They will be evaluated before the global key bindings in the InputManager.
    /// </summary>
    /// <param name="key">The key which triggers the command.</param>
    /// <param name="action">The action which should be executed.</param>
    public void AddKeyBinding(Key key, ActionDlgt action)
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

    public void Reset()
    {
      _visual.InvalidateLayout();
      _visual.Initialize();
    }

    public void Animate()
    {
      lock (_visual)
        _animator.Animate();
    }

    public void Render()
    {
      uint time = (uint) Environment.TickCount;
      SkinContext.SystemTickCount = time;

      lock (_visual)
      {
        // Updating invalid elements works as follows:
        // We start updating the layout of elements which have the biggest distance from the visual root, moving
        // towards the root. The distance is the number of references which must be followed from the element along its
        // VisualParent property until the root visual is reached.
        // If we wouldn't follow that order and we would call the UpdateLayout method of an invalid parent P before the
        // UpdateLayout method of an invalid child C, we could produce a situation where C gets arranged but was not measured.
        //
        // To achieve that call sequence, the _invalidLayoutControls list is ordered by the distance of the referenced
        // element from the root.
        // During each call to UpdateLayout() on an invalidated element, our _invalidLayoutControls list might get
        // more entries because the updated element escalates the layout update to its parent. That's the reason why we
        // cannot simply use an enumerator.
        lock (_invalidLayoutControls)
          while (_invalidLayoutControls.Count > 0)
          {
            InvalidControl ic = _invalidLayoutControls[_invalidLayoutControls.Count-1];
            _invalidLayoutControls.RemoveAt(_invalidLayoutControls.Count-1);
            ic.Element.UpdateLayout();
          }
        _visual.Render(_renderContext);
      }
    }

    public void AttachInput()
    {
      if (!_attachedInput)
      {
        InputManager inputManager = InputManager.Instance;
        inputManager.KeyPreview += OnKeyPreview;
        inputManager.KeyPressed += OnKeyPressed;
        inputManager.MouseMoved += OnMouseMove;
        inputManager.MouseWheeled += OnMouseWheel;
        _attachedInput = true;
      }
    }

    public void DetachInput()
    {
      if (_attachedInput)
      {
        InputManager inputManager = InputManager.Instance;
        inputManager.KeyPreview -= OnKeyPreview;
        inputManager.KeyPressed -= OnKeyPressed;
        inputManager.MouseMoved -= OnMouseMove;
        inputManager.MouseWheeled -= OnMouseWheel;
        _attachedInput = false;
        RemoveCurrentFocus();
      }
    }

    public void Prepare()
    {
      lock (_visual)
      {
        _visual.Deallocate();
        _visual.Allocate();
        _visual.Initialize();

        _visual.InvalidateLayout();
        _visual.UpdateLayout();
      }
    }

    public void Show()
    {
      PretendMouseMove();
    }

    public void Close()
    {
      SkinContext.WindowSizeProperty.Detach(OnWindowSizeChanged);
      lock (_visual)
      {
        Animator.StopAll();
        _visual.Deallocate();
      }
      FireClosed();
      _visual.Dispose();
    }

    protected void FireClosed()
    {
      ClosedEventDlgt dlgt = Closed;
      if (dlgt != null)
        dlgt(this);
    }

    protected void PretendMouseMove()
    {
      lock (_visual)
      {
        IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
        if (_attachedInput && inputManager.IsMouseUsed)
          _visual.OnMouseMove(inputManager.MousePosition.X, inputManager.MousePosition.Y);
      }
    }

    private void OnWindowSizeChanged(AbstractProperty property, object oldVal)
    {
      InitializeRenderContext();
    }

    private void OnKeyPreview(ref Key key)
    {
      if (!_attachedInput)
        return;
      _visual.OnKeyPreview(ref key);
      // Try key bindings...
      KeyAction keyAction;
      if (_keyBindings != null && _keyBindings.TryGetValue(key, out keyAction))
      {
        keyAction.Action();
        key = Key.None;
      }
    }

    private void OnKeyPressed(ref Key key)
    {
      if (!_attachedInput)
        return;
      _visual.OnKeyPressed(ref key);
      if (key != Key.None)
        UpdateFocus(ref key);
    }

    private void OnMouseWheel(int numberOfDeltas)
    {
      if (!_attachedInput)
        return;
      _visual.OnMouseWheel(numberOfDeltas);
    }

    private void OnMouseMove(float x, float y)
    {
      if (!_attachedInput)
        return;
      _visual.OnMouseMove(x, y);
    }

    public void InvalidateLayout(UIElement element)
    {
      FrameworkElement fe = element as FrameworkElement;
      if (fe == null)
        return;
      InvalidControl ic = new InvalidControl(fe);
      lock (_invalidLayoutControls)
      {
        if (_invalidLayoutControls.Contains(ic))
          return;
        int index = _invalidLayoutControls.BinarySearch(ic);
        if (index < 0)
          index = ~index; // See IList<T>.BinarySearch(T)
        _invalidLayoutControls.Insert(index, ic);
      }
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

    /// <summary>
    /// Informs the screen that the specified <paramref name="focusedElement"/> gained the
    /// focus. This will reset the focus on the former focused element.
    /// This will be called from the <see cref="FrameworkElement"/> class.
    /// </summary>
    /// <param name="focusedElement">The element which gained focus.</param>
    /// <returns><c>true</c>, if the focus could be set. This is the case when the given <paramref name="focusedElement"/>
    /// has already valid <see cref="FrameworkElement.ActualBounds"/>. Else, <c>false</c>; in that case, this method
    /// should be called again after the element arranged its layout.</returns>
    public bool FrameworkElementGotFocus(FrameworkElement focusedElement)
    {
      if (_focusedElement == focusedElement)
        return true;
      RemoveCurrentFocus();
      if (!GeometricHelper.HasExtends(focusedElement.ActualBounds))
        return false;
      _focusedElement = focusedElement;
      _lastFocusRect = focusedElement.ActualBounds;
      _visual.FireEvent(FrameworkElement.GOTFOCUS_EVENT);
      return true;
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
      if (_focusedElement == focusedElement)
      {
        _focusedElement = null;
        _visual.FireEvent(FrameworkElement.LOSTFOCUS_EVENT);
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
      FrameworkElement element = _visual;
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
  }
}

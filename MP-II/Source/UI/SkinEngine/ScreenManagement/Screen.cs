#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Drawing;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.UI.Presentation.Actions;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.InputManagement;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.ScreenManagement
{
  /// <summary>
  /// Screen class respresenting a logical screen represented by a particular skin.
  /// </summary>
  public class Screen: NameScope
  {
    #region Enums

    public enum State
    {
      Opening,
      Running,
      Closing
    }

    #endregion

    #region Proteced fields

    protected string _name;
    protected State _state = State.Running;
    protected RectangleF? _lastFocusRect = null;

    /// <summary>
    /// Holds the information if our input handlers are currently attached at
    /// the <see cref="InputManager"/>.
    /// </summary>
    protected bool _attachedInput = false;

    /// <summary>
    /// Always contains the currently focused element in this screen.
    /// </summary>
    protected FrameworkElement _focusedElement = null;

    protected AbstractProperty _opened;
    public event EventHandler Closed;
    protected UIElement _visual;
    protected bool _setFocusedElement = false;
    protected Animator _animator;
    protected IList<IUpdateEventHandler> _invalidControls = new List<IUpdateEventHandler>();
    protected IDictionary<Key, KeyAction> _keyBindings = null;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Screen"/> class.
    /// </summary>
    /// <param name="name">The logical screen name.</param>
    public Screen(string name)
    {
      if (name == null)
      {
        throw new ArgumentNullException("name");
      }
      if (name.Length == 0)
      {
        throw new ArgumentOutOfRangeException("name");
      }

      _opened = new SProperty(typeof(bool), true);
      _name = name;
      _animator = new Animator();
    }

    public Animator Animator
    {
      get { return _animator; }
    }

    public UIElement Visual
    {
      get { return _visual; }
      set
      {
        _visual = value;
        if (_visual != null)
          _visual.SetScreen(this);
      }
    }

    public FrameworkElement RootElement
    {
      get { return _visual as FrameworkElement; }
    }

    /// <summary>
    /// Returns if this screen is still open or if it should be closed.
    /// </summary>
    /// <value><c>true</c> if this screen is still open; otherwise, <c>false</c>.</value>
    public bool IsOpened
    {
      get { return (bool)_opened.GetValue(); }
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
      //Trace.WriteLine("Screen Reset: " + Name);
      if (SkinContext.UseBatching)
        _visual.DestroyRenderTree();
      GraphicsDevice.InitializeZoom();
      _visual.Invalidate();
      _visual.Initialize();
    }

    public void Deallocate()
    {
      //Trace.WriteLine("Screen Deallocate: " + Name);
      if (SkinContext.UseBatching)
        _visual.DestroyRenderTree();
      _visual.Deallocate();
    }

    public void Animate()
    {
      lock (_visual)
        _animator.Animate();
    }

    public void Render()
    {
      uint time = (uint) Environment.TickCount;
      SkinContext.TimePassed = time;
      SkinContext.FinalTransform = new ExtendedMatrix();

      lock (_visual)
      {
        if (SkinContext.UseBatching)
        {
          Update();
          return;
        }
        else
          _visual.Render();
      }
      if (_setFocusedElement)
      {
        if (_visual.FocusedElement != null)
          _visual.FocusedElement.TrySetFocus(true);
        else
        {
          FrameworkElement fe = _visual as FrameworkElement;
          if (fe != null)
            fe.TrySetFocus(true);
        }
        _setFocusedElement = false;
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
        _attachedInput = false;
        RemoveCurrentFocus();
      }
    }

    public void Prepare()
    {
      lock (_visual)
      {
        if (SkinContext.UseBatching)
          _visual.DestroyRenderTree();
        _invalidControls.Clear();
        _visual.Deallocate();
        _visual.Allocate();
        _visual.Invalidate();
        _visual.Initialize();
        //if (SkinContext.UseBatching)
        //  _visual.BuildRenderTree();
        _visual.UpdateLayout();
      }
    }

    public void Show()
    {
      //Trace.WriteLine("Screen Show: " + Name);

      lock (_visual)
      {
        _setFocusedElement = true;
        IInputManager inputManager = ServiceScope.Get<IInputManager>();
        if (_attachedInput && inputManager.IsMouseUsed)
          _visual.OnMouseMove(inputManager.MousePosition.X, inputManager.MousePosition.Y);
      }
    }

    public void Hide()
    {
      //Trace.WriteLine("Screen Hide: " + Name);
      lock (_visual)
      {
        Animator.StopAll();
        if (SkinContext.UseBatching)
          _visual.DestroyRenderTree();
        _visual.Deallocate();
        _invalidControls.Clear();
      }
      if (Closed != null)
        Closed(this, null);
      _visual.Dispose();
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

    private void OnMouseMove(float x, float y)
    {
      if (!_attachedInput)
        return;
      _visual.OnMouseMove(x, y);
    }

    public void Invalidate(IUpdateEventHandler ctl)
    {
      if (!SkinContext.UseBatching)
        return;

      lock (_invalidControls)
      {
        if (!_invalidControls.Contains(ctl))
          _invalidControls.Add(ctl);
      }
    }

    void Update()
    {
      IList<IUpdateEventHandler> ctls;
      lock (_invalidControls)
      {
        if (_invalidControls.Count == 0) 
          return;
        ctls = _invalidControls;
        _invalidControls = new List<IUpdateEventHandler>();
      }
      foreach (IUpdateEventHandler ctl in ctls)
        ctl.Update();
    }

    /// <summary>
    /// Returns the currently focused element in this screen.
    /// </summary>
    public FrameworkElement FocusedElement
    {
      get { return _focusedElement; }
    }

    /// <summary>
    /// Informs the screen that the specified <paramref name="focusedElement"/> gained the
    /// focus. This will reset the focus on the former focused element.
    /// This will be called from the <see cref="FrameworkElement"/> class.
    /// </summary>
    /// <param name="focusedElement">The element which gained focus.</param>
    public void FrameworkElementGotFocus(FrameworkElement focusedElement)
    {
      if (_focusedElement != focusedElement)
      {
        RemoveCurrentFocus();
        if (!HasExtends(focusedElement.ActualBounds))
          return;
        _focusedElement = focusedElement;
        _lastFocusRect = focusedElement.ActualBounds;
        _visual.FireEvent(FrameworkElement.GOTFOCUS_EVENT);
      }
    }

    /// <summary>
    /// Tests whether any of the rect's bounds is <see cref="float.NaN"/>.
    /// </summary>
    /// <param name="rect">The rect to test.</param>
    /// <returns><c>true</c> if none of the rect's bounds is <see cref="float.NaN"/>. <c>false</c>, if any
    /// of its bounds is <see cref="float.NaN"/>.</returns>
    private static bool HasExtends(RectangleF rect)
    {
      return !float.IsNaN(rect.Top) && !float.IsNaN(rect.Bottom) && !float.IsNaN(rect.Left) && !float.IsNaN(rect.Right);
    }

    /// <summary>
    /// Checks the specified <paramref name="key"/> if it changes the focus and uses it to set a new
    /// focused element.
    /// </summary>
    /// <param name="key">A key which was pressed.</param>
    protected void UpdateFocus(ref Key key)
    {
      FrameworkElement cntl = PredictFocus(FocusedElement == null ? _lastFocusRect :
          FocusedElement.ActualBounds, key);
      if (cntl != null)
      {
        cntl.HasFocus = true;
        if (cntl.HasFocus)
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
          _focusedElement.HasFocus = false; // Will trigger the FrameworkElementLostFocus method, which sets _focusedElement to null
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
      FrameworkElement element = Visual as FrameworkElement;
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
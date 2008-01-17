#define TESTXAML
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

using System;
using System.Collections.Generic;
using System.Threading;
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.InputManager;
using MediaPortal.Core.Properties;
using MediaPortal.Core.WindowManager;
using SkinEngine.Controls;
using SkinEngine.Controls.Brushes;
using SkinEngine.Controls.Visuals;
using SkinEngine.Controls.Transforms;
using SkinEngine.Controls.Panels;
using SkinEngine.Controls.Animations;
using SkinEngine.Skin;
using Control = SkinEngine.Controls.Control;
namespace SkinEngine
{
  public class Window : IWindow
  {
    #region enums

    public enum State
    {
      Opening,
      Running,
      Closing
    }

    #endregion

    #region variables

    private string _name;
    private bool _hasFocus;
    private State _state = State.Running;
    private List<IControlExt> _controls;
    private Dictionary<string, IControlExt> _controlMap;
    private Dictionary<string, Model> _models;
    private KeyPressedHandler _keyPressHandler;
    private MouseMoveHandler _mouseMoveHandler;
    private WaitCursor _waitCursor;
    private Property _opened;
    private Property _controlsProperty;
    private Property _resultProperty;
    private bool _attachedInput = false;
    private string _defaultFocus;
    private Thread _thread;
    private ICommand _openCommand;
    private ICommandParameter _openCommandParameter;
    private ICommand _closeCommand;
    private ICommandParameter _closeCommandParameter;
    public event EventHandler OnClose;
    private Control _focusedControl;
    private Control _focusedMouseControl;
    private bool _history;
    UIElement _visual;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Window"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    public Window(string name)
    {
      if (name == null)
      {
        throw new ArgumentNullException("name");
      }
      if (name.Length == 0)
      {
        throw new ArgumentOutOfRangeException("name");
      }

      _history = true;
      _opened = new Property(true);
      _waitCursor = new WaitCursor();
      _name = name;
      _controls = new List<IControlExt>();
      _controlMap = new Dictionary<string, IControlExt>();
      _controlsProperty = new Property(_controls);
      _models = new Dictionary<string, Model>();
      _keyPressHandler = new KeyPressedHandler(OnKeyPressed);
      _mouseMoveHandler = new MouseMoveHandler(OnMouseMove);

      //XamlLoader loader = new XamlLoader();
      //_visual = (UIElement)loader.Load("test.xml");


    }
    public UIElement Visual
    {
      get
      {
        return _visual;
      }
      set
      {
        _visual = value;
        if (_visual != null)
        {
          _visual.IsArrangeValid = true;
        }
      }
    }
    public Control FocusedControl
    {
      get { return _focusedControl; }
      set { _focusedControl = value; }
    }
    public FrameworkElement RootElement
    {
      get
      {
#if TESTXAML
        return _visual as FrameworkElement;
#else
        return null;
#endif
      }
    }

    public Control FocusedMouseControl
    {
      get { return _focusedMouseControl; }
      set { _focusedMouseControl = value; }
    }


    public bool History
    {
      get { return _history; }
      set { _history = value; }
    }

    /// <summary>
    /// Returns if this window is still open or if it should be closed
    /// </summary>
    /// <value><c>true</c> if this window is still open; otherwise, <c>false</c>.</value>
    public bool IsOpened
    {
      get { return (bool)_opened.GetValue(); }
      set { _opened.SetValue(value); }
    }

    public Property IsOpenedProperty
    {
      get { return _opened; }
      set { _opened = value; }
    }

    public ICommand OpenCommand
    {
      get { return _openCommand; }
      set { _openCommand = value; }
    }
    public ICommandParameter OpenCommandParameter
    {
      get { return _openCommandParameter; }
      set { _openCommandParameter = value; }
    }

    public ICommand CloseCommand
    {
      get { return _closeCommand; }
      set { _closeCommand = value; }
    }

    public ICommandParameter CloseCommandParameter
    {
      get { return _closeCommandParameter; }
      set { _closeCommandParameter = value; }
    }

    /// <summary>
    /// Gets or sets the result value (for dialogs).
    /// </summary>
    /// <value>The result.</value>
    public Property Result
    {
      get { return _resultProperty; }
      set { _resultProperty = value; }
    }

    /// <summary>
    /// Gets or sets the name of the control which should receive focus when we open the window
    /// </summary>
    /// <value>The default focus.</value>
    public string DefaultFocus
    {
      get { return _defaultFocus; }
      set
      {
        if (value == null)
        {
          throw new ArgumentNullException("DefaultFocus");
        }
        _defaultFocus = value;
      }
    }


    /// <summary>
    /// Adds a model to the window.
    /// </summary>
    /// <param name="name">The model name.</param>
    /// <param name="model">The model.</param>
    public void AddModel(string name, Model model)
    {
      if (model == null)
      {
        throw new ArgumentNullException("model");
      }
      if (name == null)
      {
        throw new ArgumentNullException("name");
      }
      if (name.Length == 0)
      {
        throw new ArgumentOutOfRangeException("name");
      }
      _models[name] = model;
    }

    /// <summary>
    /// adds a control to the window
    /// </summary>
    /// <param name="control">The control.</param>
    public void AddControl(IControlExt control)
    {
      if (control == null)
      {
        throw new ArgumentNullException("control");
      }
      _controls.Add(control);
      if (control.Name.Length > 0)
      {
        _controlMap[control.Name] = control;
      }
      _controlsProperty.SetValue(_controlMap.Count + _controls.Count + 1);
    }

    public Property ControlCountProperty
    {
      get { return _controlsProperty; }
      set { _controlsProperty = value; }
    }

    /// <summary>
    /// Adds a named-control to the window.
    /// </summary>
    /// <param name="control">The control.</param>
    public void AddNamedControl(IControlExt control)
    {
      if (control == null)
      {
        throw new ArgumentNullException("control");
      }
      if (control.Name == null)
      {
        throw new ArgumentNullException("control.Name");
      }
      if (control.Name.Length == 0)
      {
        throw new ArgumentOutOfRangeException("control.Name");
      }
      _controlMap[control.Name] = control;
      _controlsProperty.SetValue(_controlMap.Count + _controls.Count + 1);
    }

    /// <summary>
    /// returns the model with the specified name
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public Model GetModelByName(string name)
    {
      if (_models.ContainsKey(name))
      {
        return _models[name];
      }
      return null;
    }

    /// <summary>
    /// returns the control with the specified name
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public Control GetControlByName(string name)
    {
      if (_controlMap.ContainsKey(name))
      {
        return (Control)_controlMap[name];
      }
      return null;
    }

    /// <summary>
    /// Gets the window-name.
    /// </summary>
    /// <value>The name.</value>
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// Gets or sets the state of the window.
    /// </summary>
    /// <value>The state of the window.</value>
    public State WindowState
    {
      get { return _state; }
      set { _state = value; }
    }


    /// <summary>
    /// Gets or sets a value indicating whether this window has focus.
    /// </summary>
    /// <value><c>true</c> if this window has focus; otherwise, <c>false</c>.</value>
    public bool HasFocus
    {
      get { return _hasFocus; }
      set
      {
        _hasFocus = value;
        if (!_hasFocus)
          FocusedControl = null;
      }
    }

    /// <summary>
    /// Gets or sets the controls.
    /// </summary>
    /// <value>The controls.</value>
    public List<IControlExt> Controls
    {
      get { return _controls; }
      set { _controls = value; }
    }

    /// <summary>
    /// Gets the wait cursor.
    /// </summary>
    /// <value>The wait cursor.</value>
    public WaitCursor WaitCursor
    {
      get { return _waitCursor; }
    }

    /// <summary>
    /// Renders this window.
    /// </summary>
    public void Render()
    {
      uint time = (uint)Environment.TickCount;
      SkinContext.TimePassed = time;
      SkinContext.FinalMatrix = new ExtendedMatrix();

#if TESTXAML
#else
      for (int i = 0; i < _controls.Count; ++i)
      {
        //_controls[i].UpdateProperties();
        _controls[i].DoRender(time);
      }
      _waitCursor.Render(time);
#endif
      if (!IsOpened && _thread == null && !IsAnimating)
      {
        //we cannot close the window from the render thread
        //so start a new workerthread to close ourselves
        _thread = new Thread(new ThreadStart(CloseThisWindow));
        _thread.Name = "Window Close Thread";
        _thread.Start();
      }

#if TESTXAML
      _visual.Render();
      _visual.Animate();
#endif
    }

    /// <summary>
    /// Closes the this window.
    /// </summary>
    private void CloseThisWindow()
    {
      if (_closeCommand != null)
      {
        _closeCommand.Execute(_closeCommandParameter);
      }
      WindowManager manager = (WindowManager)ServiceScope.Get<IWindowManager>();
      if (manager.CurrentWindow == this)
      {
        manager.ShowPreviousWindow();
      }
      _thread = null;
    }

    /// <summary>
    /// Called when window should be shown
    /// </summary>
    /// <param name="animate">if set to <c>true</c> [animate].</param>
    public void Show(bool animate)
    {
      if (!_attachedInput)
      {
        ServiceScope.Get<IInputManager>().OnKeyPressed += _keyPressHandler;
        ServiceScope.Get<IInputManager>().OnMouseMove += _mouseMoveHandler;
        _attachedInput = true;
      }
#if TESTXAML
      _visual.Invalidate();
      VisualTreeHelper.Instance.SetRootElement(_visual);
#else
      if (animate)
      {
        for (int i = 0; i < _controls.Count; ++i)
        {
          _controls[i].Reset();
        }

        if (_defaultFocus != null)
        {
          IControlExt c = GetControlByName(_defaultFocus);
          if (c != null)
          {
            c.HasFocus = true;
          }
        }
      }
#endif
      if (_openCommand != null)
      {
        _openCommand.Execute(_openCommandParameter);
      }
    }

    /// <summary>
    /// Called when a keypress has been received
    /// </summary>
    /// <param name="key">The key.</param>
    private void OnKeyPressed(ref Key key)
    {
      if (!HasFocus)
      {
        return;
      }
#if TESTXAML
      _visual.OnKeyPressed(ref key);
#else
      for (int i = 0; i < _controls.Count; ++i)
      {
        _controls[i].OnKeyPressed(ref key);
      }
      if (FocusedControl == null)
      {
        if (_defaultFocus != null)
        {
          IControlExt c = GetControlByName(_defaultFocus);
          if (c != null)
          {
            c.HasFocus = true;
          }
        }
      }
#endif
    }

    /// <summary>
    /// Called when the mouse was moved
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    private void OnMouseMove(float x, float y)
    {
      if (!HasFocus)
      {
        return;
      }
#if TESTXAML
      _visual.OnMouseMove(x, y);
#else
      for (int i = 0; i < _controls.Count; ++i)
      {
        _controls[i].OnMouseMove(x, y);
      }
      if (FocusedControl == null)
      {
        if (_defaultFocus != null)
        {
          IControlExt c = GetControlByName(_defaultFocus);
          if (c != null)
          {
            c.HasFocus = true;
          }
        }
      }
#endif
    }

    /// <summary>
    /// called when the window should close
    /// </summary>
    public void Hide()
    {
      if (_attachedInput)
      {
        ServiceScope.Get<IInputManager>().OnKeyPressed -= _keyPressHandler;
        ServiceScope.Get<IInputManager>().OnMouseMove -= _mouseMoveHandler;
        _attachedInput = false;
        if (OnClose != null)
        {
          OnClose(this, null);
        }
      }
    }

    /// <summary>
    /// Gets a value indicating whether this window is animating.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this window is animating; otherwise, <c>false</c>.
    /// </value>
    public bool IsAnimating
    {
      get
      {
        for (int i = 0; i < _controls.Count; ++i)
        {
          if (_controls[i].IsAnimating)
          {
            return true;
          }
        }
        return false;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating the wait cursor should be shown
    /// </summary>
    /// <value>
    /// 	<c>true</c> if wait cursor should be shown; otherwise, <c>false</c>.
    /// </value>
    public bool WaitCursorVisible
    {
      get { return _waitCursor.Visible; }
      set { _waitCursor.Visible = value; }
    }

    public void Reset()
    {
      for (int i = 0; i < _controls.Count; ++i)
      {
        _controls[i].Reset();
      }
    }
  }
}

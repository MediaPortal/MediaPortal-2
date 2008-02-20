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
    private KeyPressedHandler _keyPressHandler;
    private MouseMoveHandler _mouseMoveHandler;
    private Property _opened;
    private bool _attachedInput = false;
    private string _defaultFocus;
    private Thread _thread;
    public event EventHandler OnClose;
    private bool _history;
    UIElement _visual;
    bool _setFocusedElement = false;
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
      _name = name;
      _keyPressHandler = new KeyPressedHandler(OnKeyPressed);
      _mouseMoveHandler = new MouseMoveHandler(OnMouseMove);
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
          _history = _visual.History;
          _visual.IsArrangeValid = true;
        }
      }
    }
    public FrameworkElement RootElement
    {
      get
      {
        return _visual as FrameworkElement;
      }
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
      }
    }

    public bool IsAnimating
    {
      get
      {
        return false;
      }
    }

    /// <summary>
    /// Renders this window.
    /// </summary>
    public void Render()
    {
      uint time = (uint)Environment.TickCount;
      SkinContext.TimePassed = time;
      SkinContext.FinalMatrix = new ExtendedMatrix();


      if (!IsOpened && _thread == null && !IsAnimating)
      {
        //we cannot close the window from the render thread
        //so start a new workerthread to close ourselves
        _thread = new Thread(new ThreadStart(CloseThisWindow));
        _thread.Name = "Window Close Thread";
        _thread.Start();
      }

      lock (_visual)
      {
        _visual.Render();
        _visual.Animate();
        if (_setFocusedElement)
        {
          if (_visual.FocusedElement != null)
          {
            _visual.FocusedElement.HasFocus = true;
            _setFocusedElement = !_visual.FocusedElement.HasFocus;
          }
        }
      }
    }

    /// <summary>
    /// Closes the this window.
    /// </summary>
    private void CloseThisWindow()
    {
      WindowManager manager = (WindowManager)ServiceScope.Get<IWindowManager>();
      if (manager.CurrentWindow == this)
      {
        manager.ShowPreviousWindow();
      }
      _thread = null;
    }

    public void AttachInput()
    {
      if (!_attachedInput)
      {
        ServiceScope.Get<IInputManager>().OnKeyPressed += _keyPressHandler;
        ServiceScope.Get<IInputManager>().OnMouseMove += _mouseMoveHandler;
        _attachedInput = true;
        HasFocus = true;
      }
    }

    /// <summary>
    /// Called when window should be shown
    /// </summary>
    public void Show()
    {
      FocusManager.FocusedElement = null;
      VisualTreeHelper.Instance.SetRootElement(_visual);
      _visual.Allocate();
      _visual.Reset();
      _visual.Invalidate();
      _visual.InitializeBindings();
      _setFocusedElement = true;
    }
    /// <summary>
    /// Called when window should be hidden
    /// </summary>
    public void Hide()
    {
      lock (_visual)
      {
        _visual.Deallocate();
      }
    }

    /// <summary>
    /// Called when a keypress has been received
    /// </summary>
    /// <param name="key">The key.</param>
    private void OnKeyPressed(ref Key key)
    {
      if (!HasFocus || !_attachedInput)
      {
        return;
      }
      _visual.OnKeyPressed(ref key);
    }

    /// <summary>
    /// Called when the mouse was moved
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    private void OnMouseMove(float x, float y)
    {
      if (!HasFocus || !_attachedInput)
      {
        return;
      }
      _visual.OnMouseMove(x, y);
    }

    /// <summary>
    /// called when the window should close
    /// </summary>
    public void DetachInput()
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

    public void Reset()
    {
      SkinContext.Zoom = new System.Drawing.SizeF(((float)GraphicsDevice.Width) / SkinContext.Width, ((float)GraphicsDevice.Height) / SkinContext.Height);
      _visual.Invalidate();
      _visual.InitializeBindings();
      _visual.Reset();
    }
  }
}

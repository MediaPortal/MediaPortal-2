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
using System.Diagnostics;
using System.Collections.Generic;

using MediaPortal.Core;
using MediaPortal.Control.InputManager;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.SkinEngine.InputManagement;
using MediaPortal.SkinEngine.Xaml;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.ScreenManagement
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

    #region Variables

    private string _name;
    private bool _hasFocus;
    private State _state = State.Running;

    // TRUE if the sceen is a dialog and is a child of another dialog.
    private bool _isChildDialog;

    /// <summary>
    /// Holds the information if our input handlers are currently attached at
    /// the <see cref="IInputManager"/>.
    /// </summary>
    private bool _attachedInput = false;

    private Property _opened;
    public event EventHandler Closed;
    private bool _history;
    UIElement _visual;
    bool _setFocusedElement = false;
    Animator _animator;
    List<IUpdateEventHandler> _invalidControls = new List<IUpdateEventHandler>();

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

      _history = true;
      _opened = new Property(typeof(bool), true);
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
        {
          _history = _visual.History;
          _visual.SetScreen(this);
        }
      }
    }

    public FrameworkElement RootElement
    {
      get { return _visual as FrameworkElement; }
    }

    // FIXME Albert78: Remove this - history is managed by workflow manager now
    public bool History
    {
      get { return _history; }
      set { _history = value; }
    }

    public bool IsChildDialog
    {
      get { return _isChildDialog; }
      set { _isChildDialog = value; }
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

    public Property IsOpenedProperty
    {
      get { return _opened; }
      set { _opened = value; }
    }

    public State ScreenState
    {
      get { return _state; }
      set { _state = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this screen has focus.
    /// </summary>
    /// <value><c>true</c> if this screen has focus; otherwise, <c>false</c>.</value>
    public bool HasFocus
    {
      get { return _hasFocus; }
      set { _hasFocus = value; }
    }

    public string Name
    {
      get { return _name; }
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
      Trace.WriteLine("Screen Deallocate: " + Name);
      if (SkinContext.UseBatching)
        _visual.DestroyRenderTree();
      _visual.Deallocate();
    }

    public void Render()
    {
      uint time = (uint)Environment.TickCount;
      SkinContext.TimePassed = time;
      SkinContext.FinalMatrix = new ExtendedMatrix();

      if (SkinContext.UseBatching)
      {
        lock (_visual)
        {
          _animator.Animate();
          Update();
        }
        return;
      }
      else
      {

        lock (_visual)
        {
          _visual.Render();
          _animator.Animate();
        }
      }
      if (_setFocusedElement)
      {
        if (_visual.FocusedElement != null)
        {
          _visual.FocusedElement.HasFocus = true;
          _setFocusedElement = !_visual.FocusedElement.HasFocus;
        }
      }
    }

    public void AttachInput()
    {
      if (!_attachedInput)
      {
        ServiceScope.Get<IInputManager>().KeyPressed += OnKeyPressed;
        ServiceScope.Get<IInputManager>().MouseMoved += OnMouseMove;
        FocusManager.AttachInput(this);
        _attachedInput = true;
        HasFocus = true;
      }
    }

    public void DetachInput()
    {
      if (_attachedInput)
      {
        ServiceScope.Get<IInputManager>().KeyPressed -= OnKeyPressed;
        ServiceScope.Get<IInputManager>().MouseMoved -= OnMouseMove;
        FocusManager.DetachInput(this);
        _attachedInput = false;
        // FIXME Albert78: Don't fire the Closed event in method DetachInput
        if (Closed != null)
          Closed(this, null);
      }
    }

    public void Show()
    {
      //Trace.WriteLine("Screen Show: " + Name);

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
        _setFocusedElement = true;
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
    }

    private void OnKeyPressed(ref Key key)
    {
      if (!HasFocus || !_attachedInput)
        return;
      _visual.OnKeyPressed(ref key);
      if (key != Key.None)
        FocusManager.OnKeyPressed(ref key);
    }

    private void OnMouseMove(float x, float y)
    {
      if (!HasFocus || !_attachedInput)
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
      List<IUpdateEventHandler> ctls;
      lock (_invalidControls)
      {
        if (_invalidControls.Count == 0) 
          return;
        ctls = _invalidControls;
        _invalidControls = new List<IUpdateEventHandler>();
      }
      for (int i = 0; i < ctls.Count; ++i)
        ctls[i].Update();
    }
  }
}
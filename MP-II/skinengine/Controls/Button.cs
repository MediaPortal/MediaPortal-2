#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using MediaPortal.Core.InputManager;
using MediaPortal.Core.Properties;
using SkinEngine.Skin;

namespace SkinEngine.Controls
{
  public class Button : Control
  {
    #region variables

    protected Style _style;
    private Property _label;
    private Property _mouseOnly;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Button"/> class.
    /// </summary>
    /// <param name="parent">The parent.</param>
    public Button(Control parent)
      : base(parent)
    {
      IsFocusable = true;
      CanFocus = true;
      _label = new Property("Name", "");
      _mouseOnly = new Property("MouseOnly", false);
    }

    /// <summary>
    /// Gets or sets the mouse only property.
    /// </summary>
    /// <value>The mouse only property.</value>
    public virtual Property MouseOnlyProperty
    {
      get { return _mouseOnly; }
      set { _mouseOnly = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether [mouse only].
    /// </summary>
    /// <value><c>true</c> if [mouse only]; otherwise, <c>false</c>.</value>
    public virtual bool MouseOnly
    {
      get { return (bool)_mouseOnly.GetValue(); }
      set { _mouseOnly.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the buttons label property.
    /// </summary>
    /// <value>The label property.</value>
    public virtual Property LabelProperty
    {
      get { return _label; }
      set { _label = value; }
    }

    public virtual string Text
    {
      get { return _label.GetValue().ToString(); }
      set { _label.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the buttons style.
    /// </summary>
    /// <value>The style.</value>
    public Style Style
    {
      get { return _style; }
      set { _style = value; }
    }

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    /// <value>The width.</value>
    public override float Width
    {
      get
      {
        if (_width != 0) return _width;
        return _style.Width;
      }
      set { _width = value; }
    }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    /// <value>The height.</value>
    public override float Height
    {
      get
      {
        if (_height != 0) return _height;
        return _style.Height;
      }
      set { _height = value; }
    }

    /// <summary>
    /// Resets the control and its animations.
    /// </summary>
    public override void Reset()
    {
      base.Reset();
      if (_style != null)
      {
        _style.Reset();
      }
    }
    public override void DoLayout()
    {
    }
    public override void Invalidate()
    {
      //  Trace.WriteLine("btn.invalidate");
      base.Invalidate();
      _style.Invalidate();
    }
    /// <summary>
    /// Renders the control
    /// </summary>
    /// <param name="timePassed">The time passed.</param>
    public override void Render(uint timePassed)
    {
      if (!IsVisible)
      {
        if (!IsAnimating)
        {
          return;
        }
      }
      _style.Render(timePassed);
      base.Render(timePassed);
    }

    /// <summary>
    /// Checks if control is positioned at coordinates (x,y)
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns></returns>
    public override bool HitTest(float x, float y)
    {
      if (!IsVisible)
      {
        return false;
      }
      if (MouseOnly)
      {
        HasMouseFocus = _style.HitTest(x, y);
        return false;
      }
      if (!IsFocusable)
      {
        return false;
      }
      return _style.HitTest(x, y);
    }

    /// <summary>
    /// handles mouse movements
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public override void OnMouseMove(float x, float y)
    {
      if (!IsVisible)
      {
        return;
      }
      if (MouseOnly)
      {
        HasMouseFocus = _style.HitTest(x, y);
        return;
      }
      if (!IsFocusable)
      {
        return;
      }
      _style.OnMouseMove(x, y);
      Control c = _style.FocusedControl;
      HasFocus = HitTest(x, y);
      if (HasFocus && c != null)
      {
        c.HasFocus = true;
      }

      //      Trace.WriteLine("mousemove btn : " + this.Name + " focus:" + HasFocus);
    }
    public override void Execute()
    {
      if (Command != null)
      {
        Command.Execute(CommandParameter);
        if (CommandResult != null)
        {
          CommandResult.Execute();
        }
      }
      if (ListItem != null)
      {
        if (ListItem.Command != null)
        {
          ListItem.Command.Execute(ListItem.CommandParameter);
        }
      }
      if (_container != null && ((_container as Button) == null))
      {
        _container.Execute();
      }
    }

    /// <summary>
    /// handles any keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public override void OnKeyPressed(ref Key key)
    {
      if (MouseOnly && HasMouseFocus)
      {
        if (key == Key.Enter)
        {
          key = Key.None;
          Execute();
        }
        return;
      }
      if (!HasFocus)
      {
        return;
      }
      if (Window.FocusedMouseControl != null)
      {
        if (key == Key.Enter)
        {
          return;
        }
      }
      if (_style.HasFocus)
      {
        if (key == Key.Left || key == Key.Right)
        {
          Control org = _style.FocusedControl;
          Control subCntl = PredictFocus(_style.FocusedControl, ref key);
          if (subCntl != null)
          {
            subCntl.HasFocus = true;
            if (org != null)
              org.HasFocus = false;
            key = Key.None;
            return;
          }
          else
          {
            this.Window.FocusedControl = this;
            if (org != null)
              org.HasFocus = false;
            key = Key.None;
            return;
          }
        }
        if (key != Key.Up && key != Key.Down)
        {
          if (_style.FocusedControl != null)
          {
            _style.FocusedControl.OnKeyPressed(ref key);
            return;
          }
        }
      }

      if (key == Key.Enter)
      {
        key = Key.None;
        Execute();
        return;
      }
      if (key == Key.ContextMenu)
      {
        key = Key.None;
        ExecuteContextMenu();
        return;
      }
      if (key == Key.None)
      {
        return;
      }

      Control cntl = PredictFocus(this, ref key);
      if (cntl != null)
      {
        cntl.HasFocus = true;
        key = Key.None;
      }
      else
      {
        cntl = InputManager.PredictFocus(this, ref key);
        if (cntl != null)
        {
          Control org = _style.FocusedControl;
          if (org != null)
            org.HasFocus = false;
          HasFocus = false;
          cntl.HasFocus = true;
          Trace.WriteLine("keypress btn : " + this.Name + " focus:" + HasFocus);
          Trace.WriteLine("         btn : " + cntl.Name + " focus:" + cntl.HasFocus);
          key = Key.None;
        }
      }
    }

    Control PredictFocus(Control focusedControl, ref Key key)
    {
      if (key == Key.Left)
      {
        Control c = _style.PredictFocusLeft(focusedControl, ref key, true);
        if (c != null)
        {
          key = Key.None;
          return c;
        }
      }
      if (key == Key.Right)
      {
        Control c = _style.PredictFocusRight(focusedControl, ref key, true);
        if (c != null)
        {
          key = Key.None;
          return c;
        }
      }

      return null;
    }
    public override void SetState()
    {
      Animations.SetState(this);
      _style.SetState();
    }

    /// <summary>
    /// Gets a value indicating whether this control is animating.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this control is animating; otherwise, <c>false</c>.
    /// </value>
    public override bool IsAnimating
    {
      get { return base.IsAnimating || _style.IsAnimating; }
    }

    /*
    public override void UpdateProperties()
    {
      _style.UpdateProperties();
      base.UpdateProperties();
    }*/

    public override Control PredictFocusLeft(Control focusedControl, ref Key key, bool strict)
    {
      Control c = _style.PredictFocusLeft(focusedControl, ref key, strict);
      if (c != null)
      {
        key = Key.None;
        return this;
      }
      return base.PredictFocusLeft(focusedControl, ref key, strict);
    }

    /// <summary>
    /// Predicts the next control which is position right of this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <param name="strict"></param>
    /// <returns></returns>
    public override Control PredictFocusRight(Control focusedControl, ref Key key, bool strict)
    {/*
      Control c = _style.PredictFocusRight(focusedControl, ref key, strict);
      if (c != null)
      {
        key = Key.None;
        return this;
      }*/
      return base.PredictFocusRight(focusedControl, ref key, strict);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this control has focus.
    /// </summary>
    /// <value><c>true</c> if this control has focus; otherwise, <c>false</c>.</value>
    public override bool HasFocus
    {
      get
      {
        return base.HasFocus;
      }
      set
      {
        base.HasFocus = value;
        if (!value)
        {
          Control c = _style.FocusedControl;
          if (c != null) c.HasFocus = false;
        }
      }
    }

  }
}
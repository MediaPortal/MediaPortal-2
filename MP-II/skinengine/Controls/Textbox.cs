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

using MediaPortal.Core;
using MediaPortal.Core.InputManager;
using MediaPortal.Core.Properties;
using SkinEngine.Skin;

namespace SkinEngine.Controls
{
  public class Textbox : Control
  {
    #region variables

    protected Style _style;
    private ILabelProperty _text;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Button"/> class.
    /// </summary>
    /// <param name="parent">The parent.</param>
    public Textbox(Control parent)
      : base(parent)
    {
      IsFocusable = true;
      CanFocus = true;
      _text = new LabelProperty("");
    }


    /// <summary>
    /// Gets or sets the textbox text.
    /// </summary>
    /// <value>The label property.</value>
    public virtual ILabelProperty Text
    {
      get { return _text; }
      set { _text = value; }
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
      get { return _style.Width; }
      set { _width = value; }
    }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    /// <value>The height.</value>
    public override float Height
    {
      get { return _style.Height; }
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
      HasFocus = HitTest(x, y);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this control has focus.
    /// </summary>
    /// <value><c>true</c> if this control has focus; otherwise, <c>false</c>.</value>
    public override bool HasFocus
    {
      get { return base.HasFocus; }
      set
      {
        bool hadFocus = base.HasFocus;
        base.HasFocus = value;
        if (base.HasFocus)
        {
          ServiceScope.Get<IInputManager>().NeedRawKeyData = true;
        }
        else if (hadFocus)
        {
          ServiceScope.Get<IInputManager>().NeedRawKeyData = false;
        }
      }
    }

    /// <summary>
    /// handles any keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public override void OnKeyPressed(ref Key key)
    {
      if (!HasFocus)
      {
        return;
      }
      if (key == Key.Enter)
      {
        key = Key.None;
        Execute();
        return;
      }
      if (key == Key.None)
      {
        return;
      }
      Control cntl = InputManager.PredictFocus(this, ref key);
      if (cntl != null)
      {
        cntl.HasFocus = true;
        HasFocus = false;
        //        Trace.WriteLine("keypress btn : " + this.Name + " focus:" + HasFocus);
        //        Trace.WriteLine("         btn : " + cntl.Name + " focus:" + cntl.HasFocus);
      }
      key = Key.None;
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

    /*public override void UpdateProperties()
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
        return c;
      }
      return base.PredictFocusLeft(focusedControl, ref key, strict);
    }

    public override Control PredictFocusRight(Control focusedControl, ref Key key, bool strict)
    {
      Control c = _style.PredictFocusRight(focusedControl, ref key, strict);
      if (c != null)
      {
        key = Key.None;
        return c;
      }
      return base.PredictFocusRight(focusedControl, ref key, strict);
    }
  }
}
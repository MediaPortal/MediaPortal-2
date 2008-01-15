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

using System.Collections.Generic;
using System.Diagnostics;
using MediaPortal.Core;
using MediaPortal.Core.InputManager;
using MediaPortal.Core.WindowManager;
using SkinEngine.Skin.Layout;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;

namespace SkinEngine.Controls
{
  public class Group : Control
  {
    #region variables

    protected List<Control> _controls;

    private float _calculatedWidth = -1;
    private float _calculatedHeight = -1;
    ILayout _layout;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Group"/> class.
    /// </summary>
    /// <param name="parent">The parent.</param>
    public Group(Control parent)
      : base(parent)
    {
      _controls = new List<Control>();
    }

    /// <summary>
    /// Gets or sets the controls belonging to this group.
    /// </summary>
    /// <value>The controls.</value>
    public List<Control> Controls
    {
      get { return _controls; }
      set { _controls = value; }
    }
    /// <summary>
    /// Gets or sets the lay out.
    /// </summary>
    /// <value>The lay out.</value>
    public ILayout LayOut
    {
      get
      {
        return _layout;
      }
      set
      {
        _layout = value;
      }
    }
    /// <summary>
    /// Does the layout.
    /// </summary>
    public override void DoLayout()
    {
    }
    public override void Invalidate()
    {
      //Trace.WriteLine("group.invalidate");
      for (int i = 0; i < _controls.Count; ++i)
      {
        _controls[i].Invalidate();
      }
      _calculatedWidth = -1;
      _calculatedHeight = -1;

      if (LayOut != null)
      {
        LayOut.Perform();
      }
    }
    /// <summary>
    /// Gets or sets the width of the group
    /// </summary>
    /// <value>The width.</value>
    public override float Width
    {
      get
      {
        if (_width != 0) return _width;

        if (_calculatedWidth < 0)
        {
          float lowestX = float.MaxValue;
          float highestX = 0.0f;
          for (int i = 0; i < _controls.Count; ++i)
          {
            if (_controls[i].Position.X < lowestX)
            {
              lowestX = _controls[i].Position.X;
              if (highestX == 0.0f)
                highestX = lowestX;
            }
            if (_controls[i].Position.X + _controls[i].Width > highestX)
            {
              highestX = _controls[i].Position.X + _controls[i].Width;
              if (lowestX == float.MaxValue)
                lowestX = highestX;
            }
          }
          _calculatedWidth = (highestX - lowestX);
        }
        return _calculatedWidth;
      }
      set { _width = value; }
    }

    /// <summary>
    /// Gets or sets the height of the group
    /// </summary>
    /// <value>The height.</value>
    public override float Height
    {
      get
      {
        if (_height != 0) return _height;
        if (_calculatedHeight < 0)
        {
          float lowestY = float.MaxValue;
          float highestY = 0.0f;
          for (int i = 0; i < _controls.Count; ++i)
          {
            if (_controls[i].Position.Y < lowestY)
            {
              lowestY = _controls[i].Position.Y;
              if (highestY == 0.0f) 
                highestY = lowestY;
            }
            if (_controls[i].Position.Y + _controls[i].Height > highestY)
            {
              highestY = _controls[i].Position.Y + _controls[i].Height;
              if (lowestY == float.MaxValue)
                lowestY = highestY;
            }
          }
          _calculatedHeight = (highestY - lowestY);
        }
        return _calculatedHeight;
      }
      set { _height = value; }
    }

    /// <summary>
    /// Resets the group, all subcontrols and their animations.
    /// </summary>
    public override void Reset()
    {
      base.Reset();
      for (int i = 0; i < _controls.Count; ++i)
      {
        _controls[i].Reset();
      }
    }


    /// <summary>
    /// Renders the group
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
      base.Render(timePassed);

      for (int i = 0; i < _controls.Count; ++i)
      {
        _controls[i].DoRender(timePassed);
      }
    }

    #region focus prediction

    /// <summary>
    /// Predicts the next control which is position above this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public override Control PredictFocusUp(Control focusedControl, ref Key key, bool strict)
    {
      Control bestMatch = null;
      IWindowManager manager = (IWindowManager)ServiceScope.Get<IWindowManager>();
      IWindow window = manager.CurrentWindow;
      float bestDistance = float.MaxValue;
      foreach (Control c in Controls)
      {
        Control match = c.PredictFocusUp(focusedControl, ref key, strict);
        if (key == Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match.IsFocusable)
          {
            if (match == focusedControl)
            {
              continue;
            }
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedControl);
            }
            else
            {
              if (match.Position.Y + match.Height >= bestMatch.Position.Y + bestMatch.Height)
              {
                float distance = Distance(match, focusedControl);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }

    /// <summary>
    /// Predicts the next control which is position below this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public override Control PredictFocusDown(Control focusedControl, ref Key key, bool strict)
    {
      Control bestMatch = null;
      IWindowManager manager = (IWindowManager)ServiceScope.Get<IWindowManager>();
      IWindow window = manager.CurrentWindow;
      float bestDistance = float.MaxValue;
      foreach (Control c in Controls)
      {
        Control match = c.PredictFocusDown(focusedControl, ref key, strict);
        if (key == Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match == focusedControl)
          {
            continue;
          }
          if (match.IsFocusable)
          {
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedControl);
            }
            else
            {
              if (match.Position.Y <= bestMatch.Position.Y)
              {
                float distance = Distance(match, focusedControl);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }

    /// <summary>
    /// Predicts the next control which is position left of this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public override Control PredictFocusLeft(Control focusedControl, ref Key key, bool strict)
    {
      Control bestMatch = null;
      IWindowManager manager = (IWindowManager)ServiceScope.Get<IWindowManager>();
      IWindow window = manager.CurrentWindow;
      float bestDistance = float.MaxValue;
      foreach (Control c in Controls)
      {
        Control match = c.PredictFocusLeft(focusedControl, ref key, strict);
        if (key == Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match == focusedControl)
          {
            continue;
          }
          if (match.IsFocusable)
          {
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedControl);
            }
            else
            {
              if (match.Position.X >= bestMatch.Position.X)
              {
                float distance = Distance(match, focusedControl);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }

    /// <summary>
    /// Predicts the next control which is position right of this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public override Control PredictFocusRight(Control focusedControl, ref Key key, bool strict)
    {
      Control bestMatch = null;
      IWindowManager manager = (IWindowManager)ServiceScope.Get<IWindowManager>();
      IWindow window = manager.CurrentWindow;
      float bestDistance = float.MaxValue;
      foreach (Control c in Controls)
      {
        Control match = c.PredictFocusRight(focusedControl, ref key, strict);
        if (key == Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match == focusedControl)
          {
            continue;
          }
          if (match.IsFocusable)
          {
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedControl);
            }
            else
            {
              if (match.Position.X <= bestMatch.Position.X)
              {
                float distance = Distance(match, focusedControl);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }


    public override Control PredictControlUp(Control focusedControl, ref Key key)
    {
      Control bestMatch = null;
      IWindowManager manager = (IWindowManager)ServiceScope.Get<IWindowManager>();
      IWindow window = manager.CurrentWindow;
      float bestDistance = float.MaxValue;
      foreach (Control c in Controls)
      {
        Control match = c.PredictControlUp(focusedControl, ref key);
        if (key == Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match.CanFocus)
          {
            if (match == focusedControl)
            {
              continue;
            }
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedControl);
            }
            else
            {
              if (match.Position.Y + match.Height >= bestMatch.Position.Y + bestMatch.Height)
              {
                float distance = Distance(match, focusedControl);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }

    public override Control PredictControlDown(Control focusedControl, ref Key key)
    {
      Control bestMatch = null;
      IWindowManager manager = (IWindowManager)ServiceScope.Get<IWindowManager>();
      IWindow window = manager.CurrentWindow;
      float bestDistance = float.MaxValue;
      foreach (Control c in Controls)
      {
        Control match = c.PredictControlDown(focusedControl, ref key);
        if (key == Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match.CanFocus)
          {
            if (match == focusedControl)
            {
              continue;
            }
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedControl);
            }
            else
            {
              if (match.Position.Y <= bestMatch.Position.Y)
              {
                float distance = Distance(match, focusedControl);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }

    public override Control PredictControlLeft(Control focusedControl, ref Key key)
    {
      Control bestMatch = null;
      IWindowManager manager = (IWindowManager)ServiceScope.Get<IWindowManager>();
      IWindow window = manager.CurrentWindow;
      float bestDistance = float.MaxValue;
      foreach (Control c in Controls)
      {
        Control match = c.PredictControlLeft(focusedControl, ref key);
        if (key == Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match.CanFocus)
          {
            if (match == focusedControl)
            {
              continue;
            }
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedControl);
            }
            else
            {
              if (match.Position.X >= bestMatch.Position.X)
              {
                float distance = Distance(match, focusedControl);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }

    public override Control PredictControlRight(Control focusedControl, ref Key key)
    {
      Control bestMatch = null;
      IWindowManager manager = (IWindowManager)ServiceScope.Get<IWindowManager>();
      IWindow window = manager.CurrentWindow;
      float bestDistance = float.MaxValue;
      foreach (Control c in Controls)
      {
        Control match = c.PredictControlRight(focusedControl, ref key);
        if (key == Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match.CanFocus)
          {
            if (match == focusedControl)
            {
              continue;
            }
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedControl);
            }
            else
            {
              if (match.Position.X <= bestMatch.Position.X)
              {
                float distance = Distance(match, focusedControl);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }

    #endregion

    /// <summary>
    /// Gets or sets a value indicating whether this control (or one of its subcontrols) has focus.
    /// </summary>
    /// <value><c>true</c> if this control has focus; otherwise, <c>false</c>.</value>
    public override bool HasFocus
    {
      get { return (FocusedControl != null); }
      set
      {
        if (base.HasFocus != value)
        {
          base.HasFocus = value;
          if (value)
          {
            if (FocusedControl == null)
            {
              SetFocus(0);
            }
          }
        }
      }
    }

    /// <summary>
    /// Sets the focus to the specified subcontrol
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns></returns>
    public bool SetFocus(int index)
    {
      //Trace.WriteLine(" style:setfocus on:" + index.ToString());
      bool result = false;
      int count = 0;
      for (int i = 0; i < Controls.Count; ++i)
      {
        if (Controls[i] as Group != null)
        {
          Group group = (Group)Controls[i];
          if (group.SetFocus(index))
          {
            return true;
          }
        }
        if (count == index)
        {
          if (Controls[i].IsFocusable)
          {
            //            Trace.WriteLine(" style: on:" + i.ToString());
            Controls[i].HasFocus = true;
            result = true;
          }
        }
        else
        {
          Controls[i].HasFocus = false;
        }
        if (Controls[i].IsFocusable)
        {
          count++;
        }
      }
      return result;
    }


    /// <summary>
    /// Gets the subcontrol which has focused 
    /// </summary>
    /// <value>The focused control.</value>
    public Control FocusedControl
    {
      get { return GetFocusedControl; }
    }

    /// <summary>
    /// Gets the index of the focused subcontrol.
    /// </summary>
    /// <value>The index of the focused control.</value>
    public int FocusedControlIndex
    {
      get { return GetFocusedControlIndex; }
    }

    /// <summary>
    /// Gets the number of subcontrols which can receive focus
    /// </summary>
    /// <value>The focusable control count.</value>
    public int FocusableControlCount
    {
      get { return GetFocusableControlCount; }
    }

    /// <summary>
    /// Gets a value indicating whether this control (or one of the subcontrols) is animating.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this control (or one of the subcontrols) is animating; otherwise, <c>false</c>.
    /// </value>
    public override bool IsAnimating
    {
      get
      {
        if (Animations.IsAnimating)
        {
          return true;
        }
        foreach (Control c in Controls)
        {
          Group group = c as Group;
          if (group != null)
          {
            if (group.IsAnimating)
            {
              return true;
            }
          }
          else
          {
            if (c.Animations.IsAnimating)
            {
              return true;
            }
          }
        }
        return false;
      }
    }

    /// <summary>
    /// handles any keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public override void OnKeyPressed(ref Key key)
    {
      for (int i=0; i < _controls.Count;++i)
      {
        _controls[i].OnKeyPressed(ref key);
      }
      base.OnKeyPressed(ref key);
    }

    /// <summary>
    /// handles mouse movements
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public override void OnMouseMove(float x, float y)
    {
      foreach (Control c in Controls)
      {
        c.OnMouseMove(x, y);
      }
    }


    /// <summary>
    /// Checks if control is positioned at coordinates (x,y)
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns></returns>
    public override bool HitTest(float x, float y)
    {
      bool result = false;
      foreach (Control c in Controls)
      {
        result |= c.HitTest(x, y);
      }
      return result;
    }

    /// <summary>
    /// Gets the subcontrol which has focused 
    /// </summary>
    /// <value>The focused control.</value>
    private Control GetFocusedControl
    {
      get
      {
        foreach (Control c in Controls)
        {
          Group group = c as Group;
          if (group != null)
          {
            Control f = group.FocusedControl;
            if (f != null)
            {
              return f;
            }
          }
          if (c.HasFocus)
          {
            return c;
          }
        }
        return null;
      }
    }

    /// <summary>
    /// Gets the index of the focused subcontrol.
    /// </summary>
    /// <value>The index of the focused control.</value>
    private int GetFocusedControlIndex
    {
      get
      {
        int index = -1;
        foreach (Control c in Controls)
        {
          Group group = c as Group;
          if (group != null)
          {
            int x = group.FocusedControlIndex;
            if (x > -1)
            {
              return x;
            }
          }
          else
          {
            if (c.IsFocusable)
            {
              index++;
              if (c.HasFocus)
              {
                return index;
              }
            }
          }
        }
        return index;
      }
    }

    /// <summary>
    /// Gets the number of subcontrols which can receive focus
    /// </summary>
    /// <value>The focusable control count.</value>
    private int GetFocusableControlCount
    {
      get
      {
        int count = 0;
        foreach (Control c in Controls)
        {
          Group group = c as Group;
          if (group != null)
          {
            count += group.FocusableControlCount;
          }
          else
          {
            if (c.IsFocusable && c.IsVisible)
            {
              count++;
            }
          }
        }
        return count;
      }
    }

  }
}

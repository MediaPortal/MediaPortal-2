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
using MediaPortal.Core.InputManager;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls
{
  public class UpDown : Button
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="UpDown"/> class.
    /// </summary>
    /// <param name="parent">The parent.</param>
    public UpDown(Control parent)
      : base(parent)
    {
      IsFocusable = true;
      CanFocus = true;
    }

    #region ILabelProperty Members

    /// <summary>
    /// returns the label text for the up/down control
    /// </summary>
    /// <param name="container">The container.</param>
    /// <returns>string in format of x/y where x is the current page and y the max. pages</returns>
    public string Evaluate(IControl control, IControl container)
    {
      ListContainer list = (ListContainer) container;
      if (list == null)
      {
        return "1/1";
      }
      int itemCount = list.Items.Count;
      int pageSize = list.PageSize;
      if (pageSize != 0)
      {
        int pageNr = (list.PageOffset/pageSize);
        if ((list.PageOffset%pageSize) != 0)
        {
          pageNr++;
        }
        int totalPages = (itemCount/pageSize);
        if ((itemCount%pageSize) != 0)
        {
          totalPages++;
        }
        return String.Format("{0}/{1}", 1 + pageNr, totalPages);
      }
      return ("1/1");
    }

    #endregion

    /// <summary>
    /// Gets the focused control.
    /// </summary>
    /// <value>The focused control.</value>
    public Control FocusedControl
    {
      get { return _style.FocusedControl; }
    }

    /// <summary>
    /// Predicts the next control which is position above this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public override Control PredictFocusUp(Control focusedControl, ref Key key, bool strict)
    {
      return _style.PredictFocusUp(focusedControl, ref key, strict);
    }

    /// <summary>
    /// Predicts the next control which is position below this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public override Control PredictFocusDown(Control focusedControl, ref Key key, bool strict)
    {
      return _style.PredictFocusDown(focusedControl, ref key, strict);
    }

    /// <summary>
    /// Predicts the next control which is position left of this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public override Control PredictFocusLeft(Control focusedControl, ref Key key, bool strict)
    {
      return _style.PredictFocusLeft(focusedControl, ref key, strict);
    }

    /// <summary>
    /// Predicts the next control which is position right of this control
    /// </summary>
    /// <param name="focusedControl">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public override Control PredictFocusRight(Control focusedControl, ref Key key, bool strict)
    {
      return _style.PredictFocusRight(focusedControl, ref key, strict);
    }

    /// <summary>
    /// handles mouse movements
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public override void OnMouseMove(float x, float y)
    {
      _style.OnMouseMove(x, y);
      HasFocus = _style.HitTest(x, y);
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
      if (key == Key.PageUp)
      {
        ListContainer list = (ListContainer) Container;
        list.PageUp();
      }
      if (key == Key.PageDown)
      {
        ListContainer list = (ListContainer) Container;
        list.PageDown();
      }
      _style.OnKeyPressed(ref key);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this control has focus.
    /// </summary>
    /// <value><c>true</c> if this control has focus; otherwise, <c>false</c>.</value>
    public override bool HasFocus
    {
      get { return (FocusedControl != null); }
      set { }
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
    }
  }
}